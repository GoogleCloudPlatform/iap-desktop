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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Ssh.Native;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSshAuthenticatedSession : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Channel.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenOpenShellChannelSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    keyType)
                .ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                credential,
                KeyboardInteractiveHandler.Silent))
            using (var channel = authSession.OpenShellChannel(
                LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL,
                "vanilla",
                80,
                24,
                null))
            {
                channel.Close();
            }
        }

        [Test]
        public async Task WhenClosingSessionBeforeChannel_ThenDoubleFreeIsPrevented(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            var session = CreateSession();
            var connection = session.Connect(endpoint);
            var authSession = connection.Authenticate(
                credential,
                KeyboardInteractiveHandler.Silent);
            var channel = authSession.OpenShellChannel(
                LIBSSH2_CHANNEL_EXTENDED_DATA.NORMAL,
                "vanilla",
                80,
                24,
                null);

            session.Dispose();

            //
            // Free channel after session - note that this causes an assertion
            // when debugging.
            //
            channel.Dispose();
        }

        [Test]
        public async Task WhenUsingRsaKeyButOnlyEcdsaAllowed_ThenAuthenticateThrowsException(
            [LinuxInstance(InitializeScript = InitializeScripts.AllowEcdsaOnlyForPubkey)]
            ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);
            var credential = await CreateAsymmetricKeyCredentialAsync(
                    instance,
                    SshKeyType.Rsa3072)
                .ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.PUBLICKEY_UNRECOGNIZED,
                    () => connection.Authenticate(
                        credential,
                        KeyboardInteractiveHandler.Silent));
            }
        }
    }
}
