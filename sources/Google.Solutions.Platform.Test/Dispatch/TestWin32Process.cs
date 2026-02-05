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
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.Test.Dispatch
{
    [TestFixture]
    public class TestWin32Process
    {
        private const uint SystemProcessId = 4;

        private static readonly string CmdExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\cmd.exe";

        private static readonly string NotepadExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\notepad.exe";

        //---------------------------------------------------------------------
        // ImageName.
        //---------------------------------------------------------------------

        [Test]
        public void ImageName()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                Assert.That(process.ImageName, Is.EqualTo("cmd.exe"));

                process.Terminate(1);
            }
        }

        //---------------------------------------------------------------------
        // Id.
        //---------------------------------------------------------------------

        [Test]
        public void Id()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                Assert.That(process.Id, Is.Not.EqualTo(0));

                process.Terminate(1);
            }
        }


        //---------------------------------------------------------------------
        // IsRunning.
        //---------------------------------------------------------------------

        [Test]
        public void IsRunning()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                Assert.That(process.IsRunning, Is.True);

                process.Resume();
                Assert.That(process.IsRunning, Is.True);

                process.Terminate(1);
                Assert.That(process.IsRunning, Is.False);
            }
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_ContainsIdAndImageName()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                Assert.That(process.ToString(), Does.Contain("cmd.exe"));
                Assert.That(process.ToString(), Does.Contain(process.Id.ToString()));

                process.Terminate(1);
            }
        }

        //---------------------------------------------------------------------
        // Session.
        //---------------------------------------------------------------------

        [Test]
        public void Session()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                Assert.That(WtsSession.GetCurrent(), Is.EqualTo(process.Session));

                process.Terminate(1);
            }
        }

        //---------------------------------------------------------------------
        // WaitHandle.
        //---------------------------------------------------------------------

        [Test]
        public void WaitHandle_ReturnsHandle()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                Assert.IsNotNull(process.WaitHandle);
                Assert.That(process.WaitHandle.WaitOne(1), Is.False);

                process.Terminate(1);
                Assert.That(process.WaitHandle.WaitOne(50), Is.True);
            }
        }

        //---------------------------------------------------------------------
        // Resume.
        //---------------------------------------------------------------------

        [Test]
        public void Resume_WhenResumedAlready()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                Assert.IsNotNull(process.Handle);
                Assert.That(process.Handle.IsInvalid, Is.False);

                process.Resume();
                process.Resume(); // Again.

                Assert.That(process.IsRunning, Is.True);

                process.Terminate(1);
            }
        }

        //---------------------------------------------------------------------
        // Terminate.
        //---------------------------------------------------------------------

        [Test]
        public void Terminate_WhenTerminatedAlready_ThenTerminateThrowsException()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                process.Resume();

                Assert.IsNotNull(process.Handle);
                Assert.That(process.Handle.IsInvalid, Is.False);
                Assert.That(process.IsRunning, Is.True);

                process.Terminate(1);

                Assert.That(process.IsRunning, Is.False);

                Assert.Throws<DispatchException>(() => process.Terminate(1));
            }
        }

        //---------------------------------------------------------------------
        // Exited.
        //---------------------------------------------------------------------

        [Test]
        public async Task Exited_WhenTerminated_ThenExitedEventIsRaised()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                var eventRaised = new TaskCompletionSource<object?>();
                process.Exited += (_, __) => eventRaised.SetResult(null);

                process.Resume();

                Assert.IsNotNull(process.Handle);
                Assert.That(process.Handle.IsInvalid, Is.False);
                Assert.That(process.IsRunning, Is.True);

                process.Terminate(1);

                await eventRaised.Task.ConfigureAwait(false);
            }
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task Close_WhenProcessHasNoWindows_ThenCloseReturnsTrue()
        {
            var factory = new Win32ProcessFactory();

            using (var cts = new CancellationTokenSource(TimeSpan.Zero))
            using (var process = factory.CreateProcess(CmdExe, null))
            {
                process.Resume();
                Assert.That(process.WindowCount, Is.EqualTo(0));

                var terminatedGracefully = await process
                    .CloseAsync(cts.Token)
                    .ConfigureAwait(false);

                Assert.That(terminatedGracefully, Is.True);
            }
        }

        [Test]
        public async Task Close_WhenProcessHasWindows_ThenCloseReturnsTrue()
        {
            var factory = new Win32ProcessFactory();

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            using (var process = factory.CreateProcess(NotepadExe, null))
            {
                process.Resume();

                //
                // Give notepad some time to create its window.
                //
                for (var i = 0; i < 100 && process.WindowCount == 0; i++)
                {
                    Thread.Sleep(10);
                }

                var terminatedGracefully = await process
                    .CloseAsync(cts.Token)
                    .ConfigureAwait(false);
                Assert.That(terminatedGracefully, Is.True);
            }
        }

        [Test]
        public async Task Close_WhenProcessTerminated_ThenCloseReturnsTrue()
        {
            var factory = new Win32ProcessFactory();

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            using (var process = factory.CreateProcess(NotepadExe, null))
            {
                process.Terminate(0);


                var terminatedGracefully = await process
                    .CloseAsync(cts.Token)
                    .ConfigureAwait(false);
                Assert.That(terminatedGracefully, Is.True);
            }
        }

        //---------------------------------------------------------------------
        // Wait.
        //---------------------------------------------------------------------

        [Test]
        public void Wait_WhenTimeoutElapses_ThenWaitThrowsException()
        {
            var factory = new Win32ProcessFactory();

            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5)))
            using (var process = factory.CreateProcess(CmdExe, null))
            {
                process.Resume();

                ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                    () => process
                        .WaitAsync(cts.Token)
                        .Wait());

                process.Terminate(0);
            }
        }

        [Test]
        public void Wait_WhenCancelled_ThenWaitThrowsException()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            using (var cts = new CancellationTokenSource())
            {
                var task = process.WaitAsync(cts.Token);

                cts.Cancel();

                ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                    () => task.Wait());

                process.Terminate(0);
            }
        }

        [Test]
        public async Task Wait_WhenProcessTerminated_ThenWaitSucceeds()
        {
            var factory = new Win32ProcessFactory();

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
            using (var process = factory.CreateProcess(CmdExe, null))
            {
                process.Resume();
                process.Terminate(1);

                var exitCode = await process
                    .WaitAsync(cts.Token)
                    .ConfigureAwait(false);

                Assert.That(exitCode, Is.EqualTo(1));
            }
        }

        //---------------------------------------------------------------------
        // FromProcessId.
        //---------------------------------------------------------------------

        [Test]
        public void FromProcessId_WhenProcessAccessible_ThenFromProcessIdReturnsProcess()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(
                CmdExe,
                null))
            {
                process.Resume();

                using (var openedProcess = Win32Process.FromProcessId(process.Id))
                {
                    Assert.IsNotNull(openedProcess);
                    Assert.That(openedProcess.ImageName, Is.EqualTo(process.ImageName));
                    Assert.That(openedProcess.IsRunning, Is.True);
                }

                process.Terminate(1);
            }
        }

        [Test]
        public void FromProcessId_WhenProcessInccessible_ThenFromProcessThrowsException()
        {
            var factory = new Win32ProcessFactory();

            Assert.Throws<DispatchException>(
                () => Win32Process.FromProcessId(SystemProcessId));
        }

        [Test]
        public void FromProcessId_WhenProcessOpenedById_ThenResumeThrowsException()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(
                CmdExe,
                null))
            {
                process.Resume();

                using (var openedProcess = Win32Process.FromProcessId(process.Id))
                {
                    Assert.Throws<DispatchException>(() => openedProcess.Resume());
                }

                process.Terminate(1);
            }
        }

        [Test]
        public void FromProcessId_WhenProcessIdDoesNotExist_ThenFromProcessThrowsException()
        {
            var factory = new Win32ProcessFactory();

            Assert.Throws<DispatchException>(
                () => Win32Process.FromProcessId(1));
        }
    }
}
