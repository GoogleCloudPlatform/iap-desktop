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

using Google.Apis.Util.Store;
using Google.Solutions.Common.Auth;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Auth
{
    [TestFixture]
    public class TestGoogleAuthAdapter : CommonFixtureBase
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

        
        // TODO: restore tests
        //[Test]
        //public async Task WhenExistingAuthLacksScopes_TryLoadExistingAuthorizationAsyncReturnsNullAndExistingAuthzIsDeleted()
        //{
        //    var tokenResponse = new TokenResponse()
        //    {
        //        RefreshToken = "rt",
        //        Scope = "one two"
        //    };

        //    var adapter = new Mock<IAuthAdapter>();
        //    adapter.Setup(a => a.GetStoredRefreshTokenAsync(It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(tokenResponse));
        //    adapter.Setup(a => a.IsRefreshTokenValid(tokenResponse))
        //        .Returns(true);
        //    adapter.SetupGet(a => a.Scopes)
        //        .Returns(new[] { "one", "two", "email" });

        //    var authz = await OAuthAuthorization.TryLoadExistingAuthorizationAsync(
        //            adapter.Object,
        //            CancellationToken.None)
        //        .ConfigureAwait(false);

        //    Assert.IsNull(authz);

        //    adapter.Verify(a => a.DeleteStoredRefreshToken(), Times.Once);
        //}

        //[Test]
        //public async Task WhenExistingAuthIsOk_TryLoadExistingAuthorizationAsyncReturnsAuthorization()
        //{
        //    var tokenResponse = new TokenResponse()
        //    {
        //        RefreshToken = "rt",
        //        Scope = "email one two"
        //    };

        //    var adapter = new Mock<IAuthAdapter>();
        //    adapter.Setup(a => a.GetStoredRefreshTokenAsync(It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(tokenResponse));
        //    adapter.Setup(a => a.IsRefreshTokenValid(tokenResponse))
        //        .Returns(true);
        //    adapter.SetupGet(a => a.Scopes)
        //        .Returns(new[] { "one", "two", "email" });

        //    var authz = await OAuthAuthorization.TryLoadExistingAuthorizationAsync(
        //            adapter.Object,
        //            CancellationToken.None)
        //        .ConfigureAwait(false);

        //    Assert.IsNotNull(authz);

        //    adapter.Verify(a => a.AuthorizeUsingRefreshToken(tokenResponse), Times.Once);
        //    adapter.Verify(a => a.QueryUserInfoAsync(
        //        It.IsAny<ICredential>(),
        //        It.IsAny<CancellationToken>()), Times.Once);
        //}
    }
}