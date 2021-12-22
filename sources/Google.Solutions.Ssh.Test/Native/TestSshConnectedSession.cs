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
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    public class TestSshConnectedSession : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenGetRemoteBannerReturnsBanner(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                var banner = connection.GetRemoteBanner();
                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);
                Assert.IsNotNull(banner);
            }
        }


        //---------------------------------------------------------------------
        // Algorithms.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenActiveAlgorithmsAreSet(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.KEX));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.HOSTKEY));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.CRYPT_CS));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.CRYPT_SC));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.MAC_CS));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.MAC_SC));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.COMP_CS));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.COMP_SC));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.LANG_CS));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.LANG_SC));
            }
        }

        [Test]
        public async Task WhenRequestedAlgorithmInvalid_ThenActiveAlgorithmsThrowsArgumentException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                Assert.Throws<ArgumentException>(
                    () => connection.GetActiveAlgorithms((LIBSSH2_METHOD)9999999));
            }
        }

        //---------------------------------------------------------------------
        // Host Key.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenGetRemoteHostKeyReturnsKey(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                var key = connection.GetRemoteHostKey();
                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);
                Assert.IsNotNull(key);
            }
        }

        [Test]
        public async Task WhenConnected_ThenGetRemoteHostKeyTypeReturnsEcdsa256(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                var keyType = connection.GetRemoteHostKeyType();
                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);
                Assert.IsTrue(
                    keyType == LIBSSH2_HOSTKEY_TYPE.ECDSA_256 ||
                    keyType == LIBSSH2_HOSTKEY_TYPE.RSA);
            }
        }

        [Test]
        public async Task WhenConnected_ThenGetRemoteHostKeyHashReturnsKeyHash(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                Assert.IsNotNull(connection.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.MD5), "MD5");
                Assert.IsNotNull(connection.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.SHA1), "SHA1");

                // SHA256 is not always available.
                // Assert.IsNotNull(connection.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.SHA256), "SHA256");
            }
        }

        [Test]
        public async Task WhenRequestedAlgorithmInvalid_ThennGetRemoteHostKeyHashThrowsArgumentException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                Assert.Throws<ArgumentException>(
                    () => connection.GetRemoteHostKeyHash((LIBSSH2_HOSTKEY_HASH)9999999));
            }
        }

        [Test]
        public async Task WhenNeitherEcdsaNorRsaHostKeyAlgorithmAllowed_ThenConnectThrowsException(
            [LinuxInstance(InitializeScript = InitializeScripts.AllowNeitherEcdsaNorRsaForHostKey)] 
            ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            {
                //
                // NB. Connect should throw an exception, but libssh doesn't
                // set the last error. So we have to check the exception
                // message.
                //

                try
                {
                    session.Connect(endpoint);
                    Assert.Fail("Expected exception");
                }
                catch (SshNativeException e)
                {
                    Assert.AreEqual("Unable to exchange encryption keys", e.Message);
                }
            }
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCustomBannerHasWrongPrefix_ThenSetLocalBannerThrowsArgumentException()
        {
            using (var session = CreateSession())
            {
                Assert.Throws<ArgumentException>(
                    () => session.SetLocalBanner("SSH-test-123"));
            }
        }

        [Test]
        public async Task WhenCustomBannerSet_ThenConnectionSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            {
                session.SetLocalBanner("SSH-2.0-test-123");
                using (var connection = session.Connect(endpoint))
                {
                    Assert.IsFalse(connection.IsAuthenticated);
                }
            }

        }

        [Test]
        public async Task WhenConnected_GetRemoteBannerReturnsBanner(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            {
                using (var connection = session.Connect(endpoint))
                {
                    StringAssert.StartsWith("SSH", connection.GetRemoteBanner());
                }
            }
        }

        //---------------------------------------------------------------------
        // User auth.
        //---------------------------------------------------------------------

        private string UnexpectedAuthenticationCallback(
            string name,
            string instruction,
            string prompt,
            bool echo)
        {
            Assert.Fail("Unexpected callback");
            return null;
        }

        [Test]
        public async Task WhenConnected_ThenIsAuthenticatedIsFalse(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                Assert.IsFalse(connection.IsAuthenticated);
            }
        }

        [Test]
        public async Task WhenConnected_ThenGetAuthenticationMethodsReturnsPublicKey(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                var methods = connection.GetAuthenticationMethods(string.Empty);
                Assert.IsNotNull(methods);
                Assert.AreEqual(1, methods.Length);
                Assert.AreEqual("publickey", methods.First());
            }
        }

        [Test]
        public async Task WhenPublicKeyValidButUnrecognized_ThenAuthenticateThrowsAuthenticationFailed(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072))
            {
                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                    () => connection.Authenticate(
                        "invaliduser",
                        key,
                        this.UnexpectedAuthenticationCallback));
            }
        }

        [Test]
        public async Task WhenSessionDisconnected_ThenAuthenticateThrowsSocketSend(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                {
                    connection.Dispose();

                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.SOCKET_SEND,
                        () => connection.Authenticate(
                            "testuser",
                            key,
                            this.UnexpectedAuthenticationCallback));
                }
            }
        }

        [Test]
        public async Task WhenRsaPublicKeyValidAndKnownFromMetadata_ThenAuthenticationSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                {
                    var authSession = connection.Authenticate(
                        "testuser",
                        key,
                        this.UnexpectedAuthenticationCallback);
                    Assert.IsNotNull(authSession);
                }
            }
        }

        [Test]
        public async Task WhenEcdsaPublicKeyValidAndKnownFromMetadata_ThenAuthenticationSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Values(
                SshKeyType.EcdsaNistp256, 
                SshKeyType.EcdsaNistp384, 
                SshKeyType.EcdsaNistp521)] SshKeyType keyType)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var key = SshKey.NewEphemeralKey(keyType))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                {
                    var authSession = connection.Authenticate(
                        "testuser",
                        key,
                        this.UnexpectedAuthenticationCallback);
                    Assert.IsNotNull(authSession);
                }
            }
        }

        //---------------------------------------------------------------------
        // 2FA.
        //---------------------------------------------------------------------

        //
        // Service acconts can't use 2FA, so emulate the 2FA prompting behavior
        // by setting up SSHD to require a public key *and* a keyboard-interactive.
        //
        private const string RequireSshPassword =
            "cat << EOF > /etc/ssh/sshd_config\n" +
            "UsePam yes\n"+
            "AuthenticationMethods publickey,keyboard-interactive\n" + 
            "EOF\n" +
            "systemctl restart sshd";

        [Test]
        public async Task When2faRequiredAndPromptReturnsWrongValue_ThenPromptIsRetried(
            [LinuxInstance(InitializeScript = RequireSshPassword)] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                {
                    var callbackCount = 0;
                    
                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                        () => connection.Authenticate(
                            "testuser",
                            key,
                            (name, instruction, prompt, echo) =>
                            {
                                callbackCount++;

                                Assert.AreEqual("Password: ", prompt);
                                Assert.IsFalse(echo);

                                return "wrong";
                            }));
                    Assert.AreEqual(SshConnectedSession.KeyboardInteractiveRetries, callbackCount);
                }
            }
        }

        [Test]
        public async Task When2faRequiredAndPromptReturnsNull_ThenPromptIsRetried(
            [LinuxInstance(InitializeScript = RequireSshPassword)] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                {
                    var callbackCount = 0;

                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                        () => connection.Authenticate(
                            "testuser",
                            key,
                            (name, instruction, prompt, echo) =>
                            {
                                callbackCount++;

                                Assert.AreEqual("Password: ", prompt);
                                Assert.IsFalse(echo);

                                return null;
                            }));
                    Assert.AreEqual(SshConnectedSession.KeyboardInteractiveRetries, callbackCount);
                }
            }
        }

        [Test]
        public async Task When2faRequiredAndPromptThrowsException_ThenAuthenticationFailsWithoutRetry(
            [LinuxInstance(InitializeScript = RequireSshPassword)] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil
                    .PublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                22);
            using (var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072))
            {
                await InstanceUtil
                    .AddPublicKeyToMetadata(
                        await instanceLocatorTask,
                        "testuser",
                        key)
                    .ConfigureAwait(false);

                using (var session = CreateSession())
                using (var connection = session.Connect(endpoint))
                {
                    var callbackCount = 0;

                    Assert.Throws<OperationCanceledException>(
                        () => connection.Authenticate(
                            "testuser",
                            key,
                            (name, instruction, prompt, echo) =>
                            {
                                callbackCount++;
                                throw new OperationCanceledException();
                            }));
                    Assert.AreEqual(1, callbackCount);
                }
            }
        }
    }
}
