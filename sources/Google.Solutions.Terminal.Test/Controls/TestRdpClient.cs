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
            window.Client.Domain = ".";
            window.Client.Username = username;
            window.Client.Password = password;
            window.Client.Server = Dns.GetHostEntry(serverName)
                .AddressList
                .First()
                .ToString();
            return window;
        }

        private static async Task AwaitStateAsync(
            ClientDiagnosticsWindow<RdpClient> window,
            ClientState state)
        {
            await window.Client
                .AwaitStateAsync(state, CancellationToken.None)
                .ConfigureAwait(true);
            await Task
                .Delay(TimeSpan.FromSeconds(1))
                .ConfigureAwait(true);
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
                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Maximize.
                //
                window.WindowState = FormWindowState.Maximized;
                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Restore.
                //
                window.WindowState = FormWindowState.Normal;
                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Disconnect.
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
                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Enter full-screen.
                //
                Assert.IsFalse(window.Client.IsFullScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.TryEnterFullScreen(null));
                Assert.IsTrue(window.Client.IsFullScreen);

                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Minimize.
                //
                window.WindowState = FormWindowState.Minimized;
                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Restore.
                //
                window.WindowState = FormWindowState.Normal;
                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Disconnect.
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
                await AwaitStateAsync(window, ClientState.LoggedOn)
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

                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Leave full-screen.
                //
                Assert.IsTrue(window.Client.TryLeaveFullScreen());
                Assert.IsFalse(window.Client.IsFullScreen);

                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Enter full-screen again.
                //
                Assert.IsFalse(window.Client.IsFullScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.TryEnterFullScreen(null));
                Assert.IsTrue(window.Client.IsFullScreen);

                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Leave full-screen again.
                //
                Assert.IsTrue(window.Client.TryLeaveFullScreen());
                Assert.IsFalse(window.Client.IsFullScreen);

                //
                // Disconnect.
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
                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                RdpClient.ConnectionClosedEventArgs? eventArgs = null;
                window.Client.ConnectionClosed += (_, e) => eventArgs = e;

                //
                // Close window.
                //
                window.Close();

                await AwaitStateAsync(window, ClientState.NotConnected)
                    .ConfigureAwait(true);

                Assert.NotNull(eventArgs);
                Assert.AreEqual(RdpClient.DisconnectReason.FormClosed, eventArgs!.Reason);
            }
        }

        [WindowsFormsTest]
        public async Task Connect_WhenServerInvalid()
        {
            using (var window = CreateWindow())
            {
                window.Client.Server = InvalidServer;
                window.Client.ConnectionTimeout = TimeSpan.FromSeconds(1);
                window.Silent = true;

                window.Show();

                //
                // Connect to non-existing server.
                //
                window.Client.Connect();

                await ExceptionAssert.ThrowsAsync<RdpDisconnectedException>(
                    () => AwaitStateAsync(window, ClientState.Connected))
                    .ConfigureAwait(true);

                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task AwaitState_WhenCancelled()
        {
            using (var window = CreateWindow())
            {
                window.Client.Server = InvalidServer;
                window.Client.ConnectionTimeout = TimeSpan.FromSeconds(1);
                window.Silent = true;

                window.Show();

                //
                // Cancel before connecting.
                //
                var tokenSource = new CancellationTokenSource();
                tokenSource.CancelAfter(TimeSpan.FromMilliseconds(10));

                await ExceptionAssert.ThrowsAsync<TaskCanceledException>(
                    () => window.Client.AwaitStateAsync(
                        ClientState.Connected,
                        tokenSource.Token))
                    .ConfigureAwait(true);

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
                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                window.Client.Logoff();

                await AwaitStateAsync(window, ClientState.NotConnected)
                    .ConfigureAwait(true);

                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task CanXxx()
        {
            using (var window = CreateWindow())
            {
                window.Show();

                Assert.IsFalse(window.Client.CanSendText);
                Assert.IsFalse(window.Client.CanShowTaskManager);
                Assert.IsFalse(window.Client.CanShowSecurityScreen);
                Assert.IsFalse(window.Client.CanEnterFullScreen);
                Assert.IsFalse(window.Client.CanReconnect);
                Assert.IsFalse(window.Client.CanLogoff);

                //
                // Connect.
                //
                window.Client.Connect();
                await AwaitStateAsync(window, ClientState.LoggedOn)
                    .ConfigureAwait(true);

                Assert.IsTrue(window.Client.CanSendText);
                Assert.IsTrue(window.Client.CanShowTaskManager);
                Assert.IsTrue(window.Client.CanShowSecurityScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.CanReconnect);
                Assert.IsTrue(window.Client.CanLogoff);

                window.Client.Logoff();

                await AwaitStateAsync(window, ClientState.NotConnected)
                    .ConfigureAwait(true);

                Assert.IsFalse(window.Client.CanSendText);
                Assert.IsFalse(window.Client.CanShowTaskManager);
                Assert.IsFalse(window.Client.CanShowSecurityScreen);
                Assert.IsFalse(window.Client.CanEnterFullScreen);
                Assert.IsFalse(window.Client.CanReconnect);
                Assert.IsFalse(window.Client.CanLogoff);

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
                await AwaitStateAsync(window, ClientState.LoggedOn)
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

                    await AwaitStateAsync(window, ClientState.NotConnected)
                        .ConfigureAwait(true);
                    await AwaitStateAsync(window, ClientState.LoggedOn)
                        .ConfigureAwait(true);
                }

                Assert.AreEqual(5, connectionClosedEvents);

                expectedReason = ClientBase.DisconnectReason.FormClosed;
                window.Close();
            }
        }
    }
}
