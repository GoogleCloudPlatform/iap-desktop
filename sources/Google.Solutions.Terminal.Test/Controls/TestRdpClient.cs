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
using Microsoft.VisualBasic;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Test.Controls
{
    [TestFixture]
    [RequiresInteraction]
    [Apartment(ApartmentState.STA)]
    public class TestRdpClient
    {
        private const string InvalidServer = "8.8.8.8";

        private static string? serverName;
        private static string? username;
        private static string? password;

        [OneTimeSetUp]
        public static void CollectCredentials()
        {
            serverName = Interaction.InputBox("RDP server");
            username = Interaction.InputBox("RDP username");
            password = Interaction.InputBox("RDP password");
        }

        private static ClientDiagnosticsWindow<RdpClient> CreateWindow()
        {
            var window = new ClientDiagnosticsWindow<RdpClient>(new RdpClient());
            window.Client.MainWindow = window;
            window.Client.Username = $".\\{username}";
            window.Client.Password = password;
            window.Client.Server = Dns.GetHostEntry(serverName)
                .AddressList
                .First()
                .ToString();
            return window;
        }

        [WindowsFormsTest]
        public async Task ResizeWindow(
            [Values(true, false)] bool enableAutoResize)
        {
            using (var window = CreateWindow())
            {
                window.Client.EnableAutoResize = enableAutoResize;
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Maximize.
                //
                window.WindowState = FormWindowState.Maximized;
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Restore.
                //
                window.WindowState = FormWindowState.Normal;
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Disonnect.
                //
                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task MinimizeAndRestoreFullScreenWindow(
            [Values(true, false)] bool enableAutoResize)
        {
            using (var window = CreateWindow())
            {
                window.Client.EnableAutoResize = enableAutoResize;
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Enter full-screen.
                //
                Assert.IsFalse(window.Client.IsFullScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.TryEnterFullScreen(null));
                Assert.IsTrue(window.Client.IsFullScreen);

                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);
                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Minimize.
                //
                window.WindowState = FormWindowState.Minimized;
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Restore.
                //
                window.WindowState = FormWindowState.Normal;
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Disonnect.
                //
                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task EnterAndLeaveFullscreen(
            [Values(true, false)] bool enableAutoResize)
        {
            using (var window = CreateWindow())
            {
                window.Client.EnableAutoResize = enableAutoResize;
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Enter full-screen.
                //
                Assert.IsFalse(window.Client.IsFullScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.TryEnterFullScreen(null));

                await Task.Delay(TimeSpan.FromSeconds(1));

                Assert.IsTrue(window.Client.IsFullScreen);
                Assert.IsFalse(window.Client.CanEnterFullScreen);

                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);
                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Leave full-screen.
                //
                Assert.IsTrue(window.Client.TryLeaveFullScreen());
                Assert.IsFalse(window.Client.IsFullScreen);

                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);
                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Enter full-screen again.
                //
                Assert.IsFalse(window.Client.IsFullScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.TryEnterFullScreen(null));
                Assert.IsTrue(window.Client.IsFullScreen);

                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);
                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Leave full-screen again.
                //
                Assert.IsTrue(window.Client.TryLeaveFullScreen());
                Assert.IsFalse(window.Client.IsFullScreen);

                //
                // Disonnect.
                //
                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task Close_RaisesEvent()
        {
            using (var window = CreateWindow())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                RdpClient.ConnectionClosedEventArgs? eventArgs = null;
                window.Client.ConnectionClosed += (_, e) => eventArgs = e;

                //
                // Close window.
                //
                window.Close();

                await window.Client
                    .AwaitStateAsync(ConnectionState.NotConnected)
                    .ConfigureAwait(true);

                Assert.NotNull(eventArgs);
                Assert.AreEqual(RdpClient.DisconnectReason.FormClosed, eventArgs!.Reason);
            }
        }

        [WindowsFormsTest]
        public async Task Connect_WhenServerInvalid_ThenRaisesEvent()
        {
            using (var window = CreateWindow())
            {
                window.Client.Server = InvalidServer;
                window.Client.ConnectionTimeout = TimeSpan.FromSeconds(1);

                window.Show();

                ExceptionEventArgs? eventArgs = null;
                window.Client.ConnectionFailed += (_, e) => eventArgs = e;

                //
                // Connect to non-existing server.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ConnectionState.NotConnected)
                    .ConfigureAwait(true);

                Assert.NotNull(eventArgs);
                Assert.IsInstanceOf<RdpDisconnectedException>(eventArgs!.Exception);

                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task Logoff()
        {
            using (var window = CreateWindow())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                window.Client.Logoff();

                await window.Client
                    .AwaitStateAsync(ConnectionState.NotConnected)
                    .ConfigureAwait(true);

                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task Reconnect()
        {
            using (var window = CreateWindow())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                var connectionClosedEvents = 0;
                var expectedReason = ClientBase.DisconnectReason.ReconnectInitiatedByUser;
                window.Client.ConnectionClosed += (_, args) =>
                {
                    connectionClosedEvents++;
                    Assert.AreEqual(expectedReason, args.Reason);
                };

                for (var i = 0; i < 5; i++)
                {
                    window.Client.Reconnect();

                    await window.Client
                        .AwaitStateAsync(ConnectionState.NotConnected)
                        .ConfigureAwait(true);
                    await window.Client
                        .AwaitStateAsync(ConnectionState.LoggedOn)
                        .ConfigureAwait(true);
                }

                Assert.AreEqual(5, connectionClosedEvents);

                expectedReason = ClientBase.DisconnectReason.FormClosed;
                window.Close();
            }
        }
    }
}
