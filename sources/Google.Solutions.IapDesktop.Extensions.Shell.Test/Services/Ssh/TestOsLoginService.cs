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
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Ssh
{
    [TestFixture]
    public class TestOsLoginService : ApplicationFixtureBase
    {
        [Test]
        public void WhenArgumentsIncomplete_ThenAuthorizeKeyAsyncThrowsArgumentException()
        {
            var service = new OsLoginService(new Mock<IOsLoginAdapter>().Object);

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(() => service.AuthorizeKeyAsync(
                "",
                OsLoginSystemType.Linux,
                new Mock<ISshKey>().Object,
                TimeSpan.FromDays(1),
                CancellationToken.None).Wait());

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(() => service.AuthorizeKeyAsync(
                null,
                OsLoginSystemType.Linux,
                new Mock<ISshKey>().Object,
                TimeSpan.FromDays(1),
                CancellationToken.None).Wait());

            ExceptionAssert.ThrowsAggregateException<ArgumentNullException>(() => service.AuthorizeKeyAsync(
                "project-1",
                OsLoginSystemType.Linux,
                null,
                TimeSpan.FromDays(1),
                CancellationToken.None).Wait());
        }

        [Test]
        public void WhenValidityIsZeroOrNegative_ThenAuthorizeKeyAsyncThrowsArgumentException()
        {
            var service = new OsLoginService(new Mock<IOsLoginAdapter>().Object);

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(() => service.AuthorizeKeyAsync(
                "project-1",
                OsLoginSystemType.Linux,
                new Mock<ISshKey>().Object,
                TimeSpan.FromDays(-1),
                CancellationToken.None).Wait());
            ExceptionAssert.ThrowsAggregateException<ArgumentException>(() => service.AuthorizeKeyAsync(
                "project-1",
                OsLoginSystemType.Linux,
                new Mock<ISshKey>().Object,
                TimeSpan.FromSeconds(0),
                CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenAdapterReturnsMultipleAccounts_ThenAuthorizeKeyAsyncSelectsPrimary()
        {
            var adapter = new Mock<IOsLoginAdapter>();
            adapter
                .Setup(a => a.ImportSshPublicKeyAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<ISshKey>(),
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
            var service = new OsLoginService(adapter.Object);

            var key = await service
                .AuthorizeKeyAsync(
                    "project-1",
                    OsLoginSystemType.Linux,
                    new Mock<ISshKey>().Object,
                    TimeSpan.FromDays(1),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("joe3", key.Username);
            Assert.AreEqual(AuthorizeKeyMethods.Oslogin, key.AuthorizationMethod);
        }

        [Test]
        public void WhenAdapterReturnsNoAccount_ThenAuthorizeKeyAsyncThrowsOsLoginSshKeyImportFailedException()
        {

            var adapter = new Mock<IOsLoginAdapter>();
            adapter
                .Setup(a => a.ImportSshPublicKeyAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<ISshKey>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginProfile());
            var service = new OsLoginService(adapter.Object);

            ExceptionAssert.ThrowsAggregateException<OsLoginSshKeyImportFailedException>(
                () => service.AuthorizeKeyAsync(
                    "project-1",
                    OsLoginSystemType.Linux,
                    new Mock<ISshKey>().Object,
                    TimeSpan.FromDays(1),
                    CancellationToken.None).Wait());
        }
    }
}
