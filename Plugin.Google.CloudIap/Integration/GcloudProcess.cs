//
// Copyright 2019 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Google.CloudIap.Integration
{
    /// <summary>
    /// Wrapper class for a gcloud process. Gcloud process are rather tedious to 
    /// handle on Windows because gcloud is not a binary, but a cmd file. For each
    /// gcloud invocation, you therefore end up with a process hierarchy that looks
    /// like this:
    /// 
    ///    (invoking process)
    ///      +-- cmd.exe (gcloud.cmd)
    ///          +-- conhost.exe
    ///          +-- cmd.exe (python wrapper)
    ///              +-- python.exe 
    ///                  +-- python.exe (in some cases)
    ///                  
    /// This tree becomes an issue when you want to terminate gcloud. As typical on Windows,
    /// terminating a parent process does not terminate its children -- so terminating the 
    /// top cmd.exe does not really do anything. To really terminate gcloud, you have to find 
    /// the topmost python.exe process in the process tree and terminate it.
    /// 
    /// This class wraps this mess and allows for easy terminating of gcloud processes,
    /// which is important to be able to terminate IAP tunnels.
    /// </summary>
    internal abstract class GcloudProcess : IDisposable
    {
        protected readonly Process wrapperProcess;
        private readonly StringBuilder errorBuffer = new StringBuilder();

        // Lightweight reference to a project that does not use a handle.
        // (which would have to be disposed).
        private class ProcessReference
        {
            public string ExecutablePath;
            public uint Id;
        }

        public int Id => this.wrapperProcess.Id;

        protected GcloudProcess(Process wrapperProcess)
        {
            this.wrapperProcess = wrapperProcess;

            this.wrapperProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data == "Testing if tunnel connection works.")
                {
                    // That is not an error, ignore.
                    return;
                }

                this.errorBuffer.Append(args.Data + "\n");
            };

            this.wrapperProcess.BeginErrorReadLine();
        }

        private static IEnumerable<ProcessReference> FindChildProcesses(ProcessReference parent)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT * " +
                "FROM Win32_Process " +
                "WHERE ParentProcessId=" + parent.Id);
            ManagementObjectCollection collection = searcher.Get();
            return collection
                .Cast<ManagementBaseObject>()
                .Select(item => new ProcessReference()
                {
                    ExecutablePath = (string)item["ExecutablePath"],
                    Id = (uint)item["ProcessId"]
                });
        }

        private static void FindDescendentProcesses(
            ProcessReference parent, 
            List<ProcessReference> accumulator)
        {
            foreach (var process in FindChildProcesses(parent))
            {
                accumulator.Add(process);
                FindDescendentProcesses(process, accumulator);
            }
        }

        private static IEnumerable<ProcessReference> FindDescendentProcesses(
            ProcessReference parent)
        {
            var accumulator = new List<ProcessReference>();
            FindDescendentProcesses(parent, accumulator);
            return accumulator;
        }

        protected Task<Process> GetWorkerProcess()
        {
            return Task.Run(() =>
            {
                // this.wrapperProcess refers to a cmd.exe process. This is not too useful
                // as the actual work is being done by the python child process.
                // Also, if we want to stop the process, we need the Python process,
                // not the cmd.exe wrapper.
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    var pythonProcess = FindDescendentProcesses(
                        new ProcessReference()
                        {
                            Id = (uint)this.wrapperProcess.Id,
                            ExecutablePath = ""
                        })
                        .FirstOrDefault(p => p.ExecutablePath.ToLowerInvariant().EndsWith("python.exe"));
                    if (pythonProcess != null)
                    {
                        return Process.GetProcessById((int)pythonProcess.Id);
                    }

                    // Might be too early, back off and try again.
                    Thread.Sleep(200);
                }

                throw new ApplicationException(
                    "Failed to launch gcloud",
                    new GCloudCommandException(this.errorBuffer.ToString()));
            });
        }

        protected static ProcessStartInfo CreateStartInfo(FileInfo gcloudExecutable, string arguments)
        {
            return new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                FileName = gcloudExecutable.FullName,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                Arguments = arguments
            };
        }

        public void Kill()
        {
            using (var workerProcess = GetWorkerProcess().Result)
            {
                workerProcess.Kill();
            }
        }

        public void Dispose()
        {
            this.wrapperProcess.Dispose();
        }

        public string ErrorOutput
        {
            get { return this.errorBuffer.ToString(); }
        }
    }

    [Serializable]
    public class GCloudCommandException : ApplicationException
    {
        public GCloudCommandException(string output)
            : base(output)
        {

        }
    }
}
