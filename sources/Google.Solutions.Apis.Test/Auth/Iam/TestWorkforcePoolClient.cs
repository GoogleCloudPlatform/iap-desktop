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
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Auth.Iam
{
    [TestFixture]
    public class TestWorkforcePoolClient
    {
        private static readonly WorkforcePoolProviderLocator SampleProvider
            = new WorkforcePoolProviderLocator("global", "pool", "provider");

        private static readonly OidcClientRegistration SampleRegistration
            = new OidcClientRegistration(
                OidcIssuer.Sts,
                "client-id",
                "client-secret",
                "/");

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
        // Authorize.
        //---------------------------------------------------------------------

        private class WorkforcePoolClientWithMockFlow : WorkforcePoolClient
        {
            public Mock<IAuthorizationCodeFlow> Flow = new Mock<IAuthorizationCodeFlow>();
            public StsService.IntrospectTokenResponse? IntrospectTokenResponse = null;

            public WorkforcePoolClientWithMockFlow(
                IDeviceEnrollment deviceEnrollment,
                IOidcOfflineCredentialStore store,
                WorkforcePoolProviderLocator provider,
                OidcClientRegistration registration)
                : base(
                      WorkforcePoolClient.CreateEndpoint(),
                      deviceEnrollment,
                      store,
                      provider,
                      registration,
                      TestProject.UserAgent)
            {
            }

            protected override IAuthorizationCodeFlow CreateFlow()
            {
                return this.Flow.Object;
            }

            private protected override Task<StsService.IntrospectTokenResponse> IntrospectTokenAsync(
                StsService.IntrospectTokenRequest request, CancellationToken cancellationToken)
            {
                if (this.IntrospectTokenResponse == null)
                {
                    throw new InvalidOperationException();
                }

                return Task.FromResult(this.IntrospectTokenResponse);
            }
        }

        [Test]
        public async Task Authorize_WhenBrowserFlowFails_ThenThrowsException()
        {
            var store = new OfflineStore();
            var client = new WorkforcePoolClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleProvider,
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
        public async Task Authorize_WhenTokenExchangeFails_ThenThrowsException()
        {
            var store = new OfflineStore();
            var client = new WorkforcePoolClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleProvider,
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
        public async Task Authorize_WhenTokenExchangeSucceeds_ThenReturnsSession()
        {
            var store = new OfflineStore();
            var client = new WorkforcePoolClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleProvider,
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
                    ExpiresInSeconds = 3600,
                    IssuedUtc = DateTime.UtcNow
                });
            client.IntrospectTokenResponse = new StsService.IntrospectTokenResponse()
            {
                Active = true,
                ClientId = SampleRegistration.ClientId,
                Iss = "https://sts.googleapis.com/",
                Username = "principal://iam.googleapis.com/locations/LOCATION/workforcePools/POOL/subject/SUBJECT"
            };

            var session = await client
                .AuthorizeAsync(codeReceiver.Object, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(session);
            Assert.That(session.Username, Is.EqualTo("SUBJECT"));
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
                    OidcIssuer.Sts,
                    "scope",
                    "refresh-token",
                    null)
            };

            var client = new WorkforcePoolClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleProvider,
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
                    OidcIssuer.Sts,
                    "scope",
                    "refresh-token",
                    null)
            };

            var client = new WorkforcePoolClientWithMockFlow(
                CreateDisabledEnrollment().Object,
                store,
                SampleProvider,
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
                    ExpiresInSeconds = 3600,
                    IssuedUtc = DateTime.UtcNow
                });
            client.IntrospectTokenResponse = new StsService.IntrospectTokenResponse()
            {
                Active = true,
                ClientId = SampleRegistration.ClientId,
                Iss = "https://sts.googleapis.com/",
                Username = "principal://iam.googleapis.com/locations/LOCATION/workforcePools/POOL/subject/SUBJECT"
            };

            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(session);
            Assert.That(session!.Username, Is.EqualTo("SUBJECT"));
            Assert.That(((UserCredential)session.ApiCredential).Token.AccessToken, Is.EqualTo("access-token"));
            Assert.That(((UserCredential)session.ApiCredential).Token.RefreshToken, Is.EqualTo("refresh-token"));

            // Terminate session.
            Assert.IsNotNull(store.StoredCredential);
            session.Terminate();
            Assert.IsNull(store.StoredCredential);
        }
    }
}
