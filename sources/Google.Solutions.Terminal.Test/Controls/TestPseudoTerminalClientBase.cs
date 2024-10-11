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

            protected override IPseudoTerminal ConnectCore(PseudoTerminalSize initialSize)
            {
                return this.connectCore(initialSize);
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

                Assert.AreEqual(ClientBase.ConnectionState.NotConnected, client.State);

                client.Connect();

                Assert.AreEqual(ClientBase.ConnectionState.LoggedOn, client.State);
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

                Assert.AreEqual(ClientBase.ConnectionState.NotConnected, client.State);

                int connectionFailedEvents = 0;
                client.ConnectionFailed += (_, args) => connectionFailedEvents++;

                client.Connect();

                Assert.AreEqual(ClientBase.ConnectionState.NotConnected, client.State);
                Assert.AreEqual(1, connectionFailedEvents);
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

                Assert.AreEqual(ClientBase.ConnectionState.NotConnected, client.State);

                client.Connect();
                Assert.AreEqual(ClientBase.ConnectionState.LoggedOn, client.State);

                form.Close();

                Assert.AreEqual(ClientBase.ConnectionState.NotConnected, client.State);
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

                Assert.AreEqual(ClientBase.ConnectionState.NotConnected, client.State);

                client.Connect();
                Assert.AreEqual(ClientBase.ConnectionState.LoggedOn, client.State);

                int connectionFailedEvents = 0;
                client.ConnectionFailed += (_, args) => connectionFailedEvents++;

                pty.Raise(p => p.FatalError += null, new PseudoTerminalErrorEventArgs(new Exception("mock")));

                Assert.AreEqual(ClientBase.ConnectionState.NotConnected, client.State);
                Assert.AreEqual(1, connectionFailedEvents);
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

                Assert.AreEqual(ClientBase.ConnectionState.NotConnected, client.State);

                client.Connect();
                Assert.AreEqual(ClientBase.ConnectionState.LoggedOn, client.State);

                int connectionClosedEvents = 0;
                client.ConnectionClosed += (_, args) => connectionClosedEvents++;

                pty.Raise(p => p.Disconnected += null, EventArgs.Empty);

                Assert.AreEqual(ClientBase.ConnectionState.NotConnected, client.State);
                Assert.AreEqual(1, connectionClosedEvents);
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

                Assert.AreEqual(ClientBase.ConnectionState.NotConnected, client.State);

                Assert.Throws<InvalidOperationException>(
                    () => client.SendText("test"));
            }
        }
    }
}
