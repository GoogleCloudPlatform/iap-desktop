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
using Google.Solutions.Ssh.Native;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    [UsesCloudResources]
    public class TestLibssh2Session : SshFixtureBase
    {
        private readonly IPEndPoint NonSshEndpoint =
            new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);

        //---------------------------------------------------------------------
        // Handle.
        //---------------------------------------------------------------------

        [Test]
        public void Handle_WhenNotInitialized_ThenHandleThrowsException()
        {
            using (var session = CreateSession())
            {
                Assert.Throws<InvalidOperationException>(() => _ = session.Handle);
            }
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        [Test]
        public void Banner_WhenBannerInvalid_ThenSetBannerThrowsException(
            [Values("-1", "a b")] string banner)
        {
            using (var session = CreateSession())
            {
                Assert.Throws<ArgumentException>(() => session.Banner = banner);
            }
        }

        //---------------------------------------------------------------------
        // Timeout.
        //---------------------------------------------------------------------

        [Test]
        public void Timeout()
        {
            using (var session = CreateSession())
            {
                session.Timeout = TimeSpan.FromSeconds(123);

                Assert.AreEqual(TimeSpan.FromSeconds(123), session.Timeout);
            }
        }

        //---------------------------------------------------------------------
        // Blocking.
        //---------------------------------------------------------------------

        [Test]
        public void Blocking_EnabledByDefault()
        {
            using (var session = CreateSession())
            {
                Assert.IsTrue(session.IsBlocking);
            }
        }

        //---------------------------------------------------------------------
        // Error.
        //---------------------------------------------------------------------

        [Test]
        public void LastError_WhenLastErrorStillApplies_ThenExceptionContainsErrorMessage()
        {
            using (var session = CreateSession())
            {
                try
                {
                    //
                    // Trigger an error.
                    //
                    session.GetSupportedAlgorithms((LIBSSH2_METHOD)(-1));

                    Assert.Fail();
                }
                catch (Libssh2Exception e)
                {
                    Assert.AreEqual(
                        "Unknown method type",
                        e.Message);
                    Assert.AreEqual(LIBSSH2_ERROR.METHOD_NOT_SUPPORTED, e.ErrorCode);
                }
            }
        }

        [Test]
        public void LastError_WhenLastErrorDoesNotApplyAnymore_ThenExceptionContainsGenericErrorMessage()
        {
            using (var session = CreateSession())
            {
                var exception = session.CreateException(LIBSSH2_ERROR.PASSWORD_EXPIRED);
                Assert.AreEqual(
                    "SSH operation failed: PASSWORD_EXPIRED",
                    exception.Message);
                Assert.AreEqual(LIBSSH2_ERROR.PASSWORD_EXPIRED, exception.ErrorCode);
            }
        }

        //---------------------------------------------------------------------
        // GetSupportedAlgorithms.
        //---------------------------------------------------------------------

        [Test]
        public void GetSupportedAlgorithms_WhenRequestingKex_ThenSupportedAlgorithmsIncludesDiffieHellman()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.KEX);

                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);

                CollectionAssert.Contains(algorithms, "ecdh-sha2-nistp256");
                CollectionAssert.Contains(algorithms, "ecdh-sha2-nistp384");
                CollectionAssert.Contains(algorithms, "ecdh-sha2-nistp521");

                CollectionAssert.Contains(algorithms, "diffie-hellman-group-exchange-sha256");
                CollectionAssert.Contains(algorithms, "diffie-hellman-group-exchange-sha1");
                CollectionAssert.Contains(algorithms, "diffie-hellman-group14-sha1");
                CollectionAssert.Contains(algorithms, "diffie-hellman-group1-sha1");
            }
        }

        [Test]
        public void GetSupportedAlgorithms_WhenRequestingHostkey_ThenSupportedAlgorithmsIncludesRsaAndEcdsa()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.HOSTKEY);

                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);

                CollectionAssert.Contains(algorithms, "ecdsa-sha2-nistp256");
                CollectionAssert.Contains(algorithms, "ecdsa-sha2-nistp384");
                CollectionAssert.Contains(algorithms, "ecdsa-sha2-nistp521");

                CollectionAssert.Contains(algorithms, "ssh-rsa");
                CollectionAssert.Contains(algorithms, "rsa-sha2-256");
                CollectionAssert.Contains(algorithms, "rsa-sha2-512");
            }
        }

        [Test]
        public void GetSupportedAlgorithms_WhenRequestingCryptCs_ThenSupportedAlgorithmsIncludesAes()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.CRYPT_CS);

                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);

                CollectionAssert.Contains(algorithms, "aes128-ctr");
                CollectionAssert.Contains(algorithms, "aes256-ctr");
            }
        }

        [Test]
        public void WGetSupportedAlgorithms_henRequestingCryptSc_ThenSupportedAlgorithmsIncludesAes()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.CRYPT_SC);

                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);

                CollectionAssert.Contains(algorithms, "aes128-ctr");
                CollectionAssert.Contains(algorithms, "aes256-ctr");
            }
        }

        [Test]
        public void GetSupportedAlgorithms_WhenRequestingMacSc_ThenSupportedAlgorithmsIncludesAes()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.MAC_SC);

                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);

                CollectionAssert.Contains(algorithms, "hmac-sha2-256");
                CollectionAssert.Contains(algorithms, "hmac-sha2-512");
            }
        }

        [Test]
        public void GetSupportedAlgorithms_WhenRequestingMacCs_ThenSupportedAlgorithmsIncludesAes()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.MAC_CS);

                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);

                CollectionAssert.Contains(algorithms, "hmac-sha2-256");
                CollectionAssert.Contains(algorithms, "hmac-sha2-512");
            }
        }

        [Test]
        public void GetSupportedAlgorithms_WhenRequestingInvalidType_ThenSupportedAlgorithmsThrowsException()
        {
            using (var session = CreateSession())
            {
                Assert.Throws<Libssh2Exception>(
                    () => session.GetSupportedAlgorithms((LIBSSH2_METHOD)int.MaxValue));
            }
        }

        //---------------------------------------------------------------------
        // SetPreferredMethods.
        //---------------------------------------------------------------------

        [Test]
        public async Task SetPreferredMethods_WhenPreferredMethodNotAccepted_ThenConnectFails(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            {
                var methods = new[] { "diffie-hellman-group-exchange-sha1" };
                session.SetPreferredMethods(
                    LIBSSH2_METHOD.KEX,
                    methods);

                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.KEX_FAILURE,
                    () => session.Connect(endpoint));
            }
        }

        [Test]
        public async Task SetPreferredMethods_WhenPreferredMethodIsInvalid_ThenConnectFails(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            {
                var methods = new[] { "invalid" };
                session.SetPreferredMethods(
                    (LIBSSH2_METHOD)(-1),
                    methods);

                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.INVAL,
                    () => session.Connect(endpoint));
            }
        }

        [Test]
        public void SetPreferredMethods_WhenPreferredMethodIsEmpty_ThenSetPreferredMethodsThrowsNotSupportedError()
        {
            using (var session = CreateSession())
            {
                Assert.Throws<ArgumentException>(
                    () => session.SetPreferredMethods(
                        LIBSSH2_METHOD.KEX,
                        Array.Empty<string>()));
            }
        }

        //---------------------------------------------------------------------
        // Handshake/Connect.
        //---------------------------------------------------------------------

        [Test]
        public async Task Connect_WhenPortIsCorrect_ThenHandshakeSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            using (var connection = session.Connect(endpoint))
            {
                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);
            }
        }

        [Test]
        public async Task Connect_WhenPortNotListening_ThenHandshakeThrowsSocketException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await GetPublicIpAddressForInstanceAsync(await instanceLocatorTask)
                    .ConfigureAwait(false),
                12);
            using (var session = CreateSession())
            {
                ExceptionAssert.ThrowsAggregateException<SocketException>(
                    () => session.Connect(endpoint));
            }
        }

        [Test]
        public void Connect_WhenPortIsNotSsh_ThenHandshakeThrowsDisconnect()
        {
            using (var session = CreateSession())
            {
                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.SOCKET_DISCONNECT,
                    () => session.Connect(this.NonSshEndpoint));
            }
        }
    }
}
