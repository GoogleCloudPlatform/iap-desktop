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
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.Ssh;
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
    public class TestOsLoginService
    {
        //---------------------------------------------------------------------
        // AuthorizeKeyPairAsync.
        //---------------------------------------------------------------------

        [Test]
        public void WhenArgumentsIncomplete_ThenAuthorizeKeyAsyncThrowsArgumentException()
        {
            var service = new OsLoginProfile(new Mock<IOsLoginClient>().Object);

            ExceptionAssert.ThrowsAggregateException<ArgumentNullException>(() => service.AuthorizeKeyPairAsync(
                null,
                OsLoginSystemType.Linux,
                new Mock<IAsymmetricKeyCredential>().Object,
                TimeSpan.FromDays(1),
                CancellationToken.None).Wait());

            ExceptionAssert.ThrowsAggregateException<ArgumentNullException>(() => service.AuthorizeKeyPairAsync(
                new ProjectLocator("project-1"),
                OsLoginSystemType.Linux,
                null,
                TimeSpan.FromDays(1),
                CancellationToken.None).Wait());
        }

        [Test]
        public void WhenValidityIsZeroOrNegative_ThenAuthorizeKeyAsyncThrowsArgumentException()
        {
            var service = new OsLoginProfile(new Mock<IOsLoginClient>().Object);

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(() => service.AuthorizeKeyPairAsync(
                new ProjectLocator("project-1"),
                OsLoginSystemType.Linux,
                new Mock<IAsymmetricKeyCredential>().Object,
                TimeSpan.FromDays(-1),
                CancellationToken.None).Wait());
            ExceptionAssert.ThrowsAggregateException<ArgumentException>(() => service.AuthorizeKeyPairAsync(
                new ProjectLocator("project-1"),
                OsLoginSystemType.Linux,
                new Mock<IAsymmetricKeyCredential>().Object,
                TimeSpan.FromSeconds(0),
                CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenAdapterReturnsMultipleAccounts_ThenAuthorizeKeyAsyncSelectsPrimary()
        {
            var adapter = new Mock<IOsLoginClient>();
            adapter
                .Setup(a => a.ImportSshPublicKeyAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<string>(),
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
                });
            var service = new OsLoginProfile(adapter.Object);

            using (var keyCredential = AsymmetricKeyCredential.CreateEphemeral(SshKeyType.EcdsaNistp256))
            {
                var key = await service
                    .AuthorizeKeyPairAsync(
                        new ProjectLocator("project-1"),
                        OsLoginSystemType.Linux,
                        keyCredential,
                        TimeSpan.FromDays(1),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual("joe3", key.Username);
                Assert.AreEqual(KeyAuthorizationMethods.Oslogin, key.AuthorizationMethod);
            }
        }

        [Test]
        public void WhenAdapterReturnsNoAccount_ThenAuthorizeKeyAsyncThrowsOsLoginSshKeyImportFailedException()
        {
            var adapter = new Mock<IOsLoginClient>();
            adapter
                .Setup(a => a.ImportSshPublicKeyAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginProfile());
            var service = new OsLoginProfile(adapter.Object);

            using (var keyCredential = AsymmetricKeyCredential.CreateEphemeral(SshKeyType.EcdsaNistp256))
            {
                ExceptionAssert.ThrowsAggregateException<OsLoginSshKeyImportFailedException>(
                    () => service.AuthorizeKeyPairAsync(
                        new ProjectLocator("project-1"),
                        OsLoginSystemType.Linux,
                        keyCredential,
                        TimeSpan.FromDays(1),
                        CancellationToken.None).Wait());
            }
        }

        //---------------------------------------------------------------------
        // ListAuthorizedKeysAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProfileIsEmpty_ThenListAuthorizedKeysReturnsEmptyList()
        {
            var adapter = new Mock<IOsLoginClient>();
            adapter.Setup(a => a.GetLoginProfileAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginProfile()
                {
                });

            var service = new OsLoginProfile(adapter.Object);

            var keys = await service
                .ListAuthorizedKeysAsync(CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsEmpty(keys);
        }

        [Test]
        public async Task WhenProfileContainsInvalidKeys_ThenListAuthorizedKeysIgnoresThem()
        {
            var adapter = new Mock<IOsLoginClient>();
            adapter.Setup(a => a.GetLoginProfileAsync(
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

            var service = new OsLoginProfile(adapter.Object);

            var keys = await service
                .ListAuthorizedKeysAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, keys.Count());
            Assert.AreEqual("bob@gmail.com", keys.First().Email);
            Assert.AreEqual("ssh-rsa", keys.First().KeyType);
            Assert.AreEqual("AAAA", keys.First().PublicKey);
            Assert.IsNull(keys.First().ExpireOn);
        }

        [Test]
        public async Task WhenProfileContainsKeyWithExpiryDate_ThenExpiryDateIsConverted()
        {
            var firstOfJan = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var adapter = new Mock<IOsLoginClient>();
            adapter.Setup(a => a.GetLoginProfileAsync(
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

            var service = new OsLoginProfile(adapter.Object);

            var keys = await service
                .ListAuthorizedKeysAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, keys.Count());
            Assert.AreEqual("bob@gmail.com", keys.First().Email);
            Assert.AreEqual("ssh-rsa", keys.First().KeyType);
            Assert.AreEqual("AAAA", keys.First().PublicKey);
            Assert.AreEqual(firstOfJan.Date, keys.First().ExpireOn);
        }

        //---------------------------------------------------------------------
        // DeleteAuthorizedKeyAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenKeyValid_ThenDeleteAuthorizedKeyDeletesKey()
        {
            var adapter = new Mock<IOsLoginClient>();
            adapter.Setup(a => a.GetLoginProfileAsync(
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

            var service = new OsLoginProfile(adapter.Object);
            var keys = await service
                .ListAuthorizedKeysAsync(CancellationToken.None)
                .ConfigureAwait(false);

            await service.DeleteAuthorizedKeyAsync(
                    keys.First(),
                    CancellationToken.None)
                .ConfigureAwait(false);

            adapter.Verify(a => a.DeleteSshPublicKeyAsync(
                    It.Is<string>(f => f == "fingerprint-1"),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }
    }
}
