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
using Google.Solutions.Platform.IO;
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
    public class TestSshConnection : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Connect.
        //---------------------------------------------------------------------

        [Test]
        public async Task Connect_WhenConnected_ThenThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                connection.JoinWorkerThreadOnDispose = true;

                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                    () => connection.ConnectAsync().Wait());
            }
        }

        //---------------------------------------------------------------------
        // OpenShell.
        //---------------------------------------------------------------------

        private static async Task<string> ConnectAndEchoLocaleAsync(
            InstanceLocator instance,
            CultureInfo? locale)
        {
            var endpoint = await GetPublicSshEndpointAsync(instance)
                .ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            var output = new StringBuilder();

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                connection.JoinWorkerThreadOnDispose = true;

                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);

                using (var channel = await connection
                    .OpenShellAsync(
                        PseudoTerminalSize.Default,
                        "xterm",
                        locale)
                    .ConfigureAwait(false))
                {
                    channel.OutputAvailable += (_, args) => output.Append(args.Data);

                    await channel
                        .WriteAsync("locale;sleep 1;exit\n", CancellationToken.None)
                        .ConfigureAwait(false);

                    await channel
                        .DrainAsync()
                        .ConfigureAwait(false);
                }
            }

            return output.ToString();
        }

        [Test]
        public async Task OpenShell_WhenServerAcceptsLocale_ThenShellUsesRightLocale(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var output = await ConnectAndEchoLocaleAsync(
                    await instanceLocatorTask,
                    new CultureInfo("en-AU"))
                .ConfigureAwait(false);

            Assert.That(
                output.ToString(), Does.Contain("LC_ALL=en_AU.UTF-8"));
        }

        [Test]
        public async Task OpenShell_WhenServerRejectsLocale_ThenShellUsesDefaultLocale(
           [LinuxInstance(InitializeScript =
                "sed -i '/AcceptEnv/d' /etc/ssh/sshd_config && systemctl restart sshd")]
                ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var output = await ConnectAndEchoLocaleAsync(
                    await instanceLocatorTask,
                    new CultureInfo("en-AU"))
                .ConfigureAwait(false);

            Assert.That(
                output.ToString(), Does.Contain("LC_ALL=\r\n"));
        }

        [Test]
        public async Task OpenShell_WhenLocaleIsNull_ThenShellUsesDefaultLocale(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var output = await ConnectAndEchoLocaleAsync(
                    await instanceLocatorTask,
                    null)
                .ConfigureAwait(false);

            Assert.That(
                output.ToString(), Does.Contain("LC_ALL=\r\n"));
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public async Task Dispose_ThenWorkerIsStopped(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var connection = new SshConnection(
                endpoint,
                credential,
                new KeyboardInteractiveHandler()))
            {
                await connection
                    .ConnectAsync()
                    .ConfigureAwait(false);
            }
        }
    }
}
