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
        public async Task OutputAvailable_WhenProcessWritesToStdout_ThenOutputAvailableIsRaised()
        {
            var factory = new Win32ProcessFactory();
            using (var process = factory.CreateProcessWithPseudoConsole(
                "powershell.exe",
                null,
                PseudoTerminalSize.Default))
            {
                Assert.That(process.PseudoTerminal, Is.Not.Null);
                var pty = process.PseudoTerminal!;

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

                Assert.That(output.ToString(), Does.Contain("this is a test"));
            }
        }

        //---------------------------------------------------------------------
        // Disconnected.
        //---------------------------------------------------------------------

        [Test]
        public async Task Disconnected_WhenProcessTerminated_ThenDisconnectedIsRaised()
        {
            var factory = new Win32ProcessFactory();
            using (var process = factory.CreateProcessWithPseudoConsole(
                "powershell.exe",
                null,
                PseudoTerminalSize.Default))
            {
                Assert.That(process.PseudoTerminal, Is.Not.Null);
                var pty = process.PseudoTerminal!;

                var eventRaised = false;
                pty.Disconnected += (_, __) => eventRaised = true;

                process.Terminate(1);

                await pty.DrainAsync().ConfigureAwait(false);

                Assert.That(eventRaised, Is.True);
            }
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task Close_KeepsStreamOpen()
        {
            using (var pty = new Win32PseudoConsole(new PseudoTerminalSize(80, 24)))
            {
                Assert.That(pty.IsClosed, Is.False);

                await pty
                    .CloseAsync()
                    .ConfigureAwait(false);

                Assert.That(pty.IsClosed, Is.True);
                Assert.That(pty.Handle.IsClosed, Is.True);

                Assert.That(pty.InputPipe.WriteSideHandle.IsClosed, Is.False);
                Assert.That(pty.OutputPipe.ReadSideHandle.IsClosed, Is.False);
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_TerminatesProcess()
        {
            var factory = new Win32ProcessFactory();
            using (var process = factory.CreateProcessWithPseudoConsole(
                "powershell.exe",
                null,
                PseudoTerminalSize.Default))
            {
                Assert.That(process.PseudoTerminal, Is.Not.Null);
                using (var pty = process.PseudoTerminal!)
                {
                }
            }
        }

        [Test]
        public void Dispose_ClosesStreams()
        {
            var pty = new Win32PseudoConsole(new PseudoTerminalSize(80, 24));
            pty.Dispose();

            Assert.That(pty.IsClosed, Is.True, "Close Pty");
            Assert.That(pty.Handle.IsClosed, Is.True, "Close handle");

            Assert.That(pty.InputPipe.ReadSideHandle.IsClosed, Is.True, "Close input/read");
            Assert.That(pty.InputPipe.WriteSideHandle.IsClosed, Is.True, "Close input/write");
            Assert.That(pty.OutputPipe.ReadSideHandle.IsClosed, Is.True, "Close output/read");
            Assert.That(pty.OutputPipe.WriteSideHandle.IsClosed, Is.True, "Close output/write");
        }
    }
}
