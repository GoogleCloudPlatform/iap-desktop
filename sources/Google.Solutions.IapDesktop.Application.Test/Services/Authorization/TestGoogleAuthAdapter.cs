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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Authorization
{
    [TestFixture]
    public class TestGoogleAuthAdapter : ApplicationFixtureBase
    {
        [Test]
        public async Task WhenCalled_ThenQueryOpenIdConfigurationAsyncReturnsInfo()
        {
            var adapter = new GoogleAuthAdapter(
                new Apis.Auth.OAuth2.ClientSecrets(),
                new[] { "openid" },
                new NullDataStore(),
                string.Empty);

            var config = await adapter
                .QueryOpenIdConfigurationAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(config.UserInfoEndpoint);
        }

        [Test]
        public async Task WhenRefreshTokenValidAndAllScopesCovered_ThenTryAuthorizeUsingRefreshTokenAsyncReturnsCredential()
        {
            var flow = new Mock<IAuthorizationCodeFlow>();
            flow.Setup(f => f.LoadTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TokenResponse()
                {
                    RefreshToken = "refresh-token-1",
                    Scope = "scope-1 " + GoogleAuthAdapter.EmailScope
                });
            flow.Setup(f => f.ShouldForceTokenRetrieval())
                .Returns(false);

            var dataStore = new Mock<IDataStore>();

            var adapter = new GoogleAuthAdapter(
                new Apis.Auth.OAuth2.ClientSecrets(),
                new[] { "scope-1" },
                dataStore.Object,
                string.Empty,
                _ => flow.Object);

            var credential = await adapter
                .TryAuthorizeUsingRefreshTokenAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(credential);
            Assert.IsInstanceOf<UserCredential>(credential);
        }

        [Test]
        public async Task WhenRefreshTokenValidButLacksScopes_ThenTryAuthorizeUsingRefreshTokenAsyncReturnsNull()
        {
            var flow = new Mock<IAuthorizationCodeFlow>();
            flow.Setup(f => f.LoadTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TokenResponse()
                {
                    RefreshToken = "refresh-token-1",
                    Scope = "scope-1 scope-2"
                });
            flow.Setup(f => f.ShouldForceTokenRetrieval())
                .Returns(false);

            var dataStore = new Mock<IDataStore>();

            var adapter = new GoogleAuthAdapter(
                new Apis.Auth.OAuth2.ClientSecrets(),
                new[] { "scope-1" },
                dataStore.Object,
                string.Empty,
                _ => flow.Object);

            var credential = await adapter
                .TryAuthorizeUsingRefreshTokenAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(credential);
        }

        [Test]
        public async Task WhenRefreshTokenInvalid_ThenTryAuthorizeUsingRefreshTokenAsyncReturnsNull()
        {
            var flow = new Mock<IAuthorizationCodeFlow>();
            flow.Setup(f => f.ShouldForceTokenRetrieval())
                .Returns(true);

            var dataStore = new Mock<IDataStore>();

            var adapter = new GoogleAuthAdapter(
                new Apis.Auth.OAuth2.ClientSecrets(),
                new[] { "scope-1" },
                dataStore.Object,
                string.Empty,
                _ => flow.Object);

            var credential = await adapter
                .TryAuthorizeUsingRefreshTokenAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(credential);
        }
    }
}