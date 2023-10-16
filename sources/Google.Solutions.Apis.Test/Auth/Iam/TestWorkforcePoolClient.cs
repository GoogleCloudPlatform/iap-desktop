﻿//
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
using Google.Solutions.Apis.Client;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Net.Sockets;
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
        // AuthorizeWithBrowser.
        //---------------------------------------------------------------------

        private class WorkforcePoolClientWithMockFlow : WorkforcePoolClient
        {
            public Mock<IAuthorizationCodeFlow> Flow = new Mock<IAuthorizationCodeFlow>();
            public StsService.IntrospectTokenResponse IntrospectTokenResponse = null;

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
                return Task.FromResult(this.IntrospectTokenResponse);
            }
        }

        [Test]
        public void WhenBrowserFlowFails_ThenAuthorizeWithBrowserThrowsException()
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

            ExceptionAssert.ThrowsAggregateException<TokenResponseException>(
                "invalid_grant",
                () => client
                    .AuthorizeAsync(codeReceiver.Object, CancellationToken.None)
                    .Wait());
        }

        [Test]
        public void WhenTokenExchangeFails_ThenAuthorizeWithBrowserThrowsException()
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

            ExceptionAssert.ThrowsAggregateException<TokenResponseException>(
                "invalid_grant",
                () => client
                    .AuthorizeAsync(codeReceiver.Object, CancellationToken.None)
                    .Wait());
        }

        [Test]
        public async Task WhenTokenExchangeSucceeds_ThenAuthorizeWithBrowserReturnsSession()
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
            Assert.AreEqual("SUBJECT", session.Username);
            Assert.AreEqual("access-token", ((UserCredential)session.ApiCredential).Token.AccessToken);
            Assert.AreEqual("refresh-token", ((UserCredential)session.ApiCredential).Token.RefreshToken);
        }

        //---------------------------------------------------------------------
        // TryAuthorizeSilently.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenTokenExchangeFails_ThenTryAuthorizeSilentlyReturnsNull()
        {
            var store = new OfflineStore()
            {
                StoredCredential = new OidcOfflineCredential(
                    OidcIssuer.Sts,
                    null,
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
        public async Task WhenTokenExchangeSucceeds_ThenTryAuthorizeSilentlyReturnsSession()
        {
            var store = new OfflineStore()
            {
                StoredCredential = new OidcOfflineCredential(
                    OidcIssuer.Sts,
                    null,
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
            Assert.AreEqual("SUBJECT", session!.Username);
            Assert.AreEqual("access-token", ((UserCredential)session.ApiCredential).Token.AccessToken);
            Assert.AreEqual("refresh-token", ((UserCredential)session.ApiCredential).Token.RefreshToken);

            // Terminate session.
            Assert.IsNotNull(store.StoredCredential);
            session.Terminate();
            Assert.IsNull(store.StoredCredential);
        }
    }
}
