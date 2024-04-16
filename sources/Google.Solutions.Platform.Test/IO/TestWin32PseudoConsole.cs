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

using Google.Solutions.Platform.IO;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.Test.IO
{
    [TestFixture]
    public class TestWin32PseudoConsole
    {
        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenClosed_ThenStreamsAreNotClosedYet()
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
        public void WhenDisposed_ThenStreamsAreClosed()
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
