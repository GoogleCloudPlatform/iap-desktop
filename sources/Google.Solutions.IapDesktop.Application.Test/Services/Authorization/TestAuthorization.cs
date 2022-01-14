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
using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Authorization
{
    [TestFixture]
    public class TestAuthorization : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // RevokeAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task RevokeAsyncDeletesRefreshToken()
        {
            var adapter = new Mock<ISignInAdapter>();

            var authorization = await Authorization.CreateAuthorizationAsync(
                    adapter.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            await authorization.RevokeAsync();

            adapter.Verify(a => a.DeleteStoredRefreshToken(), Times.Once);
        }

        //---------------------------------------------------------------------
        // TryAuthorizeUsingRefreshTokenAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenRefreshTokenValid_ThenTryLoadExistingAuthorizationAsyncReturnsObject()
        {
            var adapter = new Mock<ISignInAdapter>();
            adapter.Setup(a => a.TrySignInWithRefreshTokenAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<ICredential>().Object);
            adapter.Setup(a => a.QueryUserInfoAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfo()
                {
                    Email = "bob@example.com"
                });

            var authorization = await Authorization.TryLoadExistingAuthorizationAsync(
                    adapter.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authorization);
            Assert.AreEqual("bob@example.com", authorization.Email);
            Assert.AreEqual("bob@example.com", authorization.UserInfo.Email);
        }

        [Test]
        public async Task WhenUsingRefreshTokenFails_ThenTryLoadExistingAuthorizationAsyncReturnsNull()
        {
            var adapter = new Mock<ISignInAdapter>();
            adapter.Setup(a => a.TrySignInWithRefreshTokenAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ICredential)null);

            var authorization = await Authorization.TryLoadExistingAuthorizationAsync(
                    adapter.Object,
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
            adapter.Setup(a => a.SignInWithBrowserAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<ICredential>().Object);
            adapter.Setup(a => a.QueryUserInfoAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfo()
                {
                    Email = "bob@example.com"
                });

            var authorization = await Authorization.CreateAuthorizationAsync(
                    adapter.Object,
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
        public async Task WhenReauthorizeUsesDifferentUser_ThenReauthorizeAsyncSwapsCredential()
        {
            var adapter = new Mock<ISignInAdapter>();
            adapter.Setup(a => a.SignInWithBrowserAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<ICredential>().Object);
            adapter.Setup(a => a.QueryUserInfoAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfo()
                {
                    Email = "alice@example.com"
                });

            var authorization = await Authorization.CreateAuthorizationAsync(
                    adapter.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authorization);
            Assert.AreEqual("alice@example.com", authorization.Email);
            Assert.AreEqual("alice@example.com", authorization.UserInfo.Email);

            adapter.Setup(a => a.QueryUserInfoAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfo()
                {
                    Email = "bob@example.com"
                });

            await authorization.ReauthorizeAsync(CancellationToken.None);

            Assert.IsNotNull(authorization);
            Assert.AreEqual("bob@example.com", authorization.Email);
            Assert.AreEqual("bob@example.com", authorization.UserInfo.Email);
        }
    }
}
