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
using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows.Auth;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Apis.Platform;
using Google.Solutions.Testing.Application;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Auth
{
    [Apartment(ApartmentState.STA)]
    [TestFixture]
    public class TestAuthorizeViewModel : ApplicationFixtureBase
    {
        private class AuthorizeViewModelWithMockSigninAdapter : AuthorizeViewModel
        {
            public Mock<IOidcClient> Client = new Mock<IOidcClient>();

            public AuthorizeViewModelWithMockSigninAdapter(
                IInstall install,
                IOidcOfflineCredentialStore offlineStore,
                IRepository<IAccessSettings> settings)
                : base(
                    GaiaOidcClient.CreateEndpoint(),
                    WorkforcePoolClient.CreateEndpoint(),
                    install,
                    offlineStore,
                    settings,
                    new HelpClient(),
                    TestProject.UserAgent)
            {
                this.Client
                    .SetupGet(c => c.Registration)
                    .Returns(new OidcClientRegistration(OidcIssuer.Gaia, "client-id", "", "/"));
            }

            public AuthorizeViewModelWithMockSigninAdapter(
                IInstall install,
                IOidcOfflineCredentialStore offlineStore)
                : this(
                      install,
                      offlineStore,
                      new Mock<IRepository<IAccessSettings>>().Object)
            {
            }

            public AuthorizeViewModelWithMockSigninAdapter()
                : this(
                      new Mock<IInstall>().Object,
                      new Mock<IOidcOfflineCredentialStore>().Object)
            {

            }

            private protected override Application.Profile.Auth.Authorization CreateAuthorization()
            {
                return new Application.Profile.Auth.Authorization(
                    this.Client.Object,
                    new Mock<IDeviceEnrollment>().Object);
            }
        }

        //---------------------------------------------------------------------
        // WindowTitle.
        //---------------------------------------------------------------------

        [Test]
        public void WindowTitle()
        {
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter())
            {
                Assert.That(viewModel.WindowTitle.Value, Does.StartWith("Sign in - "));
            }
        }

        //---------------------------------------------------------------------
        // Version.
        //---------------------------------------------------------------------

        [Test]
        public void Version()
        {
            var install = new Mock<IInstall>();
            install.SetupGet(i => i.CurrentVersion).Returns(new Version(1, 2, 3, 4));
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter(
                install.Object,
                new Mock<IOidcOfflineCredentialStore>().Object))
            {
                Assert.That(viewModel.Version.Value, Does.StartWith("Version 1.2.3.4"));
            }
        }

        //---------------------------------------------------------------------
        // TryLoadExistingAuthorization.
        //---------------------------------------------------------------------

        [Test]
        public async Task TryLoadExistingAuthorization_WhenNoExistingAuthorizationFound()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view
            })
            {
                viewModel.Client
                    .Setup(a => a.TryAuthorizeSilentlyAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((IOidcSession?)null);

                await viewModel.TryLoadExistingAuthorizationCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.That(viewModel.Authorization, Is.Null);

                Assert.That(viewModel.IsSignOnControlVisible.Value, Is.True);
                Assert.That(viewModel.IsWaitControlVisible.Value, Is.False);
                Assert.That(viewModel.IsAuthorizationComplete.Value, Is.False);
            }
        }

        [Test]
        public async Task TryLoadExistingAuthorization_WhenExistingAuthorizationFails()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view
            })
            {
                viewModel.Client
                    .Setup(a => a.TryAuthorizeSilentlyAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("mock"));

                await viewModel.TryLoadExistingAuthorizationCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.That(viewModel.Authorization, Is.Null);

                Assert.That(viewModel.IsSignOnControlVisible.Value, Is.True);
                Assert.That(viewModel.IsWaitControlVisible.Value, Is.False);
                Assert.That(viewModel.IsAuthorizationComplete.Value, Is.False);
            }
        }

        [Test]
        public async Task TryLoadExistingAuthorization_WhenExistingAuthorizationFound()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view
            })
            {
                viewModel.Client
                    .Setup(a => a.TryAuthorizeSilentlyAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Mock<IOidcSession>().Object);

                await viewModel.TryLoadExistingAuthorizationCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.That(viewModel.Authorization, Is.Not.Null);

                Assert.That(viewModel.IsSignOnControlVisible.Value, Is.False);
                Assert.That(viewModel.IsWaitControlVisible.Value, Is.True);
                Assert.That(viewModel.IsAuthorizationComplete.Value, Is.True);
            }
        }

        //---------------------------------------------------------------------
        // SignInWithDefaultBrowserCommand.
        //---------------------------------------------------------------------

        [Test]
        public void SignInWithDefaultBrowserCommand_WhenSignInCancelled()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view
            })
            {
                viewModel.Client
                    .Setup(a => a.AuthorizeAsync(
                        It.IsAny<ICodeReceiver>(),
                        It.IsAny<CancellationToken>()))
                    .Throws(new TaskCanceledException());

                ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                    () => viewModel.SignInWithDefaultBrowserCommand
                        .ExecuteAsync(CancellationToken.None)
                        .Wait());
            }
        }

        [Test]
        public async Task SignInWithDefaultBrowserCommand_WhenNetworkErrorAndRetryDenied()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view
            })
            {
                viewModel.Client
                    .Setup(a => a.AuthorizeAsync(
                        It.IsAny<ICodeReceiver>(),
                        It.IsAny<CancellationToken>()))
                    .Throws(new InvalidOperationException("mock"));

                viewModel.NetworkError += (_, args) =>
                {
                    Assert.That(args.Retry, Is.False);
                    args.Retry = false;
                };

                await viewModel.SignInWithDefaultBrowserCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.That(viewModel.Authorization, Is.Null);

                Assert.That(viewModel.IsSignOnControlVisible.Value, Is.True);
                Assert.That(viewModel.IsWaitControlVisible.Value, Is.False);
                Assert.That(viewModel.IsAuthorizationComplete.Value, Is.False);
            }
        }

        [Test]
        public async Task SignInWithDefaultBrowserCommand_WhenOAuthScopeNotGrantedAndRetryDenied()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view
            })
            {
                var tokenResponse = new TokenResponse()
                {
                    Scope = "email"
                };

                viewModel.Client
                    .Setup(a => a.AuthorizeAsync(
                        It.IsAny<ICodeReceiver>(),
                        It.IsAny<CancellationToken>()))
                    .Throws(new OAuthScopeNotGrantedException("mock"));

                viewModel.OAuthScopeNotGranted += (_, args) =>
                {
                    Assert.That(args.Retry, Is.False);
                    args.Retry = false;
                };

                await viewModel.SignInWithDefaultBrowserCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.That(viewModel.Authorization, Is.Null);

                Assert.That(viewModel.IsSignOnControlVisible.Value, Is.True);
                Assert.That(viewModel.IsWaitControlVisible.Value, Is.False);
                Assert.That(viewModel.IsAuthorizationComplete.Value, Is.False);
            }
        }

        [Test]
        public async Task SignInWithDefaultBrowserCommand_WhenInitialAuthorization()
        {
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view
            })
            {
                var tokenResponse = new TokenResponse()
                {
                    Scope = "email"
                };

                viewModel.Client
                    .Setup(a => a.AuthorizeAsync(
                        It.IsAny<ICodeReceiver>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Mock<IOidcSession>().Object);

                await viewModel.SignInWithDefaultBrowserCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.That(viewModel.Authorization, Is.Not.Null);

                Assert.That(viewModel.IsSignOnControlVisible.Value, Is.True);
                Assert.That(viewModel.IsWaitControlVisible.Value, Is.False);
                Assert.That(viewModel.IsAuthorizationComplete.Value, Is.True);
            }
        }

        [Test]
        public async Task SignInWithDefaultBrowserCommand_WhenReauthorizing()
        {
            IAuthorization authorization;
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view
            })
            {
                var tokenResponse = new TokenResponse()
                {
                    Scope = "email"
                };

                viewModel.Client
                    .Setup(a => a.AuthorizeAsync(
                        It.IsAny<ICodeReceiver>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Mock<IOidcSession>().Object);

                await viewModel.SignInWithDefaultBrowserCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                authorization = viewModel.Authorization!;
            }

            // Reauthorize.
            using (var view = new Form())
            using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter()
            {
                View = view
            })
            {
                viewModel.UseExistingAuthorization(authorization);
                var tokenResponse = new TokenResponse()
                {
                    Scope = "email"
                };

                viewModel.Client
                    .Setup(a => a.AuthorizeAsync(
                        It.IsAny<ICodeReceiver>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Mock<IOidcSession>().Object);

                await viewModel.SignInWithDefaultBrowserCommand
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(true);

                Assert.That(viewModel.Authorization, Is.SameAs(authorization));

                Assert.That(viewModel.IsSignOnControlVisible.Value, Is.True);
                Assert.That(viewModel.IsWaitControlVisible.Value, Is.False);
                Assert.That(viewModel.IsAuthorizationComplete.Value, Is.True);
            }
        }

        //---------------------------------------------------------------------
        // UseHttpSysCheckedCommand.
        //---------------------------------------------------------------------

        [Test]
        public async Task UseHttpSysCheckedCommand()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var settingsRepository = new AccessSettingsRepository(
                    settingsPath.CreateKey(),
                    null,
                    null);

                using (var view = new Form())
                using (var viewModel = new AuthorizeViewModelWithMockSigninAdapter(
                    new Mock<IInstall>().Object,
                    new Mock<IOidcOfflineCredentialStore>().Object,
                    settingsRepository)
                {
                    View = view
                })
                {
                    Assert.That(viewModel.IsUseHttpSysChecked.Value, Is.False);

                    // Toggle.
                    await viewModel.UseHttpSysCheckedCommand
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(true);

                    Assert.That(viewModel.IsUseHttpSysChecked.Value, Is.True);
                    Assert.That(
                        settingsRepository.GetSettings().UseHttpSysCodeReceiver.Value,
                        Is.True);

                    // Toggle back.
                    await viewModel.UseHttpSysCheckedCommand
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(true);

                    Assert.That(viewModel.IsUseHttpSysChecked.Value, Is.False);
                    Assert.That(
                        settingsRepository.GetSettings().UseHttpSysCodeReceiver.Value,
                        Is.False);
                }
            }
        }
    }
}
