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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Platform.IO;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSshShellChannel : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Resize.
        //---------------------------------------------------------------------

        [Test]
        public async Task Resize_UpdatesEnvironment(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
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
                        null)
                    .ConfigureAwait(false))
                {
                    var dimensions = new PseudoTerminalSize(20, 30);
                    await channel
                        .ResizeAsync(dimensions, CancellationToken.None)
                        .ConfigureAwait(false);

                    channel.OutputAvailable += (_, args) => output.Append(args.Data);

                    await channel
                        .WriteAsync("echo $COLUMNS x $LINES;exit\n", CancellationToken.None)
                        .ConfigureAwait(false);

                    await channel
                        .DrainAsync()
                        .ConfigureAwait(false);
                }

                StringAssert.Contains(
                    "20 x 30",
                    output.ToString());
            }
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public async Task Close_DoesNotRaiseDisconnectedEvent(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance)
                .ConfigureAwait(false);
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

                using (var channel = await connection
                    .OpenShellAsync(
                        PseudoTerminalSize.Default,
                        "xterm",
                        null)
                    .ConfigureAwait(false))
                {
                    bool disconnectedRaised = false;
                    channel.Disconnected += (_, args) => disconnectedRaised = true;

                    await channel
                        .CloseAsync()
                        .ConfigureAwait(false);

                    Assert.IsFalse(disconnectedRaised);
                    Assert.IsTrue(channel.IsClosed);
                }
            }
        }

        [Test]
        public async Task Close_WhenInvokedTwice(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance)
                .ConfigureAwait(false);
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

                using (var channel = await connection
                    .OpenShellAsync(
                        PseudoTerminalSize.Default,
                        "xterm",
                        null)
                    .ConfigureAwait(false))
                {
                    await channel
                        .CloseAsync()
                        .ConfigureAwait(false);

                    await channel
                        .CloseAsync()
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
