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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Application.Views.Authorization;
using Google.Solutions.IapDesktop.Core.Auth;
using Google.Solutions.Platform.Net;
using Google.Solutions.Testing.Application.Test;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Authorization
{
    [Apartment(ApartmentState.STA)]
    [TestFixture]
    public class TestAuthorizeViewModel : ApplicationFixtureBase
    {
        private class AuthorizeViewModelWithMockSigninAdapter : AuthorizeViewModel
        {
            public Mock<ISignInAdapter> SignInAdapter = new Mock<ISignInAdapter>();

            public AuthorizeViewModelWithMockSigninAdapter(Mock<IInstall> install)
                : base(install.Object)
            {
            }

            public AuthorizeViewModelWithMockSigninAdapter()
                : this(new Mock<IInstall>())
            {
            }

            protected override ISignInAdapter CreateSignInAdapter(BrowserPreference preference)
            {
                return this.SignInAdapter.Object;
            }
        }
        private static UserCredential CreateCredential()
        {
            return new UserCredential(
                new Mock<IAuthorizationCodeFlow>().Object,
                "mock-user",
                new TokenResponse());
        }

        //---------------------------------------------------------------------
        // WindowTitle.
        //---------------------------------------------------------------------

        public void WindowTitle()
        {
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter())
            {
                StringAssert.StartsWith("Sign in - ", viewModel.WindowTitle.Value);
            }
        }

        //---------------------------------------------------------------------
        // Version.
        //---------------------------------------------------------------------

        public void Version()
        {
            var install = new Mock<IInstall>();
            install.SetupGet(i => i.CurrentVersion).Returns(new Version(1, 2, 3, 4));
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter())
            {
                StringAssert.StartsWith("Version 1.2.3.4", viewModel.Version.Value);
            }
        }

        //---------------------------------------------------------------------
        // TryLoadExistingAuthorization.
        //---------------------------------------------------------------------

        [Test]
        public async Task NoExistingAuthorizationFound()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view,
                TokenStore = new Mock<IDataStore>().Object,
                DeviceEnrollment = new Mock<IDeviceEnrollment>().Object
            })
            {
                viewModel.SignInAdapter
                    .Setup(a => a.TrySignInWithRefreshTokenAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((UserCredential)null);

                await viewModel.TryLoadExistingAuthorizationCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.IsNull(viewModel.Authorization.Value);

                Assert.IsTrue(viewModel.IsSignOnControlVisible.Value);
                Assert.IsFalse(viewModel.IsWaitControlVisible.Value);
            }
        }

        [Test]
        public async Task ExistingAuthorizationFails()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view,
                TokenStore = new Mock<IDataStore>().Object,
                DeviceEnrollment = new Mock<IDeviceEnrollment>().Object
            })
            {
                viewModel.SignInAdapter
                    .Setup(a => a.TrySignInWithRefreshTokenAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("mock"));

                await viewModel.TryLoadExistingAuthorizationCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.IsNull(viewModel.Authorization.Value);

                Assert.IsTrue(viewModel.IsSignOnControlVisible.Value);
                Assert.IsFalse(viewModel.IsWaitControlVisible.Value);
            }
        }

        [Test]
        public async Task ExistingAuthorizationFound()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view,
                TokenStore = new Mock<IDataStore>().Object,
                DeviceEnrollment = new Mock<IDeviceEnrollment>().Object
            })
            {
                viewModel.SignInAdapter
                    .Setup(a => a.TrySignInWithRefreshTokenAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateCredential());
                viewModel.SignInAdapter
                    .Setup(a => a.QueryUserInfoAsync(
                        It.IsAny<ICredential>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new UserInfo());

                await viewModel.TryLoadExistingAuthorizationCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.IsNotNull(viewModel.Authorization.Value);

                Assert.IsFalse(viewModel.IsSignOnControlVisible.Value);
                Assert.IsTrue(viewModel.IsWaitControlVisible.Value);
            }
        }

        //---------------------------------------------------------------------
        // SignInAsync.
        //---------------------------------------------------------------------

        [Test]
        public void SignInCancelled()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view,
                TokenStore = new Mock<IDataStore>().Object,
                DeviceEnrollment = new Mock<IDeviceEnrollment>().Object
            })
            {
                viewModel.SignInAdapter
                    .Setup(a => a.SignInWithBrowserAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .Throws(new TaskCanceledException());

                ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                    () => viewModel.SignInWithDefaultBrowserCommand
                        .ExecuteAsync(CancellationToken.None)
                        .Wait());
            }
        }

        [Test]
        public async Task NetworkErrorAndRetryDenied()
        {
            var tokenStore = new Mock<IDataStore>();
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view,
                TokenStore = tokenStore.Object,
                DeviceEnrollment = new Mock<IDeviceEnrollment>().Object
            })
            {
                viewModel.SignInAdapter
                    .Setup(a => a.SignInWithBrowserAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .Throws(new InvalidOperationException("mock"));

                viewModel.NetworkError += (_, args) =>
                {
                    Assert.IsFalse(args.Retry);
                    args.Retry = false;
                };

                await viewModel.SignInWithDefaultBrowserCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                tokenStore.Verify(s => s.ClearAsync(), Times.Once);

                Assert.IsNull(viewModel.Authorization.Value);

                Assert.IsTrue(viewModel.IsSignOnControlVisible.Value);
                Assert.IsFalse(viewModel.IsWaitControlVisible.Value);
            }
        }

        [Test]
        public async Task OAuthScopeNotGrantedAndRetryDenied()
        {
            var tokenStore = new Mock<IDataStore>();
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view,
                TokenStore = tokenStore.Object,
                DeviceEnrollment = new Mock<IDeviceEnrollment>().Object
            })
            {
                var tokenResponse = new TokenResponse()
                {
                    Scope = "email"
                };

                viewModel.SignInAdapter
                    .Setup(a => a.SignInWithBrowserAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .Throws(new OAuthScopeNotGrantedException("mock"));
                viewModel.SignInAdapter
                    .Setup(a => a.QueryUserInfoAsync(
                        It.IsAny<ICredential>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new UserInfo()
                    {
                        Email = "mock@example.com"
                    });

                viewModel.OAuthScopeNotGranted += (_, args) =>
                {
                    Assert.IsFalse(args.Retry);
                    args.Retry = false;
                };

                await viewModel.SignInWithDefaultBrowserCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                tokenStore.Verify(s => s.ClearAsync(), Times.Once);

                Assert.IsNull(viewModel.Authorization.Value);

                Assert.IsTrue(viewModel.IsSignOnControlVisible.Value);
                Assert.IsFalse(viewModel.IsWaitControlVisible.Value);
            }
        }

        [Test]
        public async Task SignInSuccessful()
        {
            var tokenStore = new Mock<IDataStore>();
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view,
                TokenStore = tokenStore.Object,
                DeviceEnrollment = new Mock<IDeviceEnrollment>().Object
            })
            {
                var tokenResponse = new TokenResponse()
                {
                    Scope = "email"
                };

                viewModel.SignInAdapter
                    .Setup(a => a.SignInWithBrowserAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateCredential());
                viewModel.SignInAdapter
                    .Setup(a => a.QueryUserInfoAsync(
                        It.IsAny<ICredential>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new UserInfo()
                    {
                        Email = "mock@example.com"
                    });

                await viewModel.SignInWithDefaultBrowserCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.IsNotNull(viewModel.Authorization.Value);

                Assert.IsTrue(viewModel.IsSignOnControlVisible.Value);
                Assert.IsFalse(viewModel.IsWaitControlVisible.Value);
            }
        }
    }
}
