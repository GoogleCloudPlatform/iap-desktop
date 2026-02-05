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
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.Test.Dispatch
{
    [TestFixture]
    public class TestWin32ChildProcessFactory
    {
        private static readonly string CmdExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\cmd.exe";

        //---------------------------------------------------------------------
        // Job.
        //---------------------------------------------------------------------

        [Test]
        public void Job()
        {
            using (var factory = new Win32ChildProcessFactory(true))
            using (var process = factory.CreateProcess(CmdExe, null))
            {
                Assert.IsNotNull(process.Job);
                Assert.IsTrue(process.Job!.Contains(process));
                Assert.IsTrue(process.Job!.Contains(process.Id));
            }
        }

        [Test]
        public void Jobs_JobsArePerProcess()
        {
            using (var factory = new Win32ChildProcessFactory(true))
            using (var process1 = factory.CreateProcess(CmdExe, null))
            using (var process2 = factory.CreateProcess(CmdExe, null))
            {
                Assert.That(process2.Job, Is.Not.SameAs(process1.Job));
            }
        }

        //---------------------------------------------------------------------
        // Contains.
        //---------------------------------------------------------------------

        [Test]
        public void Contains_WhenProcessFound_ThenContainsReturnsTrue()
        {
            using (var factory = new Win32ChildProcessFactory(true))
            using (var process = factory.CreateProcess(CmdExe, null))
            {
                Assert.IsTrue(factory.Contains(process));
                Assert.IsTrue(factory.Contains(process.Id));
            }
        }

        [Test]
        public void Contains_WhenProcessNotFound_ThenContainsReturnsFalse()
        {
            using (var factory = new Win32ChildProcessFactory(true))
            {
                Assert.That(factory.Contains(4), Is.False);
            }
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task Close_WhenChildTerminated_ThenCloseReturns()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            using (var factory = new Win32ChildProcessFactory(false))
            {
                using (var process = factory.CreateProcess(CmdExe, null))
                {
                    process.Terminate(0);
                }

                var closed = await factory
                    .CloseAsync(cts.Token)
                    .ConfigureAwait(false);

                Assert.That(closed, Is.EqualTo(0));
            }
        }

        [Test]
        public async Task Close_WhenChildRunning_ThenCloseReturns()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            using (var factory = new Win32ChildProcessFactory(false))
            using (var process = factory.CreateProcess(CmdExe, null))
            {
                var closed = await factory
                    .CloseAsync(cts.Token)
                    .ConfigureAwait(false);

                Assert.That(closed, Is.EqualTo(1));
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_WhenTerminateOnCloseIsFalse_ThenDisposeKeepsProcesses()
        {
            var factory = new Win32ChildProcessFactory(false);
            var process = factory.CreateProcess(CmdExe, null);
            process.Resume();

            Assert.IsTrue(process.IsRunning);
            factory.Dispose();

            Assert.IsTrue(process.IsRunning);
            process.Terminate(0);
            process.Dispose();
        }

        [Test]
        public void Dispose_WhenTerminateOnCloseIsTrue_ThenDisposeTerminatesProcesses()
        {
            var factory = new Win32ChildProcessFactory(true);
            var process = factory.CreateProcess(CmdExe, null);
            process.Resume();

            Assert.IsTrue(process.IsRunning);
            factory.Dispose();

            Assert.That(process.IsRunning, Is.False);
            process.Dispose();
        }
    }
}
