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
        // Version.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRequriedVersionIsHigher_ThenVersionReturnsNull()
        {
            var version = Libssh2Session.GetVersion(new Version(0xCC, 0xBB, 0xAA));
            Assert.IsNull(version);
        }

        [Test]
        public void WhenRequriedVersionIsLower_ThenVersionReturnsXxx()
        {
            var version = Libssh2Session.GetVersion(new Version(1, 0, 0));
            Assert.IsNotNull(version);
        }

        //---------------------------------------------------------------------
        // Timeout.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTimeoutSet_ThenTimeoutReflectsValue()
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
        public void WhenUsingDefaults_ThenBlockingIsEnabled()
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
        public void WhenLastErrorStillApplies_ThenExceptionContainsErrorMessage()
        {
            using (var session = CreateSession())
            {
                // Trigger an error
                try
                {
                    session.SetPreferredMethods(LIBSSH2_METHOD.KEX, new[] { "invalid" });
                }
                catch (Exception)
                { }

                var exception = session.CreateException(LIBSSH2_ERROR.METHOD_NOT_SUPPORTED);
                Assert.AreEqual(
                    "The requested method(s) are not currently supported",
                    exception.Message);
                Assert.AreEqual(LIBSSH2_ERROR.METHOD_NOT_SUPPORTED, exception.ErrorCode);
            }
        }

        [Test]
        public void WhenLastErrorDoesNotApplyAnymore_ThenExceptionContainsGenericErrorMessage()
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
        // Algorithms.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRequestingKex_ThenSupportedAlgorithmsIncludesDiffieHellman()
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
        public void WhenRequestingHostkey_ThenSupportedAlgorithmsIncludesRsaAndDss()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.HOSTKEY);

                Assert.AreEqual(LIBSSH2_ERROR.NONE, session.LastError);

                CollectionAssert.Contains(algorithms, "ecdh-sha2-nistp256");
                CollectionAssert.Contains(algorithms, "ecdh-sha2-nistp384");
                CollectionAssert.Contains(algorithms, "ecdh-sha2-nistp521");

                CollectionAssert.Contains(algorithms, "ssh-rsa");
                CollectionAssert.Contains(algorithms, "rsa-sha2-256");
                CollectionAssert.Contains(algorithms, "rsa-sha2-512");
            }
        }

        [Test]
        public void WhenRequestingCryptCs_ThenSupportedAlgorithmsIncludesAes()
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
        public void WhenRequestingCryptSc_ThenSupportedAlgorithmsIncludesAes()
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
        public void WhenRequestingMacSc_ThenSupportedAlgorithmsIncludesAes()
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
        public void WhenRequestingMacCs_ThenSupportedAlgorithmsIncludesAes()
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
        public void WhenRequestingInvalidType_ThenSupportedAlgorithmsThrowsException()
        {
            using (var session = CreateSession())
            {
                Assert.Throws<ArgumentException>(
                    () => session.GetSupportedAlgorithms((LIBSSH2_METHOD)int.MaxValue));
            }
        }

        [Test]
        public async Task WhenPreferringIncompatibleAlgorithm_ThenConnectFails(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instance = await instanceLocatorTask;
            var endpoint = await GetPublicSshEndpointAsync(instance).ConfigureAwait(false);

            using (var session = CreateSession())
            {
                session.SetPreferredMethods(
                    LIBSSH2_METHOD.KEX,
                    new[] { "diffie-hellman-group-exchange-sha1" });

                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.KEX_FAILURE,
                    () => session.Connect(endpoint));
            }
        }

        [Test]
        public void WhenPreferredMethodIsEmpty_ThenSetPreferredMethodsThrowsNotSupportedError()
        {
            using (var session = CreateSession())
            {
                Assert.Throws<ArgumentException>(
                    () => session.SetPreferredMethods(
                        LIBSSH2_METHOD.KEX,
                        Array.Empty<string>()));
            }
        }

        [Test]
        public void WhenPreferredMethodIsInvalid_ThenSetPreferredMethodsThrowsArgumentException()
        {
            using (var session = CreateSession())
            {
                SshAssert.ThrowsNativeExceptionWithError(
                    session,
                    LIBSSH2_ERROR.METHOD_NOT_SUPPORTED,
                    () => session.SetPreferredMethods(
                        LIBSSH2_METHOD.KEX,
                        new[] { "invalid" }));
            }
        }

        //---------------------------------------------------------------------
        // Handshake/Connect.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPortIsCorrect_ThenHandshakeSucceeds(
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
        public async Task WhenPortNotListening_ThenHandshakeThrowsSocketException(
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
        public void WhenPortIsNotSsh_ThenHandshakeThrowsDisconnect()
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
