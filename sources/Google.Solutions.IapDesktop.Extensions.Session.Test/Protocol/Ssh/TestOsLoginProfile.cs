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

using Google.Apis.CloudOSLogin.v1.Data;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    public class TestOsLoginProfile
    {
        private static readonly ZoneLocator SampleZone =
            new ZoneLocator("project-1", "zone-1");

        private static IAuthorization CreateAuthorization<TSession>()
            where TSession : class, IOidcSession
        {
            var session = new Mock<TSession>();
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Session)
                .Returns(session.Object);

            return authorization.Object;
        }

        //---------------------------------------------------------------------
        // LookupUsername.
        //---------------------------------------------------------------------

        [Test]
        public void LookupUsername_WhenProfileContainsMultipleAccounts_ThenLookupUsernamePrimary()
        {
            var loginProfile = new LoginProfile()
            {
                PosixAccounts = new[]
                {
                    new PosixAccount()
                    {
                        AccountId = "1",
                        Primary = false,
                        OperatingSystemType = "DOS",
                        Username = "joe1"
                    },
                    new PosixAccount()
                    {
                        AccountId = "2",
                        Primary = true,
                        OperatingSystemType = "DOS",
                        Username = "joe2"
                    },
                    new PosixAccount()
                    {
                        AccountId = "3",
                        Primary = true,
                        OperatingSystemType = "LINUX",
                        Username = "joe3"
                    }
                }
            };

            Assert.AreEqual("joe3", OsLoginProfile.LookupUsername(loginProfile));
        }

        [Test]
        public void LookupUsername_WhenProfileContainsNoAccount_ThenLookupUsernameThrowsException()
        {
            Assert.Throws<InvalidOsLoginProfileException>(
                () => OsLoginProfile.LookupUsername(new LoginProfile()));
        }

        //---------------------------------------------------------------------
        // AuthorizeKey.
        //---------------------------------------------------------------------

        [Test]
        public void AuthorizeKey_WhenArgumentsIncomplete_ThenAuthorizeKeyAsyncThrowsArgumentException()
        {
            var profile = new OsLoginProfile(
                new Mock<IOsLoginClient>().Object,
                CreateAuthorization<IGaiaOidcSession>());

            ExceptionAssert.ThrowsAggregateException<ArgumentNullException>(() => profile.AuthorizeKeyAsync(
                null!,
                OsLoginSystemType.Linux,
                new Mock<IAsymmetricKeySigner>().Object,
                TimeSpan.FromDays(1),
                CancellationToken.None).Wait());

            ExceptionAssert.ThrowsAggregateException<ArgumentNullException>(() => profile.AuthorizeKeyAsync(
                SampleZone,
                OsLoginSystemType.Linux,
                null!,
                TimeSpan.FromDays(1),
                CancellationToken.None).Wait());
        }

        [Test]
        public void AuthorizeKey_WhenValidityIsZeroOrNegative_ThenAuthorizeKeyAsyncThrowsArgumentException()
        {
            var profile = new OsLoginProfile(
                new Mock<IOsLoginClient>().Object,
                CreateAuthorization<IGaiaOidcSession>());

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(() => profile.AuthorizeKeyAsync(
                SampleZone,
                OsLoginSystemType.Linux,
                new Mock<IAsymmetricKeySigner>().Object,
                TimeSpan.FromDays(-1),
                CancellationToken.None).Wait());
            ExceptionAssert.ThrowsAggregateException<ArgumentException>(() => profile.AuthorizeKeyAsync(
                SampleZone,
                OsLoginSystemType.Linux,
                new Mock<IAsymmetricKeySigner>().Object,
                TimeSpan.FromSeconds(0),
                CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // AuthorizeKey - Gaia.
        //---------------------------------------------------------------------

        [Test]
        public async Task AuthorizeKey_WhenUsingGaiaSession_ThenAuthorizeImportsKey()
        {
            var client = new Mock<IOsLoginClient>();
            client
                .Setup(a => a.ImportSshPublicKeyAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginProfile()
                {
                    PosixAccounts = new[]
                    {
                        new PosixAccount()
                        {
                            AccountId = "1",
                            Primary = true,
                            OperatingSystemType = "LINUX",
                            Username = "joe"
                        }
                    }
                });

            var profile = new OsLoginProfile(
                client.Object,
                CreateAuthorization<IGaiaOidcSession>());

            using (var signer = AsymmetricKeySigner.CreateEphemeral(SshKeyType.EcdsaNistp256))
            using (var credential = await profile
                .AuthorizeKeyAsync(
                    SampleZone,
                    OsLoginSystemType.Linux,
                    signer,
                    TimeSpan.FromDays(1),
                    CancellationToken.None)
                .ConfigureAwait(false))
            {
                client.Verify(c => c.ImportSshPublicKeyAsync(
                    new ProjectLocator(SampleZone.ProjectId),
                    signer.PublicKey.ToString(PublicKey.Format.OpenSsh),
                    TimeSpan.FromDays(1),
                    CancellationToken.None), Times.Once());
                Assert.AreEqual("joe", credential.Username);
                Assert.AreEqual(KeyAuthorizationMethods.Oslogin, credential.AuthorizationMethod);
            }
        }

        [Test]
        public async Task AuthorizeKey_WhenUsingWorkforceSession_ThenAuthorizeSignsKey()
        {
            var client = new Mock<IOsLoginClient>();
            client
                .Setup(a => a.SignPublicKeyAsync(
                    It.IsAny<ZoneLocator>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("ecdsa-sha2-nistp256-cert-v01@openssh.com AAAA joe");

            var profile = new OsLoginProfile(
                client.Object,
                CreateAuthorization<IWorkforcePoolSession>());

            using (var signer = AsymmetricKeySigner.CreateEphemeral(SshKeyType.EcdsaNistp256))
            using (var credential = await profile
                .AuthorizeKeyAsync(
                    SampleZone,
                    OsLoginSystemType.Linux,
                    signer,
                    TimeSpan.FromDays(1),
                    CancellationToken.None)
                .ConfigureAwait(false))
            {
                client.Verify(c => c.SignPublicKeyAsync(
                    SampleZone,
                    signer.PublicKey.ToString(PublicKey.Format.OpenSsh),
                    CancellationToken.None), Times.Once());
                Assert.AreEqual("joe", credential.Username);
                Assert.AreEqual(KeyAuthorizationMethods.Oslogin, credential.AuthorizationMethod);
                Assert.IsInstanceOf<OsLoginCertificateSigner>(credential.Signer);
            }
        }

        //---------------------------------------------------------------------
        // ListAuthorizedKeys.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListAuthorizedKeys_WhenProfileIsEmpty_ThenListAuthorizedKeysReturnsEmptyList()
        {
            var client = new Mock<IOsLoginClient>();
            client.Setup(a => a.GetLoginProfileAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginProfile()
                {
                });

            var profile = new OsLoginProfile(
                client.Object,
                CreateAuthorization<IGaiaOidcSession>());

            var keys = await profile
                .ListAuthorizedKeysAsync(CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsEmpty(keys);
        }

        [Test]
        public async Task ListAuthorizedKeys_WhenProfileContainsInvalidKeys_ThenListAuthorizedKeysIgnoresThem()
        {
            var client = new Mock<IOsLoginClient>();
            client.Setup(a => a.GetLoginProfileAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginProfile()
                {
                    SshPublicKeys = new Dictionary<string, SshPublicKey>
                    {
                        {
                            "invalid-1",
                            new SshPublicKey()
                            {
                                Fingerprint = "invalid-1",
                                Key = "",
                                Name = "users/bob@gmail.com/sshPublicKeys/invalid-1"
                            }
                        },
                        {
                            "invalid-2",
                            new SshPublicKey()
                            {
                                Fingerprint = "invalid-1",
                                Key = "ssh-rsa AAAA",
                                Name = "JUNK/bob@gmail.com/sshPublicKeys"
                            }
                        },
                        {
                            "valid",
                            new SshPublicKey()
                            {
                                Fingerprint = "valid",
                                Key = "ssh-rsa AAAA",
                                Name = "users/bob@gmail.com/sshPublicKeys/valid"
                            }
                        }
                    }
                });

            var profile = new OsLoginProfile(
                client.Object,
                CreateAuthorization<IGaiaOidcSession>());

            var keys = await profile
                .ListAuthorizedKeysAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, keys.Count());
            Assert.AreEqual("bob@gmail.com", keys.First().Email);
            Assert.AreEqual("ssh-rsa", keys.First().KeyType);
            Assert.AreEqual("AAAA", keys.First().PublicKey);
            Assert.IsNull(keys.First().ExpireOn);
        }

        [Test]
        public async Task ListAuthorizedKeys_WhenProfileContainsKeyWithExpiryDate_ThenExpiryDateIsConverted()
        {
            var firstOfJan = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var client = new Mock<IOsLoginClient>();
            client.Setup(a => a.GetLoginProfileAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginProfile()
                {
                    SshPublicKeys = new Dictionary<string, SshPublicKey>
                    {
                        {
                            "expiring",
                            new SshPublicKey()
                            {
                                Fingerprint = "expiring",
                                Key = "ssh-rsa AAAA",
                                Name = "users/bob@gmail.com/sshPublicKeys/expiring",
                                ExpirationTimeUsec = firstOfJan.ToUnixTimeMicroseconds()
                            }
                        }
                    }
                });

            var profile = new OsLoginProfile(
                client.Object,
                CreateAuthorization<IGaiaOidcSession>());

            var keys = await profile
                .ListAuthorizedKeysAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, keys.Count());
            Assert.AreEqual("bob@gmail.com", keys.First().Email);
            Assert.AreEqual("ssh-rsa", keys.First().KeyType);
            Assert.AreEqual("AAAA", keys.First().PublicKey);
            Assert.AreEqual(firstOfJan.Date, keys.First().ExpireOn);
        }

        //---------------------------------------------------------------------
        // DeleteAuthorizedKey.
        //---------------------------------------------------------------------

        [Test]
        public async Task DeleteAuthorizedKey_WhenKeyValid_ThenDeleteAuthorizedKeyDeletesKey()
        {
            var client = new Mock<IOsLoginClient>();
            client.Setup(a => a.GetLoginProfileAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginProfile()
                {
                    SshPublicKeys = new Dictionary<string, SshPublicKey>
                    {
                        {
                            "fingerprint-1",
                            new SshPublicKey()
                            {
                                Fingerprint = "fingerprint-1",
                                Key = "ssh-rsa AAAA",
                                Name = "users/bob@gmail.com/sshPublicKeys/fingerprint-1"
                            }
                        }
                    }
                });

            var profile = new OsLoginProfile(
                client.Object,
                CreateAuthorization<IGaiaOidcSession>());

            var keys = await profile
                .ListAuthorizedKeysAsync(CancellationToken.None)
                .ConfigureAwait(false);

            await profile.DeleteAuthorizedKeyAsync(
                    keys.First(),
                    CancellationToken.None)
                .ConfigureAwait(false);

            client.Verify(a => a.DeleteSshPublicKeyAsync(
                    It.Is<string>(f => f == "fingerprint-1"),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }
    }
}
