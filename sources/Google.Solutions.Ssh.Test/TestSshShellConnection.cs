//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    public class TestSshShellConnection : SshFixtureBase
    {
        private async Task AwaitBufferContentAsync(
            StringBuilder buffer,
            TimeSpan timeout,
            string token)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task
                    .Delay(TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / 10))
                    .ConfigureAwait(false);

                lock (buffer)
                {
                    if (buffer.ToString().Contains(token))
                    {
                        return;
                    }
                }
            }

            throw new TimeoutException(
                $"Timeout waiting for buffer to contain '{token}");
        }

        private string UnexpectedAuthenticationCallback(
            string name,
            string instruction,
            string prompt,
            bool echo)
        {
            Assert.Fail("Unexpected callback");
            return null;
        }

        private void UnexpectedErrorCallback(Exception exception)
        {
            Assert.Fail("Unexpected callback");
        }

        [Test]
        public async Task WhenSendingEchoCommand_ThenEchoIsReceived(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);

            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                var receiveBuffer = new StringBuilder();

                void receiveHandler(string data)
                {
                    lock (receiveBuffer)
                    {
                        receiveBuffer.Append(data);
                    }
                }

                using (var connection = new SshShellConnection(
                    "testuser",
                    endpoint,
                    key,
                    SshShellConnection.DefaultTerminal,
                    SshShellConnection.DefaultTerminalSize,
                    CultureInfo.InvariantCulture,
                    UnexpectedAuthenticationCallback,
                    receiveHandler,
                    UnexpectedErrorCallback))
                {
                    await connection.ConnectAsync().ConfigureAwait(false);

                    AssertEx.ThrowsAggregateException<InvalidOperationException>(
                        () => connection.ConnectAsync().Wait());

                    await connection.SendAsync("whoami\n").ConfigureAwait(false);
                    await connection.SendAsync("exit\n").ConfigureAwait(false);

                    await AwaitBufferContentAsync(
                            receiveBuffer,
                            TimeSpan.FromSeconds(10),
                            "testuser")
                        .ConfigureAwait(false);

                    StringAssert.Contains(
                        "testuser",
                        receiveBuffer.ToString());
                }
            }
        }

        [Test]
        public async Task WhenDisposingConnection_ThenWorkerIsStopped(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);

            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var connection = new SshShellConnection(
                    "testuser",
                    endpoint,
                    key,
                    SshShellConnection.DefaultTerminal,
                    SshShellConnection.DefaultTerminalSize,
                    CultureInfo.InvariantCulture,
                    UnexpectedAuthenticationCallback,
                    _ => { },
                    UnexpectedErrorCallback))
                {
                    await connection
                        .ConnectAsync()
                        .ConfigureAwait(false);
                }
            }
        }

        [Test]
        public async Task WhenServerAcceptsLocale_ThenShellUsesRightLocale(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);

            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                var receiveBuffer = new StringBuilder();

                void receiveHandler(string data)
                {
                    lock (receiveBuffer)
                    {
                        receiveBuffer.Append(data);
                    }
                }

                using (var connection = new SshShellConnection(
                    "testuser",
                    endpoint,
                    key,
                    SshShellConnection.DefaultTerminal,
                    SshShellConnection.DefaultTerminalSize,
                    new CultureInfo("en-AU"),
                    UnexpectedAuthenticationCallback,
                    receiveHandler,
                    UnexpectedErrorCallback))
                {
                    await connection
                        .ConnectAsync()
                        .ConfigureAwait(false);

                    AssertEx.ThrowsAggregateException<InvalidOperationException>(
                        () => connection.ConnectAsync().Wait());

                    await connection
                        .SendAsync("locale;sleep 1;exit\n")
                        .ConfigureAwait(false);

                    await AwaitBufferContentAsync(
                            receiveBuffer,
                            TimeSpan.FromSeconds(10),
                            "testuser")
                        .ConfigureAwait(false);

                    StringAssert.Contains(
                        "LC_ALL=en_AU.UTF-8",
                        receiveBuffer.ToString());
                }
            }
        }

        [Test]
        public async Task WhenServerRejectsLocale_ThenShellUsesDefaultLocale(
            [LinuxInstance(InitializeScript =
                "sed -i '/AcceptEnv/d' /etc/ssh/sshd_config && systemctl restart sshd")]
                ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);

            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                var receiveBuffer = new StringBuilder();

                void receiveHandler(string data)
                {
                    lock (receiveBuffer)
                    {
                        receiveBuffer.Append(data);
                    }
                }

                using (var connection = new SshShellConnection(
                    "testuser",
                    endpoint,
                    key,
                    SshShellConnection.DefaultTerminal,
                    SshShellConnection.DefaultTerminalSize,
                    new CultureInfo("en-AU"),
                    UnexpectedAuthenticationCallback,
                    receiveHandler,
                    UnexpectedErrorCallback))
                {
                    await connection
                        .ConnectAsync()
                        .ConfigureAwait(false);

                    AssertEx.ThrowsAggregateException<InvalidOperationException>(
                        () => connection.ConnectAsync().Wait());

                    await connection
                        .SendAsync("locale;sleep 1;exit\n")
                        .ConfigureAwait(false);

                    await AwaitBufferContentAsync(
                            receiveBuffer,
                            TimeSpan.FromSeconds(10),
                            "testuser")
                        .ConfigureAwait(false);

                    StringAssert.Contains(
                        "LC_ALL=\r\n",
                        receiveBuffer.ToString());
                }
            }
        }

        [Test]
        public async Task WhenLocaleIsNull_ThenShellUsesDefaultLocale(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);

            using (var key = new RsaSshKey(new RSACng()))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                var receiveBuffer = new StringBuilder();

                void receiveHandler(string data)
                {
                    lock (receiveBuffer)
                    {
                        receiveBuffer.Append(data);
                    }
                }

                using (var connection = new SshShellConnection(
                    "testuser",
                    endpoint,
                    key,
                    SshShellConnection.DefaultTerminal,
                    SshShellConnection.DefaultTerminalSize,
                    null,
                    UnexpectedAuthenticationCallback,
                    receiveHandler,
                    UnexpectedErrorCallback))
                {
                    await connection
                        .ConnectAsync()
                        .ConfigureAwait(false);

                    AssertEx.ThrowsAggregateException<InvalidOperationException>(
                        () => connection.ConnectAsync().Wait());

                    await connection
                        .SendAsync("locale;sleep 1;exit\n")
                        .ConfigureAwait(false);

                    await AwaitBufferContentAsync(
                            receiveBuffer,
                            TimeSpan.FromSeconds(10),
                            "testuser")
                        .ConfigureAwait(false);

                    StringAssert.Contains(
                        "LC_ALL=\r\n",
                        receiveBuffer.ToString());
                }
            }
        }
    }
}
