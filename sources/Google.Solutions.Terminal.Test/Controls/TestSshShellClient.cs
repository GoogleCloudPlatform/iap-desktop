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

using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Terminal.Controls;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Test.Controls
{
    [TestFixture]
    [RequiresInteraction]
    [Apartment(ApartmentState.STA)]
    public class TestSshShellClient : SshClientFixtureBase
    {
        private const string InvalidServer = "8.8.8.8";

        [WindowsFormsTest]
        public async Task ResizeWindow()
        {
            using (var window = CreateWindow<SshShellClient>())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ClientState.LoggedOn, CancellationToken.None)
                    .ConfigureAwait(true);

                //
                // Maximize.
                //
                window.WindowState = FormWindowState.Maximized;
                await window.Client
                    .AwaitStateAsync(ClientState.LoggedOn, CancellationToken.None)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Minimize.
                //
                window.WindowState = FormWindowState.Minimized;
                await window.Client
                    .AwaitStateAsync(ClientState.LoggedOn, CancellationToken.None)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Restore.
                //
                window.WindowState = FormWindowState.Normal;
                await window.Client
                    .AwaitStateAsync(ClientState.LoggedOn, CancellationToken.None)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Disonnect.
                //
                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task Connect_WhenServerInvalid_ThenRaisesEvent()
        {
            using (var window = CreateWindow<SshShellClient>())
            {
                window.Client.ServerEndpoint = new IPEndPoint(
                    IPAddress.Parse(InvalidServer),
                    22);
                window.Client.ConnectionTimeout = TimeSpan.FromSeconds(1);

                window.Show();

                ExceptionEventArgs? eventArgs = null;
                window.Client.ConnectionFailed += (_, e) => eventArgs = e;

                //
                // Connect to non-existing server.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ClientState.NotConnected, CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.That(eventArgs, Is.Not.Null);
                Assert.That(eventArgs!.Exception, Is.InstanceOf<SocketException>());

                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task Close_RaisesEvent()
        {
            using (var window = CreateWindow<SshShellClient>())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ClientState.LoggedOn, CancellationToken.None)
                    .ConfigureAwait(true);

                ClientBase.ConnectionClosedEventArgs? eventArgs = null;
                window.Client.ConnectionClosed += (_, e) => eventArgs = e;

                //
                // Close window.
                //
                window.Close();

                await window.Client
                    .AwaitStateAsync(ClientState.NotConnected, CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.That(eventArgs, Is.Not.Null);
                Assert.That(eventArgs!.Reason, Is.EqualTo(ClientBase.DisconnectReason.FormClosed));
            }
        }

        [WindowsFormsTest]
        public async Task Logoff()
        {
            using (var window = CreateWindow<SshShellClient>())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ClientState.LoggedOn, CancellationToken.None)
                    .ConfigureAwait(true);

                window.Client.SendText("exit\r\n");

                await window.Client
                    .AwaitStateAsync(ClientState.NotConnected, CancellationToken.None)
                    .ConfigureAwait(true);

                window.Close();
            }
        }
    }
}
