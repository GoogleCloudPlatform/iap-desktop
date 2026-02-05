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

using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.Test.Dispatch
{
    [TestFixture]
    public class TestWin32Job
    {
        private static readonly string CmdExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\cmd.exe";

        //---------------------------------------------------------------------
        // KillOnJobClose.
        //---------------------------------------------------------------------

        [Test]
        public void Ctor_WhenKillOnJobCloseIsTrue_ThenDisposeKillsProcesses()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                var job = new Win32Job(true);

                job.Add(process);
                process.Resume();
                Assert.IsTrue(process.IsRunning);

                job.Dispose();

                Assert.IsFalse(process.IsRunning);
            }
        }

        [Test]
        public void Ctor_WhenKillOnJobCloseIsFalse_ThenDisposeKeepsProcesses()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                var job = new Win32Job(false);

                job.Add(process);
                process.Resume();
                Assert.IsTrue(process.IsRunning);

                job.Dispose();

                Assert.IsTrue(process.IsRunning);
                process.Terminate(0);
            }
        }

        //---------------------------------------------------------------------
        // Add, Contains.
        //---------------------------------------------------------------------

        [Test]
        public void Contains_WhenAdded_ThenContainsReturnsTrue()
        {
            var factory = new Win32ProcessFactory();

            using (var job = new Win32Job(true))
            using (var process = factory.CreateProcess(
                CmdExe,
                null))
            {
                Assert.IsFalse(job.Contains(process));
                Assert.IsFalse(job.Contains(process.Id));

                job.Add(process);
                job.Add(process); // Again.

                Assert.IsTrue(job.Contains(process));
                Assert.IsTrue(job.Contains(process.Id));
            }
        }

        [Test]
        public void Contains_WhenProcessDoesNotExist_ThenContainsThrowsException()
        {
            using (var job = new Win32Job(true))
            {
                Assert.Throws<DispatchException>(
                    () => job.Contains(uint.MaxValue));
            }
        }

        //---------------------------------------------------------------------
        // ProcessIds.
        //---------------------------------------------------------------------

        [Test]
        public void ProcessIds_WhenJobEmpty_ThenProcessIdsIsEmpty()
        {
            using (var job = new Win32Job(true))
            {
                CollectionAssert.IsEmpty(job.ProcessIds);
            }
        }

        [Test]
        public void ProcessIds_WhenJobContainsProcesses_ThenProcessIdsReturnsId()
        {
            var factory = new Win32ProcessFactory();

            using (var job = new Win32Job(true))
            using (var process1 = factory.CreateProcess(CmdExe, null))
            using (var process2 = factory.CreateProcess(CmdExe, null))
            {
                job.Add(process1);
                job.Add(process2);

                var ids = job.ProcessIds;
                Assert.That(ids.Count(), Is.EqualTo(2));
                CollectionAssert.Contains(ids, process1.Id);
                CollectionAssert.Contains(ids, process2.Id);
            }
        }

        //---------------------------------------------------------------------
        // WaitForProcesses.
        //---------------------------------------------------------------------

        [Test]
        public async Task WaitForProcesses_WhenJobEmpty_ThenWaitReturns()
        {
            using (var job = new Win32Job(true))
            {
                await job
                    .WaitForProcessesAsync(TimeSpan.MaxValue, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public void WaitForProcesses_WhenTimeoutElapses_ThenWaitThrowsException()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            using (var job = new Win32Job(true))
            {
                job.Add(process);

                ExceptionAssert.ThrowsAggregateException<TimeoutException>(
                    () => job
                    .WaitForProcessesAsync(TimeSpan.FromMilliseconds(1), CancellationToken.None)
                    .Wait());
            }
        }

        [Test]
        public async Task WaitForProcesses_WhenProcessTerminates_ThenWaitReturns()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            using (var job = new Win32Job(true))
            {
                job.Add(process);

                var waitTask = job.WaitForProcessesAsync(TimeSpan.MaxValue, CancellationToken.None);
                Assert.IsFalse(waitTask.IsCompleted);

                process.Resume();
                process.Terminate(0);

                await waitTask.ConfigureAwait(false);
            }
        }
    }
}
