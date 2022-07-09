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
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.Testing.Application.Test;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Authorization
{
    [TestFixture]
    public class TestSignInAdapter : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // TrySignInWithRefreshTokenAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenRefreshTokenValidAndAllScopesCovered_ThenTrySignInWithRefreshTokenAsyncReturnsCredential()
        {
            var flow = new Mock<IAuthorizationCodeFlow>();
            flow.Setup(f => f.LoadTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TokenResponse()
                {
                    RefreshToken = "refresh-token-1",
                    Scope = "scope-1 " + SignInAdapter.EmailScope
                });
            flow.Setup(f => f.ShouldForceTokenRetrieval())
                .Returns(false);

            var adapter = new SignInAdapter(
                null,
                new Apis.Auth.OAuth2.ClientSecrets(),
                new[] { "scope-1" },
                new Mock<IDataStore>().Object,
                new Mock<ICodeReceiver>().Object,
                _ => flow.Object);

            var credential = await adapter
                .TrySignInWithRefreshTokenAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(credential);
            Assert.IsInstanceOf<UserCredential>(credential);
        }

        [Test]
        public async Task WhenRefreshTokenValidButLacksScopes_ThenTrySignInWithRefreshTokenAsyncReturnsNull()
        {
            var flow = new Mock<IAuthorizationCodeFlow>();
            flow.Setup(f => f.LoadTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TokenResponse()
                {
                    RefreshToken = "refresh-token-1",
                    Scope = "scope-1"
                });
            flow.Setup(f => f.ShouldForceTokenRetrieval())
                .Returns(false);

            var adapter = new SignInAdapter(
                null,
                new Apis.Auth.OAuth2.ClientSecrets(),
                new[] { "scope-1", "scope-2" },
                new Mock<IDataStore>().Object,
                new Mock<ICodeReceiver>().Object,
                _ => flow.Object);

            var credential = await adapter
                .TrySignInWithRefreshTokenAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(credential);
        }

        [Test]
        public async Task WhenRefreshTokenInvalid_ThenTrySignInWithRefreshTokenAsyncReturnsNull()
        {
            var flow = new Mock<IAuthorizationCodeFlow>();
            flow.Setup(f => f.ShouldForceTokenRetrieval()).Returns(true);

            var adapter = new SignInAdapter(
                null,
                new Apis.Auth.OAuth2.ClientSecrets(),
                new[] { "scope-1" },
                new Mock<IDataStore>().Object,
                new Mock<ICodeReceiver>().Object,
                _ => flow.Object);

            var credential = await adapter
                .TrySignInWithRefreshTokenAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(credential);
        }

        //---------------------------------------------------------------------
        // SignInWithBrowserAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSignInSucceeds_ThenSignInWithBrowserAsyncReturnsCredential()
        {
            var receiver = new Mock<ICodeReceiver>();
            receiver.Setup(r => r.ReceiveCodeAsync(
                    It.IsAny<AuthorizationCodeRequestUrl>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthorizationCodeResponseUrl()
                {
                    Code = "123"
                });

            var flow = new Mock<IAuthorizationCodeFlow>();
            flow.Setup(f => f.ShouldForceTokenRetrieval()).Returns(true);
            flow.Setup(f => f.ExchangeCodeForTokenAsync(
                    It.IsAny<string>(),
                    It.Is<string>(code => code == "123"),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TokenResponse()
                {
                    Scope = "scope-1",
                    AccessToken = "token-1",
                    RefreshToken = "refreshtoken-1"
                });

            var adapter = new SignInAdapter(
                null,
                new Apis.Auth.OAuth2.ClientSecrets(),
                new[] { "scope-1" },
                new Mock<IDataStore>().Object,
                receiver.Object,
                _ => flow.Object);

            var credential = await adapter.SignInWithBrowserAsync(
                    "bob@example.com",
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.NotNull(credential);
        }

        [Test]
        public void WhenSignInLacksScopes_ThenSignInWithBrowserAsyncThrowsException()
        {
            var receiver = new Mock<ICodeReceiver>();
            receiver.Setup(r => r.ReceiveCodeAsync(
                    It.IsAny<AuthorizationCodeRequestUrl>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthorizationCodeResponseUrl()
                {
                    Code = "123"
                });

            var flow = new Mock<IAuthorizationCodeFlow>();
            flow.Setup(f => f.ShouldForceTokenRetrieval()).Returns(true);
            flow.Setup(f => f.ExchangeCodeForTokenAsync(
                    It.IsAny<string>(),
                    It.Is<string>(code => code == "123"),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TokenResponse()
                {
                    Scope = "", // Scope missing
                    AccessToken = "token-1",
                    RefreshToken = "refreshtoken-1"
                });

            var adapter = new SignInAdapter(
                null,
                new Apis.Auth.OAuth2.ClientSecrets(),
                new[] { "scope-1" },
                new Mock<IDataStore>().Object,
                receiver.Object,
                _ => flow.Object);

            ExceptionAssert.ThrowsAggregateException<AuthorizationFailedException>(
                () => adapter.SignInWithBrowserAsync(
                    "bob@example.com",
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenSignInFailsBecauseOfConditionalAccess_ThenSignInWithBrowserAsyncThrowsException()
        {
            var receiver = new Mock<ICodeReceiver>();
            receiver.Setup(r => r.ReceiveCodeAsync(
                    It.IsAny<AuthorizationCodeRequestUrl>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthorizationCodeResponseUrl()
                {
                    Code = "123"
                });

            var flow = new Mock<IAuthorizationCodeFlow>();
            flow.Setup(f => f.ShouldForceTokenRetrieval()).Returns(true);
            flow.Setup(f => f.ExchangeCodeForTokenAsync(
                    It.IsAny<string>(),
                    It.Is<string>(code => code == "123"),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TokenResponseException(new TokenErrorResponse()
                {
                    Error = "access_denied",
                    ErrorDescription = "Account restricted",
                    ErrorUri = "https://accounts.google.com/info/servicerestricted?es\u003d..."
                }));

            var adapter = new SignInAdapter(
                null,
                new Apis.Auth.OAuth2.ClientSecrets(),
                new[] { "scope-1" },
                new Mock<IDataStore>().Object,
                receiver.Object,
                _ => flow.Object);

            ExceptionAssert.ThrowsAggregateException<AuthorizationFailedException>(
                () => adapter.SignInWithBrowserAsync(
                    "bob@example.com",
                    CancellationToken.None).Wait());
        }
    }
}