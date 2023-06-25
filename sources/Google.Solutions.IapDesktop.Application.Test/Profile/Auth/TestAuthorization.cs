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
using Google.Solutions.Apis.Auth;
using Google.Solutions.IapDesktop.Application.Profile.Auth;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Auth
{
    [TestFixture]
    public class TestAuthorization : ApplicationFixtureBase
    {
        private static UserCredential CreateUserCredentialMock(string refreshToken = null)
        {
            return new UserCredential(
                new Mock<IAuthorizationCodeFlow>().Object,
                "mock",
                new Google.Apis.Auth.OAuth2.Responses.TokenResponse()
                {
                    RefreshToken = refreshToken
                });
        }

        //---------------------------------------------------------------------
        // RevokeAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task RevokeAsyncDeletesRefreshToken()
        {
            var adapter = new Mock<ISignInAdapter>();
            adapter.
                Setup(a => a.SignInWithBrowserAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateUserCredentialMock());
            adapter
                .Setup(a => a.QueryUserInfoAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfo());

            var deviceEnrollment = new Mock<IDeviceEnrollment>();

            var authorization = await Authorization.CreateAuthorizationAsync(
                    adapter.Object,
                    deviceEnrollment.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            await authorization.RevokeAsync();

            adapter.Verify(a => a.DeleteRefreshTokenAsync(), Times.Once);
        }

        //---------------------------------------------------------------------
        // TryLoadExistingAuthorizationAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenRefreshTokenValid_ThenTryLoadExistingAuthorizationAsyncReturnsObject()
        {
            var adapter = new Mock<ISignInAdapter>();
            adapter
                .Setup(a => a.TrySignInWithRefreshTokenAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateUserCredentialMock());
            adapter
                .Setup(a => a.QueryUserInfoAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfo()
                {
                    Email = "bob@example.com"
                });

            var authorization = await Authorization.TryLoadExistingAuthorizationAsync(
                    adapter.Object,
                    new Mock<IDeviceEnrollment>().Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authorization);
            Assert.AreEqual("bob@example.com", authorization.Email);
            Assert.AreEqual("bob@example.com", authorization.UserInfo.Email);
        }

        [Test]
        public async Task WhenUsingRefreshTokenFails_ThenTryLoadExistingAuthorizationReturnsNull()
        {
            var adapter = new Mock<ISignInAdapter>();
            adapter
                .Setup(a => a.TrySignInWithRefreshTokenAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCredential)null);

            var authorization = await Authorization.TryLoadExistingAuthorizationAsync(
                    adapter.Object,
                    new Mock<IDeviceEnrollment>().Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(authorization);
        }

        //---------------------------------------------------------------------
        // CreateAuthorizationAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateAuthorizationAsyncReturnsObject()
        {
            var adapter = new Mock<ISignInAdapter>();
            adapter
                .Setup(a => a.SignInWithBrowserAsync(
                    It.Is<string>(loginHint => loginHint == null),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateUserCredentialMock());
            adapter
                .Setup(a => a.QueryUserInfoAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfo()
                {
                    Email = "bob@example.com"
                });

            var authorization = await Authorization.CreateAuthorizationAsync(
                    adapter.Object,
                    new Mock<IDeviceEnrollment>().Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authorization);
            Assert.AreEqual("bob@example.com", authorization.Email);
            Assert.AreEqual("bob@example.com", authorization.UserInfo.Email);
        }

        //---------------------------------------------------------------------
        // ReauthorizeAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenReauthorizeUsesDifferentUser_ThenReauthorizeAsyncSwapsToken()
        {
            var userCredential = CreateUserCredentialMock("original");

            var adapter = new Mock<ISignInAdapter>();
            adapter
                .Setup(a => a.SignInWithBrowserAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(userCredential);
            adapter
                .Setup(a => a.QueryUserInfoAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfo()
                {
                    Email = "alice@example.com"
                });

            var authorization = await Authorization.CreateAuthorizationAsync(
                    adapter.Object,
                    new Mock<IDeviceEnrollment>().Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authorization);
            Assert.AreEqual("alice@example.com", authorization.Email);
            Assert.AreEqual("alice@example.com", authorization.UserInfo.Email);

            adapter
                .Setup(a => a.SignInWithBrowserAsync(
                   It.IsAny<string>(),
                   It.IsAny<CancellationToken>()))
               .ReturnsAsync(CreateUserCredentialMock("new"));
            adapter
                .Setup(a => a.QueryUserInfoAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfo()
                {
                    Email = "bob@example.com"
                });

            var reauthorizeEventTriggered = false;
            authorization.Reauthorized += (_, __) => reauthorizeEventTriggered = true;

            await authorization.ReauthorizeAsync(CancellationToken.None);

            Assert.IsTrue(reauthorizeEventTriggered);

            // New user info
            Assert.IsNotNull(authorization);
            Assert.AreEqual("bob@example.com", authorization.Email);
            Assert.AreEqual("bob@example.com", authorization.UserInfo.Email);

            // Same credential object, but new token.
            Assert.AreSame(userCredential, authorization.Credential);
            Assert.AreEqual("new", ((UserCredential)authorization.Credential).Token.RefreshToken);
        }

        [Test]
        public async Task WhenExistingEmailKnown_ThenReauthorizeAsyncSetsLoginHint()
        {
            var adapter = new Mock<ISignInAdapter>();
            adapter
                .Setup(a => a.SignInWithBrowserAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateUserCredentialMock());
            adapter
                .Setup(a => a.QueryUserInfoAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfo()
                {
                    Email = "alice@example.com"
                });

            var authorization = await Authorization.CreateAuthorizationAsync(
                    adapter.Object,
                    new Mock<IDeviceEnrollment>().Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authorization);
            Assert.AreEqual("alice@example.com", authorization.Email);
            Assert.AreEqual("alice@example.com", authorization.UserInfo.Email);

            adapter.Verify(a => a.SignInWithBrowserAsync(
                    It.Is<string>(hint => hint == null),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

            await authorization.ReauthorizeAsync(CancellationToken.None);

            adapter.Verify(a => a.SignInWithBrowserAsync(
                    It.Is<string>(hint => hint == "alice@example.com"),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
        }
    }
}
