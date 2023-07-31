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

namespace Google.Solutions.Apis.Test.Auth.Gaia
{
    [TestFixture]
    public class TestGaiaOidcSession
    {
        private static UserCredential CreateUserCredential(IJsonWebToken jwt)
        {
            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer()
                {
                    ClientSecrets = new ClientSecrets()
                });
            return new UserCredential(flow, null, null)
            {
                Token = new TokenResponse()
                {
                    RefreshToken = "rt",
                    IdToken = jwt.ToString()
                }
            };
        }

        //---------------------------------------------------------------------
        // OidcSession.
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
            var session = new GaiaOidcClient.OidcSession(
                new Mock<IDeviceEnrollment>().Object,
                CreateUserCredential(idToken),
                idToken);

            Assert.AreEqual("x@example.com", session.Username);
            Assert.AreEqual("x@example.com", session.Email);
            Assert.AreEqual("example.com", session.HostedDomain);
        }
    }
}
