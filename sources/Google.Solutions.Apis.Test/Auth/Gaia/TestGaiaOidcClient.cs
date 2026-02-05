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
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Auth.Gaia
{
    [TestFixture]
    public class TestGaiaOidcClient
    {
        private static readonly OidcClientRegistration SampleRegistration
            = new OidcClientRegistration(
                OidcIssuer.Gaia,
                "client-id",
                "client-secret",
                "/authorize/");

        private static readonly UnverifiedGaiaJsonWebToken SampleIdToken
            = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    Email = "test@example.com"
                });

        private static Mock<IDeviceEnrollment> CreateDisabledEnrollment()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Disabled);
            return enrollment;
        }

        private class OfflineStore : IOidcOfflineCredentialStore
        {
            public OidcOfflineCredential? StoredCredential { get; set; }

            public void Clear()
            {
                this.StoredCredential = null;
            }

            public bool TryRead(out OidcOfflineCredential? credential)
            {
                credential = this.StoredCredential;
                return credential != null;
            }

            public void Write(OidcOfflineCredential credential)
            {
                this.StoredCredential = credential;
            }
        }

        //---------------------------------------------------------------------
        // CreateSession.
        //---------------------------------------------------------------------

        [Test]
        public void CreateSession_WhenTokenResponseContainsIdToken_ThenUsesFreshIdToken()
        {
            var freshIdToken = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    JwtId = "fresh",
                    Email = "x@example.com"
                }).ToString();

            var tokenResponse = new TokenResponse()
            {
                AccessToken = "new-at",
                RefreshToken = "new-rt",
                IdToken = freshIdToken,
                Scope = $"{Scopes.Cloud} {Scopes.Email}"
            };

            var session = GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                null,
                tokenResponse);

            Assert.That(((UserCredential)session.ApiCredential).Token.IdToken, Is.EqualTo(freshIdToken));
            Assert.That(session.OfflineCredential.IdToken, Is.EqualTo(freshIdToken));
            Assert.That(session.IdToken.ToString(), Is.EqualTo(freshIdToken));

            Assert.That(((UserCredential)session.ApiCredential).Token.RefreshToken, Is.EqualTo("new-rt"));
            Assert.That(session.OfflineCredential.RefreshToken, Is.EqualTo("new-rt"));
        }

        [Test]
        public void CreateSession_WhenTokenResponseContainsIdTokenAndOfflineCredentialContainsOldIdToken_ThenUsesFreshIdToken()
        {
            var freshIdToken = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    JwtId = "fresh",
                    Email = "x@example.com"
                }).ToString();
            var oldIdToken = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    JwtId = "old",
                    Email = "x@example.com"
                }).ToString();

            var tokenResponse = new TokenResponse()
            {
                AccessToken = "new-at",
                RefreshToken = "new-rt",
                IdToken = freshIdToken,
                Scope = $"{Scopes.Cloud} {Scopes.Email}"
            };

            var offlineCredential = new OidcOfflineCredential(
                OidcIssuer.Gaia,
                "openid",
                "old-rt",
                oldIdToken);

            var session = GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                offlineCredential,
                tokenResponse);

            Assert.That(((UserCredential)session.ApiCredential).Token.IdToken, Is.EqualTo(freshIdToken));
            Assert.That(session.OfflineCredential.IdToken, Is.EqualTo(freshIdToken));
            Assert.That(session.IdToken.ToString(), Is.EqualTo(freshIdToken));

            Assert.That(((UserCredential)session.ApiCredential).Token.RefreshToken, Is.EqualTo("new-rt"));
            Assert.That(session.OfflineCredential.RefreshToken, Is.EqualTo("new-rt"));
        }

        [Test]
        public void CreateSession_WhenTokenResponseLacksIdTokenButOfflineCredentialContainsOldIdToken_ThenUsesOldIdToken()
        {
            var oldIdToken = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    JwtId = "old",
                    Email = "x@example.com"
                }).ToString();

            var tokenResponse = new TokenResponse()
            {
                AccessToken = "new-at",
                RefreshToken = "new-rt",
                IdToken = null,
                Scope = Scopes.Cloud
            };

            var offlineCredential = new OidcOfflineCredential(
                OidcIssuer.Gaia,
                "openid",
                "old-rt",
                oldIdToken);

            var session = GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                offlineCredential,
                tokenResponse);

            Assert.IsNull(((UserCredential)session.ApiCredential).Token.IdToken);
            Assert.That(session.OfflineCredential.IdToken, Is.EqualTo(oldIdToken));
            Assert.That(session.IdToken.ToString(), Is.EqualTo(oldIdToken));

            Assert.That(((UserCredential)session.ApiCredential).Token.RefreshToken, Is.EqualTo("new-rt"));
            Assert.That(session.OfflineCredential.RefreshToken, Is.EqualTo("new-rt"));
        }

        [Test]
        public void CreateSession_WhenTokenResponseLacksIdTokenAndOfflineCredentialIsNull_ThenThrowsException()
        {
            var oldIdToken = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    JwtId = "old",
                    Email = null
                }).ToString();

            var tokenResponse = new TokenResponse()
            {
                AccessToken = "new-at",
                RefreshToken = "new-rt",
                IdToken = null,
                Scope = Scopes.Cloud
            };

            Assert.Throws<OAuthScopeNotGrantedException>
                (() => GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                null,
                tokenResponse));
        }

        [Test]
        public void CreateSession_WhenTokenResponseLacksIdTokenAndOfflineCredentialContainsUnusableIdToken_ThenThrowsException()
        {
            var oldIdToken = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    JwtId = "old",
                    Email = null
                }).ToString();

            var tokenResponse = new TokenResponse()
            {
                AccessToken = "new-at",
                RefreshToken = "new-rt",
                IdToken = null,
                Scope = Scopes.Cloud
            };

            var offlineCredential = new OidcOfflineCredential(
                OidcIssuer.Gaia,
                "openid",
                "old-rt",
                oldIdToken);

            Assert.Throws<OAuthScopeNotGrantedException>
                (() => GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                offlineCredential,
                tokenResponse));
        }

        [Test]
        public void CreateSession_WhenTokenResponseLacksIdTokenAndOfflineCredentialLacksIdToken_ThenThrowsException()
        {
            var tokenResponse = new TokenResponse()
            {
                AccessToken = "new-at",
                RefreshToken = "new-rt",
                IdToken = null,
                Scope = Scopes.Cloud
            };

            var offlineCredential = new OidcOfflineCredential(
                OidcIssuer.Gaia,
                "openid",
                "old-rt",
                string.Empty);

            Assert.Throws<OAuthScopeNotGrantedException>
                (() => GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                offlineCredential,
                tokenResponse));
        }

        //---------------------------------------------------------------------
        // AuthorizeWithBrowser.
        //---------------------------------------------------------------------

        private class FailingCodeReceiver : ICodeReceiver
        {
            public AuthorizationCodeRequestUrl? RequestUrl;

            public string RedirectUri => "http://localhost/";

            public Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(
                AuthorizationCodeRequestUrl url,
                CancellationToken taskCancellationToken)
            {
                this.RequestUrl = url;

                return Task.FromResult(new AuthorizationCodeResponseUrl()
                {
                    Error = "mock"
                });
            }
        }

        private class GaiaOidcClientWithMockFlow : GaiaOidcClient
        {
            public Mock<IAuthorizationCodeFlow> Flow = new Mock<IAuthorizationCodeFlow>();

            public GaiaOidcClientWithMockFlow(
                IDeviceEnrollment deviceEnrollment,
                IOidcOfflineCredentialStore store,
                OidcClientRegistration registration)
                : base(
                      GaiaOidcClient.CreateEndpoint(),
                      deviceEnrollment,
                      store,
                      registration,
                      TestProject.UserAgent)
            {
            }

            protected override IAuthorizationCodeFlow CreateFlow(
                GoogleAuthorizationCodeFlow.Initializer initializer)
            {
                return this.Flow.Object;
            }
        }

        [Test]
        public async Task AuthorizeWithBrowser_WhenOfflineCredentialPresent_ThenUsesMinimalFlow()
        {
            var oldIdToken = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    JwtId = "old",
                    Email = "x@example.com"
                }).ToString();

            // Non-empty store.
            var store = new OfflineStore()
            {
                StoredCredential = new OidcOfflineCredential(
                    OidcIssuer.Gaia, "openid", "rt", oldIdToken)
            };

            // Trigger a request, but let it fail.
            var codeReceiver = new FailingCodeReceiver();
            var client = new GaiaOidcClient(
                GaiaOidcClient.CreateEndpoint(),
                CreateDisabledEnrollment().Object,
                store,
                SampleRegistration,
                TestProject.UserAgent);

            await ExceptionAssert.ThrowsAsync<TokenResponseException>(
                () => client.AuthorizeAsync(codeReceiver, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.IsNotNull(codeReceiver.RequestUrl);
            Assert.That(
                codeReceiver.RequestUrl!.Scope, Is.EqualTo(Scopes.Cloud),
                "Minimal flow");
            StringAssert.Contains(
                "login_hint=x%40example.com",
                codeReceiver.RequestUrl.Build().ToString());
        }

        [Test]
        public async Task AuthorizeWithBrowser_WhenOfflineCredentialLacksEmail_ThenUsesMinimalFlow()
        {
            var oldIdToken = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    JwtId = "old",
                    Email = string.Empty // unusable
                }).ToString();

            // Non-empty store.
            var store = new OfflineStore()
            {
                StoredCredential = new OidcOfflineCredential(
                    OidcIssuer.Gaia, "openid", "rt", oldIdToken)
            };

            // Trigger a request, but let it fail.
            var codeReceiver = new FailingCodeReceiver();
            var client = new GaiaOidcClient(
                GaiaOidcClient.CreateEndpoint(),
                CreateDisabledEnrollment().Object,
                store,
                SampleRegistration,
                TestProject.UserAgent);

            await ExceptionAssert.ThrowsAsync<TokenResponseException>(
                () => client.AuthorizeAsync(codeReceiver, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.IsNotNull(codeReceiver.RequestUrl);
            Assert.That(
                codeReceiver.RequestUrl!.Scope, Is.EqualTo($"{Scopes.Cloud} {Scopes.Email}"),
                "Normal flow");
        }

        [Test]
        public async Task AuthorizeWithBrowser_WhenOfflineCredentialIsInvalid_ThenUsesFullFlow()
        {
            // Non-empty store.
            var store = new OfflineStore()
            {
                StoredCredential = new OidcOfflineCredential(
                    OidcIssuer.Gaia, "openid", "rt", "junk")
            };

            // Trigger a request, but let it fail.
            var codeReceiver = new FailingCodeReceiver();
            var client = new GaiaOidcClient(
                GaiaOidcClient.CreateEndpoint(),
                CreateDisabledEnrollment().Object,
                store,
                SampleRegistration,
                TestProject.UserAgent);

            await ExceptionAssert.ThrowsAsync<TokenResponseException>(
                () => client.AuthorizeAsync(codeReceiver, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.IsNotNull(codeReceiver.RequestUrl);
            Assert.That(
                codeReceiver.RequestUrl!.Scope, Is.EqualTo($"{Scopes.Cloud} {Scopes.Email}"),
                "Normal flow");
        }

        [Test]
        public async Task AuthorizeWithBrowser_WhenNoOfflineCredentialFound_ThenUsesFullFlow()
        {
            // Empty store.
            var store = new OfflineStore();

            // Trigger a request, but let it fail.
            var codeReceiver = new FailingCodeReceiver();
            var client = new GaiaOidcClient(
                GaiaOidcClient.CreateEndpoint(),
                CreateDisabledEnrollment().Object,
                store,
                SampleRegistration,
                TestProject.UserAgent);

            await ExceptionAssert.ThrowsAsync<TokenResponseException>(
                () => client.AuthorizeAsync(codeReceiver, CancellationToken.None))
                .ConfigureAwait(false);

            Assert.IsNotNull(codeReceiver.RequestUrl);
            Assert.That(
                codeReceiver.RequestUrl!.Scope, Is.EqualTo($"{Scopes.Cloud} {Scopes.Email}"),
                "Normal flow");
        }

        [Test]
        public async Task AuthorizeWithBrowser_WhenBrowserFlowFails_ThenThrowsException()
        {
            var store = new OfflineStore();
            var client = new GaiaOidcClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleRegistration);

            var codeReceiver = new Mock<ICodeReceiver>();
            codeReceiver
                .Setup(r => r.ReceiveCodeAsync(
                    It.IsAny<AuthorizationCodeRequestUrl>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthorizationCodeResponseUrl()
                {
                    Error = "invalid_grant"
                });

            var e = await ExceptionAssert
                .ThrowsAsync<TokenResponseException>(
                    () => client.AuthorizeAsync(codeReceiver.Object, CancellationToken.None))
                .ConfigureAwait(false);
            StringAssert.Contains("invalid_grant", e.Message);
        }

        [Test]
        public async Task AuthorizeWithBrowser_WhenTokenExchangeFails_ThenThrowsException()
        {
            var store = new OfflineStore();
            var client = new GaiaOidcClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleRegistration);

            var codeReceiver = new Mock<ICodeReceiver>();
            codeReceiver
                .Setup(r => r.ReceiveCodeAsync(
                    It.IsAny<AuthorizationCodeRequestUrl>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthorizationCodeResponseUrl()
                {
                    Code = "code"
                });
            client.Flow
                .Setup(f => f.ExchangeCodeForTokenAsync(
                    null,
                    "code",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TokenResponseException(new TokenErrorResponse()
                {
                    Error = "invalid_grant"
                }));

            var e = await ExceptionAssert
                .ThrowsAsync<TokenResponseException>(
                    () => client.AuthorizeAsync(codeReceiver.Object, CancellationToken.None))
                .ConfigureAwait(false);
            StringAssert.Contains("invalid_grant", e.Message);
        }

        [Test]
        public async Task AuthorizeWithBrowser_WhenScopeNotGranted_ThenReturnsSession()
        {
            var store = new OfflineStore();
            var client = new GaiaOidcClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleRegistration);

            var codeReceiver = new Mock<ICodeReceiver>();
            codeReceiver
                .Setup(r => r.ReceiveCodeAsync(
                    It.IsAny<AuthorizationCodeRequestUrl>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthorizationCodeResponseUrl()
                {
                    Code = "code"
                });
            client.Flow
                .Setup(f => f.ExchangeCodeForTokenAsync(
                    null,
                    "code",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TokenResponse()
                {
                    AccessToken = "access-token",
                    RefreshToken = "refresh-token",
                    IdToken = "id-token",
                    Scope = Scopes.Cloud, // missing email scope
                    ExpiresInSeconds = 3600,
                    IssuedUtc = DateTime.UtcNow
                });

            var e = await ExceptionAssert
                .ThrowsAsync<OAuthScopeNotGrantedException>(
                    () => client.AuthorizeAsync(codeReceiver.Object, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AuthorizeWithBrowser_WhenTokenExchangeSucceeds_ThenReturnsSession()
        {
            var store = new OfflineStore();
            var client = new GaiaOidcClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleRegistration);

            var codeReceiver = new Mock<ICodeReceiver>();
            codeReceiver
                .Setup(r => r.ReceiveCodeAsync(
                    It.IsAny<AuthorizationCodeRequestUrl>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthorizationCodeResponseUrl()
                {
                    Code = "code"
                });
            client.Flow
                .Setup(f => f.ExchangeCodeForTokenAsync(
                    null,
                    "code",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TokenResponse()
                {
                    AccessToken = "access-token",
                    RefreshToken = "refresh-token",
                    IdToken = SampleIdToken.ToString(),
                    Scope = $"{Scopes.Cloud} {Scopes.Email} some more junk",
                    ExpiresInSeconds = 3600,
                    IssuedUtc = DateTime.UtcNow
                });

            var session = await client
                .AuthorizeAsync(codeReceiver.Object, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(session);
            Assert.That(session.Username, Is.EqualTo(SampleIdToken.Payload.Email));
            Assert.That(((UserCredential)session.ApiCredential).Token.AccessToken, Is.EqualTo("access-token"));
            Assert.That(((UserCredential)session.ApiCredential).Token.RefreshToken, Is.EqualTo("refresh-token"));
        }

        //---------------------------------------------------------------------
        // TryAuthorizeSilently.
        //---------------------------------------------------------------------

        [Test]
        public async Task TryAuthorizeSilently_WhenTokenExchangeFails_ThenReturnsNull()
        {
            var store = new OfflineStore()
            {
                StoredCredential = new OidcOfflineCredential(
                    OidcIssuer.Gaia,
                    "scope",
                    "refresh-token",
                    null)
            };

            var client = new GaiaOidcClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleRegistration);

            client.Flow
                .Setup(f => f.RefreshTokenAsync(
                    null,
                    "refresh-token",
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TokenResponseException(new TokenErrorResponse()
                {
                    Error = "invalid_grant"
                }));

            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(session);
        }

        [Test]
        public async Task TryAuthorizeSilently_WhenTokenExchangeSucceeds_ThenReturnsSession()
        {
            var store = new OfflineStore()
            {
                StoredCredential = new OidcOfflineCredential(
                    OidcIssuer.Gaia,
                    "scope",
                    "refresh-token",
                    null)
            };

            var client = new GaiaOidcClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleRegistration);

            client.Flow
                .Setup(f => f.RefreshTokenAsync(
                    null,
                    "refresh-token",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TokenResponse()
                {
                    AccessToken = "access-token",
                    RefreshToken = "refresh-token",
                    IdToken = SampleIdToken.ToString(),
                    Scope = $"{Scopes.Cloud} {Scopes.Email}",
                    ExpiresInSeconds = 3600,
                    IssuedUtc = DateTime.UtcNow
                });

            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(session);
            Assert.That(session!.Username, Is.EqualTo(SampleIdToken.Payload.Email));
            Assert.That(((UserCredential)session.ApiCredential).Token.AccessToken, Is.EqualTo("access-token"));
            Assert.That(((UserCredential)session.ApiCredential).Token.RefreshToken, Is.EqualTo("refresh-token"));

            // Terminate session.
            Assert.IsNotNull(store.StoredCredential);
            session.Terminate();
            Assert.IsNull(store.StoredCredential);
        }
    }
}
