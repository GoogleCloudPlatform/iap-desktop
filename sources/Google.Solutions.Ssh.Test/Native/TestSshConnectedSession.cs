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
using Google.Solutions.Common.Security;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Ssh.Native;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSshConnectedSession : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenGetRemoteBannerReturnsBanner(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                var banner = connection.GetRemoteBanner();
                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);
                Assert.IsNotNull(banner);
            }
        }


        //---------------------------------------------------------------------
        // GetActiveAlgorithms.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenGetActiveAlgorithmsSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

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
        public async Task WhenRequestedAlgorithmInvalid_ThenGetActiveAlgorithmsThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                Assert.Throws<ArgumentException>(
                    () => connection.GetActiveAlgorithms((LIBSSH2_METHOD)9999999));
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
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

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
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            {
                using (var connection = session.Connect(endpoint))
                {
                    StringAssert.StartsWith("SSH", connection.GetRemoteBanner());
                }
            }
        }

        //---------------------------------------------------------------------
        // Connect - host key algorithms.
        //---------------------------------------------------------------------

        private void ConnectWithHostKey(
            IPEndPoint endpoint,
            LIBSSH2_HOSTKEY_TYPE hostKeyType)
        {
            using (var session = CreateSession())
            {
                session.SetPreferredMethods(
                    LIBSSH2_METHOD.HOSTKEY,
                    new[] { new HostKeyType(hostKeyType).Name });

                using (var connection = session.Connect(endpoint))
                {
                    Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);
                    Assert.AreEqual(hostKeyType, connection.GetRemoteHostKeyType());

                    Assert.IsNotNull(connection.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.SHA256), "SHA256");
                    Assert.IsNotNull(connection.GetRemoteHostKey());

                    CollectionAssert.AreEqual(
                        new HostKeyType(hostKeyType).Name,
                        connection.GetActiveAlgorithms(LIBSSH2_METHOD.HOSTKEY)[0]);
                }
            }
        }

        [Test]
        public async Task ConnectWithRsaHostKey(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            ConnectWithHostKey(
                endpoint,
                LIBSSH2_HOSTKEY_TYPE.RSA);
        }

        [Test]
        public async Task ConnectWithEcdsaNistp256HostKey(
            [LinuxInstance(InitializeScript = InitializeScripts.EcdsaNistp256HostKey)] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            ConnectWithHostKey(
                endpoint,
                LIBSSH2_HOSTKEY_TYPE.ECDSA_256);
        }

        [Test]
        public async Task ConnectWithEcdsaNistp384HostKey(
            [LinuxInstance(InitializeScript = InitializeScripts.EcdsaNistp384HostKey)] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            ConnectWithHostKey(
                endpoint,
                LIBSSH2_HOSTKEY_TYPE.ECDSA_384);
        }

        [Test]
        public async Task ConnectWithEcdsaNistp521HostKey(
            [LinuxInstance(InitializeScript = InitializeScripts.EcdsaNistp521HostKey)] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            ConnectWithHostKey(
                endpoint,
                LIBSSH2_HOSTKEY_TYPE.ECDSA_521);
        }

        //---------------------------------------------------------------------
        // GetRemoteHostKeyHash.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenRequestedAlgorithmInvalid_ThennGetRemoteHostKeyHashThrowsArgumentException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

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
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

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
        // GetAuthenticationMethods.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenGetAuthenticationMethodsReturnsPublicKey(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                var methods = connection.GetAuthenticationMethods(string.Empty);
                Assert.IsNotNull(methods);
                Assert.AreEqual(1, methods.Length);
                Assert.AreEqual("publickey", methods.First());
            }
        }

        //---------------------------------------------------------------------
        // Authentication.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenIsAuthenticatedIsFalse(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                Assert.IsFalse(connection.IsAuthenticated);
            }
        }

        [Test]
        public async Task WhenSessionDisconnected_ThenAuthenticateThrowsSocketSend(
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
            {
                connection.Dispose();

                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.SOCKET_SEND,
                    () => connection.Authenticate(
                        credential,
                        new KeyboardInteractiveHandler()));
            }
        }

        //---------------------------------------------------------------------
        // Authentication: password.
        //---------------------------------------------------------------------

        private const string SshdWithPublicKeyOrPasswordAuth =
            "cat << EOF > /etc/ssh/sshd_config\n" +
            "UsePam yes\n" +
            "AuthenticationMethods publickey password\n" +
            "EOF\n" +
            "systemctl restart sshd";

        [Test]
        public async Task WhenPasswordInvalid_ThenAuthenticateUsingPasswordThrowsException(
            [LinuxInstance(InitializeScript = SshdWithPublicKeyOrPasswordAuth)]
            ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                    () => connection.Authenticate(
                        new StaticPasswordCredential("invaliduser", "invalidpassword"),
                        new KeyboardInteractiveHandler()));
            }
        }

        [Test]
        public async Task WhenPasswordValid_ThenAuthenticateUsingPasswordSucceeds(
            [LinuxInstance(InitializeScript = SshdWithPublicKeyOrPasswordAuth)]
            ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            var credential = await CreatePasswordCredentialAsync(
                    instance,
                    endpoint)
                .ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                credential,
                new KeyboardInteractiveHandler()))
            {
                Assert.IsNotNull(authSession);
            }
        }

        [Test]
        public async Task WhenPasswordEmpty_ThenAuthenticateInvokesPrompt(
            [LinuxInstance(InitializeScript = SshdWithPublicKeyOrPasswordAuth)]
            ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            var credential = await CreatePasswordCredentialAsync(
                    instance,
                    endpoint)
                .ConfigureAwait(false);
            var incompleteCredentials = new StaticPasswordCredential(
                credential.Username,
                string.Empty);

            var handler = new KeyboardInteractiveHandler()
            {
                PromptForCredentialsCallback = username =>
                {
                    Assert.AreEqual(incompleteCredentials.Username, username);
                    return credential;
                }
            };

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                incompleteCredentials,
                handler))
            {
                Assert.AreEqual(1, handler.PromptCount);
                Assert.IsNotNull(authSession);
            }
        }

        [Test]
        public async Task WhenPasswordEmptyAndPromptCanceled_ThenAuthenticateThrowsException(
            [LinuxInstance(InitializeScript = SshdWithPublicKeyOrPasswordAuth)]
            ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            var credential = await CreatePasswordCredentialAsync(
                    instance,
                    endpoint)
                .ConfigureAwait(false);
            var incompleteCredentials = new StaticPasswordCredential(
                string.Empty,
                string.Empty);

            var handler = new KeyboardInteractiveHandler()
            {
                PromptForCredentialsCallback = existing =>
                {
                    throw new OperationCanceledException("mock");
                }
            };

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                Assert.Throws<OperationCanceledException>(
                    () => connection.Authenticate(
                        incompleteCredentials,
                        handler));
            }
        }

        //---------------------------------------------------------------------
        // Authentication: keyboard-interactive.
        //---------------------------------------------------------------------

        private const string SshdWithPublicKeyOrKeyboardInteractiveAuth =
            "cat << EOF > /etc/ssh/sshd_config\n" +
            "UsePam yes\n" +
            "AuthenticationMethods publickey keyboard-interactive\n" +
            "EOF\n" +
            "systemctl restart sshd";

        [Test]
        public async Task WhenPasswordInvalid_ThenAuthenticateUsingKeyboardInteractiveThrowsException(
            [LinuxInstance(InitializeScript = SshdWithPublicKeyOrKeyboardInteractiveAuth)]
            ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                    () => connection.Authenticate(
                        new StaticPasswordCredential("ignored", "ignored"),
                        new KeyboardInteractiveHandler()
                        {
                            PromptCallback = (name, instr, prompt, echo) => "wrong-password"
                        }));
            }
        }

        [Test]
        public async Task WhenPasswordValid_ThenAuthenticateUsingKeyboardInteractiveSucceeds(
            [LinuxInstance(InitializeScript = SshdWithPublicKeyOrKeyboardInteractiveAuth)]
            ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            var credential = await CreatePasswordCredentialAsync(
                    instance,
                    endpoint)
                .ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var authSession = connection.Authenticate(
                new StaticPasswordCredential(credential.Username, "ignored"),
                new KeyboardInteractiveHandler()
                {
                    PromptCallback = (name, instr, prompt, echo) =>
                    {
                        Assert.AreEqual("Interactive authentication", name);
                        Assert.AreEqual("Password: ", prompt);
                        Assert.IsFalse(echo);

                        return credential.Password.AsClearText();
                    }
                }))
            {
                Assert.IsNotNull(authSession);
            }
        }

        //---------------------------------------------------------------------
        // Authentication: public key.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPublicKeyValidButUnrecognized_ThenAuthenticateThrowsException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            using (var signer = AsymmetricKeySigner.CreateEphemeral(keyType))
            {
                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                    () => connection.Authenticate(
                        new StaticAsymmetricKeyCredential("invaliduser", signer),
                        new KeyboardInteractiveHandler()));
            }
        }

        [Test]
        public async Task WhenPublicKeyValidAndKnownFromMetadata_ThenAuthenticateSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Values(
                SshKeyType.Rsa3072,
                SshKeyType.EcdsaNistp256,
                SshKeyType.EcdsaNistp384,
                SshKeyType.EcdsaNistp521)] SshKeyType keyType)
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
                new KeyboardInteractiveHandler()))
            {
                Assert.IsNotNull(authSession);
            }
        }

        private const string SshdWithPublicKeyAndPasswordAuth =
            "cat << EOF > /etc/ssh/sshd_config\n" +
            "UsePam yes\n" +
            "AuthenticationMethods publickey,password\n" +
            "EOF\n" +
            "systemctl restart sshd";

        [Test]
        public async Task WhenPublicKeyAndPasswordRequired_ThenAuthenticateThrowsException(
            [LinuxInstance(InitializeScript = SshdWithPublicKeyAndPasswordAuth)] ResourceTask<InstanceLocator> instanceLocatorTask,
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
            {
                Assert.Throws<UnsupportedAuthenticationMethodException>(
                    () => connection.Authenticate(
                        credential,
                        new KeyboardInteractiveHandler()));
            }
        }

        //---------------------------------------------------------------------
        // Public key authentication + 2FA.
        //---------------------------------------------------------------------

        //
        // Service acconts can't use 2FA, so emulate the 2FA prompting behavior
        // by setting up SSHD to require a public key *and* a keyboard-interactive.
        //
        private const string SshdWithPublicKeyAndKeyboardInteractiveAuth =
            "cat << EOF > /etc/ssh/sshd_config\n" +
            "UsePam yes\n" +
            "AuthenticationMethods publickey,keyboard-interactive\n" +
            "EOF\n" +
            "systemctl restart sshd";

        [Test]
        public async Task When2faRequiredAndPromptReturnsWrongValue_ThenPromptIsRetried(
            [LinuxInstance(InitializeScript = SshdWithPublicKeyAndKeyboardInteractiveAuth)] ResourceTask<InstanceLocator> instanceLocatorTask,
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
            {
                var twoFaHandler = new KeyboardInteractiveHandler()
                {
                    PromptCallback = (name, instruction, prompt, echo) =>
                    {
                        Assert.AreEqual("2-step verification", name);
                        Assert.AreEqual("Password: ", prompt);
                        Assert.IsFalse(echo);

                        return "wrong";
                    }
                };

                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                    () => connection.Authenticate(credential, twoFaHandler));

                Assert.AreEqual(
                    SshConnectedSession.KeyboardInteractiveRetries,
                    twoFaHandler.PromptCount);
            }
        }

        [Test]
        public async Task When2faRequiredAndPromptReturnsNull_ThenPromptIsRetried(
            [LinuxInstance(InitializeScript = SshdWithPublicKeyAndKeyboardInteractiveAuth)] ResourceTask<InstanceLocator> instanceLocatorTask,
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
            {
                var twoFactorHandler = new KeyboardInteractiveHandler()
                {
                    PromptCallback = (name, instruction, prompt, echo) =>
                    {
                        Assert.AreEqual("Password: ", prompt);
                        Assert.IsFalse(echo);

                        return null;
                    }
                };

                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                    () => connection.Authenticate(credential, twoFactorHandler));

                Assert.AreEqual(SshConnectedSession.KeyboardInteractiveRetries, twoFactorHandler.PromptCount);
            }
        }

        [Test]
        public async Task When2faRequiredAndPromptThrowsException_ThenAuthenticateFailsWithoutRetry(
            [LinuxInstance(InitializeScript = SshdWithPublicKeyAndKeyboardInteractiveAuth)] ResourceTask<InstanceLocator> instanceLocatorTask,
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
            {
                var twoFactorHandler = new KeyboardInteractiveHandler()
                {
                    PromptCallback = (name, instruction, prompt, echo) =>
                    {
                        throw new OperationCanceledException();
                    }
                };

                Assert.Throws<OperationCanceledException>(
                    () => connection.Authenticate(credential, twoFactorHandler));
                Assert.AreEqual(1, twoFactorHandler.PromptCount);
            }
        }
    }
}
