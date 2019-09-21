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

using Google.Solutions.CloudIap.Plugin.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.Plugin.Integration
{
    /// <summary>
    /// Helper class for issueing "gcloud compute reset-windows-password"
    /// commands to reset Windows passwords.
    /// </summary>
    internal class WindowsPasswordManager
    {
        private readonly PluginConfiguration configuration;

        public WindowsPasswordManager(PluginConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<NetworkCredential> ResetPassword(
            VmInstanceReference instance,
            string username)
        {
            var gcloudPath = configuration.GcloudCommandPath;
            if (gcloudPath == null)
            {
                throw new GCloudCommandException(
                    "gcloud not found. Please provide a path to gcloud in the settings");
            }

            // For some reason, the .NET API does not allow resetting
            // passwords, so we have to revert to invoking gcloud for this
            // purpose.
            using (var process = GcloudResetWindowsPasswordProcess.Start(
                new FileInfo(gcloudPath), instance, username))
            {
                return await process.WaitForCredentials();
            }
        }
    }

    internal class GcloudResetWindowsPasswordProcess : GcloudProcess
    {
        private readonly StringBuilder outputBuffer = new StringBuilder();

        private GcloudResetWindowsPasswordProcess(Process wrapperProcess) 
            : base(wrapperProcess)
        {
            this.wrapperProcess.OutputDataReceived += (sender, args) =>
            {
                this.outputBuffer.Append(args.Data + "\n");
            };

            this.wrapperProcess.BeginOutputReadLine();
        }

        public static GcloudResetWindowsPasswordProcess Start(
            FileInfo gcloudExecutable,
            VmInstanceReference instance,
            string username)
        {
            var startInfo = GcloudProcess.CreateStartInfo(
                gcloudExecutable,
                "compute reset-windows-password " +
                    $"{instance.InstanceName} " +
                    $"--project={instance.ProjectId} " +
                    $"--zone={instance.Zone} "+
                    $"--user={username} " +
                    "--quiet");

            startInfo.RedirectStandardOutput = true;
            return new GcloudResetWindowsPasswordProcess(Process.Start(startInfo));
        }

        public Task<NetworkCredential> WaitForCredentials()
        {
            return Task.Run(() =>
            {
                this.wrapperProcess.WaitForExit();

                if (this.wrapperProcess.ExitCode != 0)
                {
                    throw new ApplicationException(
                        "Resetting windows password failed",
                        new GCloudCommandException(this.ErrorOutput));
                }

                var outputAsDict = this.outputBuffer.ToString()
                    .Split('\n')
                    .Select(line => line.Split(new string[] { ": " }, StringSplitOptions.None))
                    .Where(lineSegments => lineSegments.Length == 2)
                    .ToDictionary(lineSegments => lineSegments[0].Trim(),
                                  lineSegments => lineSegments[1].Trim());

                return new NetworkCredential(
                    outputAsDict["username"],
                    outputAsDict["password"]);
            });
        }
    }
}
