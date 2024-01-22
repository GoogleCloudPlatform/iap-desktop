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

using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Auth.Gaia
{
    [TestFixture]
    public class TestGaiaOidcSession
    {
        private static UserCredential CreateUserCredential(
            string refreshToken,
            string accessToken,
            IJsonWebToken jwt)
        {
            var flow = new Mock<IAuthorizationCodeFlow>().Object;
            return new UserCredential(flow, null, null)
            {
                Token = new TokenResponse()
                {
                    RefreshToken = refreshToken,
                    AccessToken = accessToken,
                    IdToken = jwt.ToString()
                }
            };
        }

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        [Test]
        public void BasicProperties()
        {
            var idToken = new UnverifiedGaiaJsonWebToken(
                new GoogleJsonWebSignature.Header(),
                new GoogleJsonWebSignature.Payload()
                {
                    Email = "x@example.com",
                    HostedDomain = "example.com"
                });
            var session = new GaiaOidcSession(
                CreateUserCredential("rt", "at", idToken),
                idToken);

            Assert.AreEqual("x@example.com", session.Username);
            Assert.AreEqual("x@example.com", session.Email);
            Assert.AreEqual("example.com", session.HostedDomain);
        }

        //---------------------------------------------------------------------
        // Splice.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNewSessionNotCompatible_ThenSpliceThrowsException()
        {
            var idToken = new UnverifiedGaiaJsonWebToken(
                new GoogleJsonWebSignature.Header(),
                new GoogleJsonWebSignature.Payload()
                {
                    Email = "x@example.com",
                });
            var session = new GaiaOidcSession(
                CreateUserCredential("rt", "at", idToken),
                idToken);

            Assert.Throws<ArgumentException>(
                () => session.Splice(new Mock<IOidcSession>().Object));
        }

        [Test]
        public void WhenNewSessionCompatible_ThenSpliceReplacesTokens()
        {
            var idToken = new UnverifiedGaiaJsonWebToken(
                new GoogleJsonWebSignature.Header(),
                new GoogleJsonWebSignature.Payload()
                {
                    Email = "x@example.com",
                });
            var session = new GaiaOidcSession(
                CreateUserCredential("old-rt", "old-at", idToken),
                idToken);

            Assert.AreEqual("old-rt", ((UserCredential)session.ApiCredential).Token.RefreshToken);
            Assert.AreEqual("old-at", ((UserCredential)session.ApiCredential).Token.AccessToken);

            var newIdToken = new UnverifiedGaiaJsonWebToken(
                new GoogleJsonWebSignature.Header(),
                new GoogleJsonWebSignature.Payload()
                {
                    Email = "x@example.com",
                });
            var newSession = new GaiaOidcSession(
                CreateUserCredential("new-rt", "new-at", newIdToken),
                newIdToken);

            session.Splice(newSession);

            Assert.AreEqual("new-rt", ((UserCredential)session.ApiCredential).Token.RefreshToken);
            Assert.AreEqual("new-at", ((UserCredential)session.ApiCredential).Token.AccessToken);
        }

        //---------------------------------------------------------------------
        // Terminate.
        //---------------------------------------------------------------------

        [Test]
        public void TerminateRaisesEvent()
        {
            var idToken = new UnverifiedGaiaJsonWebToken(
                new GoogleJsonWebSignature.Header(),
                new GoogleJsonWebSignature.Payload()
                {
                    Email = "x@example.com",
                    HostedDomain = "example.com"
                });
            var session = new GaiaOidcSession(
                CreateUserCredential("rt", "at", idToken),
                idToken);

            var eventRaised = false;
            session.Terminated += (_, __) => eventRaised = true;
            session.Terminate();

            Assert.IsTrue(eventRaised);
        }

        //---------------------------------------------------------------------
        // RevokeGrant.
        //---------------------------------------------------------------------

        [Test]
        public async Task RevokeGrantRevokesRefreshToken()
        {
            var flow = new Mock<IAuthorizationCodeFlow>();

            var credential = new UserCredential(flow.Object, null, null)
            {
                Token = new TokenResponse()
                {
                    RefreshToken = "rt",
                    AccessToken = "at"
                }
            };

            var session = new GaiaOidcSession(
                credential,
                new UnverifiedGaiaJsonWebToken(
                    new GoogleJsonWebSignature.Header(),
                    new GoogleJsonWebSignature.Payload()
                    {
                        Email = "x@example.com",
                    }));

            var eventRaised = false;
            session.Terminated += (_, __) => eventRaised = true;

            await session
                .RevokeGrantAsync(CancellationToken.None)
                .ConfigureAwait(false);

            flow.Verify(
                f => f.RevokeTokenAsync(null, "rt", CancellationToken.None),
                Times.Once);
            Assert.IsTrue(eventRaised);
        }

        //---------------------------------------------------------------------
        // CreateDomainSpecificServiceUri.
        //---------------------------------------------------------------------

        [Test]
        public void WhenHdSet_ThenCreateDomainSpecificServiceUriReturnsUri()
        {
            var idToken = new UnverifiedGaiaJsonWebToken(
                new GoogleJsonWebSignature.Header(),
                new GoogleJsonWebSignature.Payload()
                {
                    Email = "x@example.com",
                    HostedDomain = "example.com"
                });
            var session = new GaiaOidcSession(
                CreateUserCredential("rt", "at", idToken),
                idToken);

            Assert.AreEqual(
                new Uri("https://www.google.com/a/example.com/ServiceLogin" +
                    "?continue=https:%2F%2Fconsole.cloud.google.com%2F"),
                session.CreateDomainSpecificServiceUri(new Uri("https://console.cloud.google.com/")));
        }

        [Test]
        public void WhenHdNotSet_ThenCreateDomainSpecificServiceUriReturnsUri()
        {
            var idToken = new UnverifiedGaiaJsonWebToken(
                new GoogleJsonWebSignature.Header(),
                new GoogleJsonWebSignature.Payload()
                {
                    Email = "x@example.com"
                });
            var session = new GaiaOidcSession(
                CreateUserCredential("rt", "at", idToken),
                idToken);

            Assert.AreEqual(
                new Uri("https://console.cloud.google.com/"),
                session.CreateDomainSpecificServiceUri(new Uri("https://console.cloud.google.com/")));
        }
    }
}
