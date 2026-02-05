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
using Google.Solutions.Terminal.Controls;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Test.Controls
{
    [TestFixture]
    public class TestPseudoTerminalClientBase
    {
        private class SampleClient : PseudoTerminalClientBase
        {
            public delegate IPseudoTerminal ConnectCoreDelegate(PseudoTerminalSize initialSize);

            private readonly ConnectCoreDelegate connectCore;

            public SampleClient(ConnectCoreDelegate connectCore)
            {
                this.connectCore = connectCore;
            }

            protected override Task<IPseudoTerminal> ConnectCoreAsync(
                PseudoTerminalSize initialSize)
            {
                return Task.FromResult(this.connectCore(initialSize));
            }

            protected override bool IsCausedByConnectionTimeout(Exception e)
            {
                return e is TimeoutException;
            }
        }

        //----------------------------------------------------------------------
        // State.
        //----------------------------------------------------------------------

        [WindowsFormsTest]
        public void State_WhenConnectSucceeded()
        {
            using (var form = new Form())
            {
                var pty = new Mock<IPseudoTerminal>();
                var client = new SampleClient(_ => pty.Object);
                form.Controls.Add(client);
                form.Show();

                Assert.That(client.State, Is.EqualTo(ClientState.NotConnected));

                client.Connect();

                Assert.That(client.State, Is.EqualTo(ClientState.LoggedOn));
            }
        }

        [WindowsFormsTest]
        public void State_WhenConnectFailed()
        {
            using (var form = new Form())
            {
                var client = new SampleClient(_ => throw new TimeoutException("mock"));
                form.Controls.Add(client);
                form.Show();

                Assert.That(client.State, Is.EqualTo(ClientState.NotConnected));

                var connectionFailedEvents = 0;
                client.ConnectionFailed += (_, args) => connectionFailedEvents++;

                client.Connect();

                Assert.That(client.State, Is.EqualTo(ClientState.NotConnected));
                Assert.That(connectionFailedEvents, Is.EqualTo(1));
            }
        }

        [WindowsFormsTest]
        public void State_WhenFormClosed_ThenIsNotConnected()
        {
            using (var form = new Form())
            {
                var pty = new Mock<IPseudoTerminal>();
                var client = new SampleClient(_ => pty.Object);
                form.Controls.Add(client);
                form.Show();

                Assert.That(client.State, Is.EqualTo(ClientState.NotConnected));

                client.Connect();
                Assert.That(client.State, Is.EqualTo(ClientState.LoggedOn));

                form.Close();

                Assert.That(client.State, Is.EqualTo(ClientState.NotConnected));
            }
        }

        [WindowsFormsTest]
        public void State_WhenDeviceRaisesFatalError_ThenIsNotConnected()
        {
            using (var form = new Form())
            {
                var pty = new Mock<IPseudoTerminal>();
                var client = new SampleClient(_ => pty.Object);
                form.Controls.Add(client);
                form.Show();

                Assert.That(client.State, Is.EqualTo(ClientState.NotConnected));

                client.Connect();
                Assert.That(client.State, Is.EqualTo(ClientState.LoggedOn));

                var connectionFailedEvents = 0;
                client.ConnectionFailed += (_, args) => connectionFailedEvents++;

                pty.Raise(p => p.FatalError += null, new PseudoTerminalErrorEventArgs(new Exception("mock")));

                Assert.That(client.State, Is.EqualTo(ClientState.NotConnected));
                Assert.That(connectionFailedEvents, Is.EqualTo(1));
            }
        }

        [WindowsFormsTest]
        public void State_WhenDeviceDisconnected_ThenIsNotConnected()
        {
            using (var form = new Form())
            {
                var pty = new Mock<IPseudoTerminal>();
                var client = new SampleClient(_ => pty.Object);
                form.Controls.Add(client);
                form.Show();

                Assert.That(client.State, Is.EqualTo(ClientState.NotConnected));

                client.Connect();
                Assert.That(client.State, Is.EqualTo(ClientState.LoggedOn));

                var connectionClosedEvents = 0;
                client.ConnectionClosed += (_, args) => connectionClosedEvents++;

                pty.Raise(p => p.Disconnected += null, EventArgs.Empty);

                Assert.That(client.State, Is.EqualTo(ClientState.NotConnected));
                Assert.That(connectionClosedEvents, Is.EqualTo(1));
            }
        }

        //----------------------------------------------------------------------
        // SendText.
        //----------------------------------------------------------------------

        [WindowsFormsTest]
        public void SendText_WhenNotLoggedOn()
        {

            using (var form = new Form())
            {
                var pty = new Mock<IPseudoTerminal>();
                var client = new SampleClient(_ => pty.Object);
                form.Controls.Add(client);
                form.Show();

                Assert.That(client.State, Is.EqualTo(ClientState.NotConnected));

                Assert.Throws<InvalidOperationException>(
                    () => client.SendText("test"));
            }
        }
    }
}
