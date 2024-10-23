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

using Google.Solutions.Common.Security;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Ssh;
using Google.Solutions.Terminal.Controls;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Microsoft.VisualBasic;
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
    public class TestSshShellClient 
    {
        private const string InvalidServer = "8.8.8.8";

        private static string? serverAddress;
        private static string? username;
        private static string? password;

        [OneTimeSetUp]
        public static void CollectCredentials()
        {
            serverAddress = Interaction.InputBox("SSH server IP address");
            username = Interaction.InputBox("SSH username");
            password = Interaction.InputBox("SSH password");
        }

        [TearDown]
        public void TearDown()
        {
            //
            // Wait for worker threads to finish so that test caseses
            // don't interfere with another.
            //
            SshWorkerThread.JoinAllWorkerThreadsAsync().Wait();
        }

        private class KeyboardInteractiveHandler : IKeyboardInteractiveHandler
        {
            public string? Prompt(string caption, string instruction, string prompt, bool echo)
            {
                return Interaction.InputBox(
                    $"Caption: {caption}\nPrompt: {prompt}",
                    caption);
            }

            public IPasswordCredential PromptForCredentials(string username)
            {
                var password = Interaction.InputBox($"Enter password for {username}");
                return new StaticPasswordCredential(username, password);
            }
        }

        private static ClientDiagnosticsWindow<SshShellClient> CreateWindow()
        {
            var window = new ClientDiagnosticsWindow<SshShellClient>(new SshShellClient());
            window.Client.ServerEndpoint = new IPEndPoint(
                IPAddress.Parse(serverAddress), 
                22);
            window.Client.Credential = new StaticPasswordCredential(
                username ?? "test", 
                SecureStringExtensions.FromClearText(password));
            window.Client.KeyboardInteractiveHandler = new KeyboardInteractiveHandler();
            return window;
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
        public async Task Connect_WhenServerInvalid_ThenRaisesEvent()
        {
            using (var window = CreateWindow())
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
                    .AwaitStateAsync(ClientBase.ConnectionState.NotConnected)
                    .ConfigureAwait(true);

                Assert.NotNull(eventArgs);
                Assert.IsInstanceOf<SocketException>(eventArgs!.Exception);

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
                    .AwaitStateAsync(ClientBase.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                ClientBase.ConnectionClosedEventArgs? eventArgs = null;
                window.Client.ConnectionClosed += (_, e) => eventArgs = e;

                //
                // Close window.
                //
                window.Close();

                await window.Client
                    .AwaitStateAsync(ClientBase.ConnectionState.NotConnected)
                    .ConfigureAwait(true);

                Assert.NotNull(eventArgs);
                Assert.AreEqual(ClientBase.DisconnectReason.FormClosed, eventArgs!.Reason);
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
                    .AwaitStateAsync(ClientBase.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                window.Client.SendText("exit\r\n");

                await window.Client
                    .AwaitStateAsync(ClientBase.ConnectionState.NotConnected)
                    .ConfigureAwait(true);

                window.Close();
            }
        }
    }
}
