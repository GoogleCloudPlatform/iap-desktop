//
// Copyright 2022 Google LLC
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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    [UsesCloudResources]
    public class TestRemoteShellChannel : SshFixtureBase
    {
        private class BufferingTerminal : ITextTerminal
        {
            private readonly StringBuilder buffer = new StringBuilder();

            public string TerminalType => "vanilla";

            public CultureInfo? Locale { get; set; } = CultureInfo.InvariantCulture;

            public void OnDataReceived(string data)
            {
                this.buffer.Append(data);
            }

            public void OnError(TerminalErrorType errorType, Exception exception)
            {
                Assert.Fail("Unexpected callback");
            }

            public string Buffer => this.buffer.ToString();

            public async Task AwaitBufferContentAsync(
                TimeSpan timeout,
                string token)
            {
                for (var i = 0; i < 10; i++)
                {
                    await Task
                        .Delay(TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / 10))
                        .ConfigureAwait(false);

                    lock (this.buffer)
                    {
                        if (this.buffer.ToString().Contains(token))
                        {
                            return;
                        }
                    }
                }

                throw new TimeoutException(
                    $"Timeout waiting for buffer to contain '{token}");
            }
        }

        //---------------------------------------------------------------------
        // Send.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSendingEchoCommand_ThenEchoIsReceived(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            var terminal = new BufferingTerminal();

            using (var connection = new RemoteConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler(),
                new SynchronizationContext()))
            {
                await connection.ConnectAsync().ConfigureAwait(false);

                ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                    () => connection.ConnectAsync().Wait());

                var channel = await connection.OpenShellAsync(
                        terminal,
                        RemoteShellChannel.DefaultTerminalSize)
                    .ConfigureAwait(false);

                await channel.SendAsync("whoami\n").ConfigureAwait(false);
                await channel.SendAsync("exit\n").ConfigureAwait(false);

                await terminal.AwaitBufferContentAsync(
                        TimeSpan.FromSeconds(10),
                        "testuser")
                    .ConfigureAwait(false);

                StringAssert.Contains(
                    "testuser",
                    terminal.Buffer.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenChannelClosedExplicitly_ThenDoubleCloseIsPrevented(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            var terminal = new BufferingTerminal()
            {
                Locale = new CultureInfo("en-AU")
            };

            using (var connection = new RemoteConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler(),
                new SynchronizationContext()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                using (var channel = await connection.OpenShellAsync(
                        terminal,
                        RemoteShellChannel.DefaultTerminalSize)
                    .ConfigureAwait(false))
                {
                }
            }
        }

        //---------------------------------------------------------------------
        // Environment.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenServerAcceptsLocale_ThenShellUsesRightLocale(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            var terminal = new BufferingTerminal()
            {
                Locale = new CultureInfo("en-AU")
            };

            using (var connection = new RemoteConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler(),
                new SynchronizationContext()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                    () => connection.ConnectAsync().Wait());

                var channel = await connection.OpenShellAsync(
                        terminal,
                        RemoteShellChannel.DefaultTerminalSize)
                    .ConfigureAwait(false);

                await channel
                    .SendAsync("locale;sleep 1;exit\n")
                    .ConfigureAwait(false);

                await terminal.AwaitBufferContentAsync(
                        TimeSpan.FromSeconds(10),
                        "testuser")
                    .ConfigureAwait(false);

                StringAssert.Contains(
                    "LC_ALL=en_AU.UTF-8",
                    terminal.Buffer.ToString());
            }
        }

        [Test]
        public async Task WhenServerRejectsLocale_ThenShellUsesDefaultLocale(
            [LinuxInstance(InitializeScript =
                "sed -i '/AcceptEnv/d' /etc/ssh/sshd_config && systemctl restart sshd")]
                ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            var terminal = new BufferingTerminal()
            {
                Locale = new CultureInfo("en-AU")
            };

            using (var connection = new RemoteConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler(),
                new SynchronizationContext()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                    () => connection.ConnectAsync().Wait());


                var channel = await connection.OpenShellAsync(
                        terminal,
                        RemoteShellChannel.DefaultTerminalSize)
                    .ConfigureAwait(false);

                await channel
                    .SendAsync("locale;sleep 1;exit\n")
                    .ConfigureAwait(false);

                await terminal.AwaitBufferContentAsync(
                        TimeSpan.FromSeconds(10),
                        "testuser")
                    .ConfigureAwait(false);

                StringAssert.Contains(
                    "LC_ALL=\r\n",
                    terminal.Buffer.ToString());
            }
        }

        [Test]
        public async Task WhenLocaleIsNull_ThenShellUsesDefaultLocale(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            var terminal = new BufferingTerminal()
            {
                Locale = null
            };

            using (var connection = new RemoteConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler(),
                new SynchronizationContext()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                    () => connection.ConnectAsync().Wait());

                var channel = await connection.OpenShellAsync(
                        terminal,
                        RemoteShellChannel.DefaultTerminalSize)
                    .ConfigureAwait(false);

                await channel
                    .SendAsync("locale;sleep 1;exit\n")
                    .ConfigureAwait(false);

                await terminal.AwaitBufferContentAsync(
                        TimeSpan.FromSeconds(10),
                        "testuser")
                    .ConfigureAwait(false);

                StringAssert.Contains(
                    "LC_ALL=\r\n",
                    terminal.Buffer.ToString());
            }
        }
    }
}
