//
// Copyright 2023 Google LLC
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
using System.Collections.Concurrent;
using System.Linq;

namespace Google.Solutions.Platform.Dispatch
{
    /// <summary>
    /// Factory for Win32 processes that uses jobs to track child processes
    /// </summary>
    public class Win32ChildProcessFactory : Win32ProcessFactory, IWin32ProcessSet, IDisposable
    {
        //
        // NB. The easiest way to implement this class would be to use a single
        // job for all processes. But that doesn't work reliably for processes
        // started with CreateProcessAsUser.
        //
        // When a process is started with CreateProcessAsUser, Windows
        // automatically places the process in a (system-defined) job. 
        // Recent versions of Windows allow processes to belong to multiple
        // jobs, so we can add it to another job later.
        //
        // However, this only works once: If we launch another process
        // using CreateProcessAsUser, and try to add it to the same job,
        // then AssignProcessToJob fails with an access-denied error.
        //
        // To work around this limitations, we place each process in its
        // own job, and maintain a collection of jobs. This is more complex,
        // but it ensures that we're compatible with theq quirky
        // CreateProcessAsUser behavior and also capture child processes.
        //

        private readonly ConcurrentBag<IWin32Job> jobs = new ConcurrentBag<IWin32Job>();

        public bool TerminateOnClose { get; }
        public bool IsDisposed { get; private set; }

        public Win32ChildProcessFactory(bool terminateOnClose)
        {
            this.TerminateOnClose = terminateOnClose;
        }

        //---------------------------------------------------------------------
        // IWin32ProcessSet.
        //---------------------------------------------------------------------

        public bool Contains(IWin32Process process)
        {
            return this.jobs.Any(j => j.Contains(process));
        }

        public bool Contains(uint processId)
        {
            return this.jobs.Any(j => j.Contains(processId));
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void OnProcessCreated(IWin32Process process)
        {
            var job = new Win32Job(this.TerminateOnClose);
            try
            {
                job.Add(process);
                this.jobs.Add(job);

                //
                // NB. We're not holding on to the process object
                // at it might be disposed at any time.
                //
            }
            catch (Exception)
            {
                job.Dispose();
                throw;
            }

            base.OnProcessCreated(process);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                foreach (var job in this.jobs)
                {
                    job.Dispose();
                }

                this.IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
