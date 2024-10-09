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

using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Terminal.Controls;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
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

        private static RdpDiagnosticsWindow CreateWindow()
        {
            var window = new RdpDiagnosticsWindow();
            window.Client.MainWindow = window;
            window.Client.Username = ".\\admin";
            window.Client.Password = "admin";
            window.Client.Server = Dns.GetHostEntry("rdptesthost")
                .AddressList
                .First()
                .ToString();
            return window;
        }

        [WindowsFormsTest]
        public void ShowTestUi()
        {
            using (var window = CreateWindow())
            {
                window.Client.ConnectionFailed += (_, args)
                    => MessageBox.Show(window, args.Exception.FullMessage());

                window.ShowDialog();
            }
        }

        [WindowsFormsTest]
        public async Task ResizeWindow()
        {
            using (var window = CreateWindow())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Maximize.
                //
                window.WindowState = FormWindowState.Maximized;
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Restore.
                //
                window.WindowState = FormWindowState.Normal;
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Disonnect.
                //
                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task MinimizeAndRestoreFullScreenWindow()
        {
            using (var window = CreateWindow())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Enter full-screen.
                //
                Assert.IsFalse(window.Client.IsFullScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.TryEnterFullScreen(null));
                Assert.IsTrue(window.Client.IsFullScreen);

                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);
                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Minimize.
                //
                window.WindowState = FormWindowState.Minimized;
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Restore.
                //
                window.WindowState = FormWindowState.Normal;
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Disonnect.
                //
                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task EnterAndLeaveFullscreen()
        {
            using (var window = CreateWindow())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Enter full-screen.
                //
                Assert.IsFalse(window.Client.IsFullScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.TryEnterFullScreen(null));
                Assert.IsTrue(window.Client.IsFullScreen);
                Assert.IsFalse(window.Client.CanEnterFullScreen);

                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);
                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Leave full-screen.
                //
                Assert.IsTrue(window.Client.TryLeaveFullScreen());
                Assert.IsFalse(window.Client.IsFullScreen);

                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
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
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
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
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                RdpClient.ConnectionClosedEventArgs? eventArgs = null;
                window.Client.ConnectionClosed += (_, e) => eventArgs = e;

                //
                // Close window.
                //
                window.Close();

                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.NotConnected)
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
                    .AwaitStateAsync(RdpClient.ConnectionState.NotConnected)
                    .ConfigureAwait(true);

                Assert.NotNull(eventArgs);
                Assert.IsInstanceOf<RdpDisconnectedException>(eventArgs!.Exception);

                window.Close();
            }
        }
    }
}
