//
// Copyright 2024 Google LLC
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
using Google.Solutions.Platform.IO;
using NUnit.Framework;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.Test.Dispatch
{
    [TestFixture]
    public class TestWin32PseudoConsole
    {
        //---------------------------------------------------------------------
        // OutputAvailable.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProcessWritesToStdout_ThenOutputAvailableIsRaised()
        {
            var factory = new Win32ProcessFactory();
            using (var process = factory.CreateProcessWithPseudoConsole(
                "powershell.exe",
                null,
                PseudoConsoleSize.Default))
            {
                Assert.IsNotNull(process.PseudoConsole);
                var pty = process.PseudoConsole!;
                
                var output = new StringBuilder();
                pty.OutputAvailable += (_, args) =>
                {
                    output.Append(args.Data);
                };

                process.Resume();

                var command = "Write-Host 'this is a test'";
                await pty
                    .WriteAsync(command + "\r\n", CancellationToken.None)
                    .ConfigureAwait(false);
                await pty
                    .WriteAsync("exit\r\n", CancellationToken.None)
                    .ConfigureAwait(false);

                await pty
                    .DrainAsync()
                    .ConfigureAwait(false);

                StringAssert.Contains("this is a test", output.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Disconnected.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProcessTerminated_ThenDisconnectedIsRaised()
        {
            var factory = new Win32ProcessFactory();
            using (var process = factory.CreateProcessWithPseudoConsole(
                "powershell.exe",
                null,
                PseudoConsoleSize.Default))
            {
                Assert.IsNotNull(process.PseudoConsole);
                var pty = process.PseudoConsole!;

                var eventRaised = false;
                pty.Disconnected += (_, __) => eventRaised = true;

                process.Terminate(1);

                await pty.DrainAsync().ConfigureAwait(false);

                Assert.IsTrue(eventRaised);
            }
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task CloseKeepStreamOpen()
        {
            using (var pty = new Win32PseudoConsole(new PseudoConsoleSize(80, 24)))
            {
                Assert.IsFalse(pty.IsClosed);

                await pty
                    .CloseAsync()
                    .ConfigureAwait(false);

                Assert.IsTrue(pty.IsClosed);
                Assert.IsTrue(pty.Handle.IsClosed);

                Assert.IsFalse(pty.InputPipe.WriteSideHandle.IsClosed);
                Assert.IsFalse(pty.OutputPipe.ReadSideHandle.IsClosed);
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void DisposeTerminatesProcess()
        {
            var factory = new Win32ProcessFactory();
            using (var process = factory.CreateProcessWithPseudoConsole(
                "powershell.exe",
                null,
                PseudoConsoleSize.Default))
            {
                Assert.IsNotNull(process.PseudoConsole);
                using (var pty = process.PseudoConsole!)
                {
                }
            }
        }

        [Test]
        public void DisposeClosesStreams()
        {
            var pty = new Win32PseudoConsole(new PseudoConsoleSize(80, 24));
            pty.Dispose();

            Assert.IsTrue(pty.IsClosed);
            Assert.IsTrue(pty.Handle.IsClosed);

            Assert.IsTrue(pty.InputPipe.ReadSideHandle.IsClosed);
            Assert.IsTrue(pty.InputPipe.WriteSideHandle.IsClosed);
            Assert.IsTrue(pty.OutputPipe.ReadSideHandle.IsClosed);
            Assert.IsTrue(pty.OutputPipe.WriteSideHandle.IsClosed);
        }
    }
}
