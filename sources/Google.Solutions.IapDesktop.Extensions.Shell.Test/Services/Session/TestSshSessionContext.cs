//
// Copyright 2023 Google LLC
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
using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.Ssh.Auth;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Session
{
    [TestFixture]
    public class TestSshSessionContext
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        [Test]
        public void AuthorizeCredentialReturnsCredential()
        {
            var authorizedKey = AuthorizedKeyPair.ForMetadata(
                new Mock<ISshKeyPair>().Object,
                "username",
                false,
                new Mock<IAuthorization>().Object);

            var key = new Mock<ISshKeyPair>().Object;
            var keyAuthService = new Mock<IKeyAuthorizationService>();
            keyAuthService
                .Setup(s => s.AuthorizeKeyAsync(
                    SampleInstance,
                    key,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string>(),
                    KeyAuthorizationMethods.All,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(authorizedKey);

            var context = new SshSessionContext(
                new Mock<ITunnelBrokerService>().Object,
                keyAuthService.Object,
                SampleInstance,
                key);

            Assert.AreSame(
                authorizedKey,
                context.AuthorizeCredentialAsync(CancellationToken.None)
                    .Result
                    .Key);
        }
    }
}
