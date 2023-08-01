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
using Google.Solutions.Apis.Client;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Auth.Gaia
{
    [TestFixture]
    public class TestGaiaOidcClient
    {
        //---------------------------------------------------------------------
        // CreateSession.
        //---------------------------------------------------------------------
        
        [Test]
        public void WhenTokenResponseContainsIdToken_ThenCreateSessionUsesFreshIdToken()
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
                Scope = $"{GaiaOidcClient.Scopes.Cloud} {GaiaOidcClient.Scopes.Email}"
            };

            var session = GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                null,
                tokenResponse);

            Assert.AreEqual(freshIdToken, ((UserCredential)session.ApiCredential).Token.IdToken);
            Assert.AreEqual(freshIdToken, session.OfflineCredential.IdToken);
            Assert.AreEqual(freshIdToken, session.IdToken.ToString());

            Assert.AreEqual("new-rt", ((UserCredential)session.ApiCredential).Token.RefreshToken);
            Assert.AreEqual("new-rt", session.OfflineCredential.RefreshToken);
        }

        [Test]
        public void WhenTokenResponseContainsIdTokenAndOfflineCredentialContainsOldIdToken_ThenCreateSessionUsesFreshIdToken()
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
                Scope = $"{GaiaOidcClient.Scopes.Cloud} {GaiaOidcClient.Scopes.Email}"
            };

            var offlineCredential = new OidcOfflineCredential(
                OidcOfflineCredentialIssuer.Gaia,
                "old-rt",
                oldIdToken);

            var session = GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                offlineCredential,
                tokenResponse);

            Assert.AreEqual(freshIdToken, ((UserCredential)session.ApiCredential).Token.IdToken);
            Assert.AreEqual(freshIdToken, session.OfflineCredential.IdToken);
            Assert.AreEqual(freshIdToken, session.IdToken.ToString());

            Assert.AreEqual("new-rt", ((UserCredential)session.ApiCredential).Token.RefreshToken);
            Assert.AreEqual("new-rt", session.OfflineCredential.RefreshToken);
        }

        [Test]
        public void WhenTokenResponseLacksIdTokenButOfflineCredentialContainsOldIdToken_ThenCreateSessionUsesOldIdToken()
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
                Scope = $"{GaiaOidcClient.Scopes.Cloud}"
            };

            var offlineCredential = new OidcOfflineCredential(
                OidcOfflineCredentialIssuer.Gaia,
                "old-rt",
                oldIdToken);

            var session = GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                offlineCredential,
                tokenResponse);

            Assert.IsNull(((UserCredential)session.ApiCredential).Token.IdToken);
            Assert.AreEqual(oldIdToken, session.OfflineCredential.IdToken);
            Assert.AreEqual(oldIdToken, session.IdToken.ToString());

            Assert.AreEqual("new-rt", ((UserCredential)session.ApiCredential).Token.RefreshToken);
            Assert.AreEqual("new-rt", session.OfflineCredential.RefreshToken);
        }

        [Test]
        public void WhenTokenResponseLacksIdTokenAndOfflineCredentialIsNull_ThenCreateSessionThrowsException()
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
                Scope = $"{GaiaOidcClient.Scopes.Cloud}"
            };

            Assert.Throws<OAuthScopeNotGrantedException>
                (() => GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                null,
                tokenResponse));
        }

        [Test]
        public void WhenTokenResponseLacksIdTokenAndOfflineCredentialContainsUnusableIdToken_ThenCreateSessionThrowsException()
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
                Scope = $"{GaiaOidcClient.Scopes.Cloud}"
            };

            var offlineCredential = new OidcOfflineCredential(
                OidcOfflineCredentialIssuer.Gaia,
                "old-rt",
                oldIdToken);

            Assert.Throws<OAuthScopeNotGrantedException>
                (() => GaiaOidcClient.CreateSession(
                new Mock<IAuthorizationCodeFlow>().Object,
                offlineCredential,
                tokenResponse));
        }

        [Test]
        public void WhenTokenResponseLacksIdTokenAndOfflineCredentialLacksIdToken_ThenCreateSessionThrowsException()
        {
            var tokenResponse = new TokenResponse()
            {
                AccessToken = "new-at",
                RefreshToken = "new-rt",
                IdToken = null,
                Scope = $"{GaiaOidcClient.Scopes.Cloud}"
            };

            var offlineCredential = new OidcOfflineCredential(
                OidcOfflineCredentialIssuer.Gaia,
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
            public AuthorizationCodeRequestUrl RequestUrl;

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

        private class GoogleOidcClientWithMockFlow : GaiaOidcClient
        {
            public Mock<IAuthorizationCodeFlow> Flow = new Mock<IAuthorizationCodeFlow>();

            public GoogleOidcClientWithMockFlow(
                ServiceEndpoint<GaiaOidcClient> endpoint,
                IDeviceEnrollment deviceEnrollment,
                IOidcOfflineCredentialStore store,
                ClientSecrets clientSecrets) 
                : base(endpoint, deviceEnrollment, store, clientSecrets)
            {
            }

            protected override IAuthorizationCodeFlow CreateFlow(
                GoogleAuthorizationCodeFlow.Initializer initializer)
            {
                return this.Flow.Object;
            }
        }

        [Test]
        public void WhenOfflineCredentialPresent_ThenAuthorizeWithBrowserUsesMinimalFlow()
        {
            // Not enrolled.
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            var oldIdToken = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    JwtId = "old",
                    Email = "x@example.com"
                }).ToString();

            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential(
                OidcOfflineCredentialIssuer.Gaia, "rt", oldIdToken);
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            // Trigger a request, but let it fail.
            var codeReceiver = new FailingCodeReceiver();
            var client = new GaiaOidcClient(
                GaiaOidcClient.CreateEndpoint(),
                enrollment.Object,
                store.Object,
                new ClientSecrets());

            ExceptionAssert.ThrowsAggregateException<TokenResponseException>(
                () => client.AuthorizeAsync(codeReceiver, CancellationToken.None).Wait());

            Assert.AreEqual(
                GaiaOidcClient.Scopes.Cloud,
                codeReceiver.RequestUrl.Scope,
                "Minimal flow");
            StringAssert.Contains(
                "login_hint=x%40example.com",
                codeReceiver.RequestUrl.Build().ToString());
        }

        [Test]
        public void WhenOfflineCredentialLacksEmail_ThenAuthorizeWithBrowserUsesMinimalFlow()
        {
            // Not enrolled.
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            var oldIdToken = new UnverifiedGaiaJsonWebToken(
                new Google.Apis.Auth.GoogleJsonWebSignature.Header(),
                new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    JwtId = "old",
                    Email = string.Empty // unusable
                }).ToString();

            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential(
                OidcOfflineCredentialIssuer.Gaia, "rt", oldIdToken);
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            // Trigger a request, but let it fail.
            var codeReceiver = new FailingCodeReceiver();
            var client = new GaiaOidcClient(
                GaiaOidcClient.CreateEndpoint(),
                enrollment.Object,
                store.Object,
                new ClientSecrets());

            ExceptionAssert.ThrowsAggregateException<TokenResponseException>(
                () => client.AuthorizeAsync(codeReceiver, CancellationToken.None).Wait());

            Assert.AreEqual(
                $"{GaiaOidcClient.Scopes.Cloud} {GaiaOidcClient.Scopes.Email}",
                codeReceiver.RequestUrl.Scope,
                "Normal flow");
        }

        [Test]
        public void WhenOfflineCredentialIsInvalid_ThenAuthorizeWithBrowserUsesFullFlow()
        {
            // Not enrolled.
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential(
                OidcOfflineCredentialIssuer.Gaia, "rt", "junk");
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            // Trigger a request, but let it fail.
            var codeReceiver = new FailingCodeReceiver();
            var client = new GaiaOidcClient(
                GaiaOidcClient.CreateEndpoint(),
                enrollment.Object,
                store.Object,
                new ClientSecrets());

            ExceptionAssert.ThrowsAggregateException<TokenResponseException>(
                () => client.AuthorizeAsync(codeReceiver, CancellationToken.None).Wait());

            Assert.AreEqual(
                $"{GaiaOidcClient.Scopes.Cloud} {GaiaOidcClient.Scopes.Email}",
                codeReceiver.RequestUrl.Scope,
                "Normal flow");
        }

        [Test]
        public void WhenNoOfflineCredentialFound_ThenAuthorizeWithBrowserUsesFullFlow()
        {
            // Not enrolled.
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            // Empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            OidcOfflineCredential offlineCredential = null;
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(false);

            // Trigger a request, but let it fail.
            var codeReceiver = new FailingCodeReceiver();
            var client = new GaiaOidcClient(
                GaiaOidcClient.CreateEndpoint(),
                enrollment.Object,
                store.Object,
                new ClientSecrets());

            ExceptionAssert.ThrowsAggregateException<TokenResponseException>(
                () => client.AuthorizeAsync(codeReceiver, CancellationToken.None).Wait());

            Assert.AreEqual(
                $"{GaiaOidcClient.Scopes.Cloud} {GaiaOidcClient.Scopes.Email}",
                codeReceiver.RequestUrl.Scope,
                "Normal flow");
        }

        //---------------------------------------------------------------------
        // ActivateOfflineCredential.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenRefreshTokenInvalid_ThenActivateOfflineCredentialReturnsNull()
        {
            // Not enrolled.
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential(
                OidcOfflineCredentialIssuer.Gaia,
                "invalid-rt",
                null);
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            var client = new GaiaOidcClient(
                GaiaOidcClient.CreateEndpoint(),
                enrollment.Object,
                store.Object,
                new ClientSecrets());

            var session = await client
                .TryAuthorizeSilentlyAsync(
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(session);
        }

        //---------------------------------------------------------------------
        // CodeFlowInitializer.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotEnrolled_ThenCreateOpenIdInitializerUsesTls(
            [Values(
                DeviceEnrollmentState.NotEnrolled,
                DeviceEnrollmentState.Disabled)] DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);

            var initializer = new GaiaOidcClient.CodeFlowInitializer(
                GaiaOidcClient.CreateEndpoint(),
                enrollment.Object);

            Assert.AreEqual("https://accounts.google.com/o/oauth2/v2/auth", initializer.AuthorizationServerUrl);
            Assert.AreEqual("https://oauth2.googleapis.com/token", initializer.TokenServerUrl);
            Assert.AreEqual("https://oauth2.googleapis.com/revoke", initializer.RevokeTokenUrl);
        }

        [Test]
        public void WhenEnrolled_ThenCreateOpenIdInitializerUsesTlsUsesMtls()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Enrolled);

            var initializer = new GaiaOidcClient.CodeFlowInitializer(
                GaiaOidcClient.CreateEndpoint(),
                enrollment.Object);

            Assert.AreEqual("https://accounts.google.com/o/oauth2/v2/auth", initializer.AuthorizationServerUrl);
            Assert.AreEqual("https://oauth2.mtls.googleapis.com/token", initializer.TokenServerUrl);
            Assert.AreEqual("https://oauth2.mtls.googleapis.com/revoke", initializer.RevokeTokenUrl);
        }
    }
}
