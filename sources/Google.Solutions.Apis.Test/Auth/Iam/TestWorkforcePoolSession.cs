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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Iam;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.Apis.Test.Auth.Iam
{
    [TestFixture]
    public class TestWorkforcePoolSession
    {
        private static UserCredential CreateUserCredential(
            string refreshToken,
            string accessToken)
        {
            var flow = new Mock<IAuthorizationCodeFlow>().Object;
            return new UserCredential(flow, null, null)
            {
                Token = new TokenResponse()
                {
                    RefreshToken = refreshToken,
                    AccessToken = accessToken
                }
            };
        }

        //---------------------------------------------------------------------
        // PrincipalIdentifier.
        //---------------------------------------------------------------------

        [Test]
        public void PrincipalIdentifier()
        {
            var session = new WorkforcePoolSession(
                CreateUserCredential("rt", "at"),
                new WorkforcePoolProviderLocator("global", "pool-1", "provider-1"),
                new WorkforcePoolIdentity("global", "pool-1", "subject-1"));

            Assert.That(
                session.PrincipalIdentifier, Is.EqualTo("principal://iam.googleapis.com/locations/global/workforcePools/" +
                "pool-1/subject/subject-1"));
        }

        //---------------------------------------------------------------------
        // Username.
        //---------------------------------------------------------------------

        [Test]
        public void Username()
        {
            var session = new WorkforcePoolSession(
                CreateUserCredential("rt", "at"),
                new WorkforcePoolProviderLocator("global", "pool-1", "provider-1"),
                new WorkforcePoolIdentity("global", "pool-1", "subject-1"));

            Assert.That(session.Username, Is.EqualTo("subject-1"));
        }

        //---------------------------------------------------------------------
        // Splice.
        //---------------------------------------------------------------------

        [Test]
        public void Splice_WhenNewSessionNotCompatible_ThenThrowsException()
        {
            var session = new WorkforcePoolSession(
                CreateUserCredential("rt", "at"),
                new WorkforcePoolProviderLocator("global", "pool-1", "provider-1"),
                new WorkforcePoolIdentity("global", "pool-1", "subject-1"));

            Assert.Throws<ArgumentException>(
                () => session.Splice(new Mock<IOidcSession>().Object));
        }

        [Test]
        public void Splice_WhenNewSessionCompatible_ThenReplacesTokens()
        {
            var session = new WorkforcePoolSession(
                CreateUserCredential("old-rt", "old-at"),
                new WorkforcePoolProviderLocator("global", "pool-1", "provider-1"),
                new WorkforcePoolIdentity("global", "pool-1", "subject-1"));

            Assert.That(((UserCredential)session.ApiCredential).Token.RefreshToken, Is.EqualTo("old-rt"));
            Assert.That(((UserCredential)session.ApiCredential).Token.AccessToken, Is.EqualTo("old-at"));


            var newSession = new WorkforcePoolSession(
                CreateUserCredential("new-rt", "new-at"),
                new WorkforcePoolProviderLocator("global", "pool-1", "provider-1"),
                new WorkforcePoolIdentity("global", "pool-1", "subject-1"));

            session.Splice(newSession);

            Assert.That(((UserCredential)session.ApiCredential).Token.RefreshToken, Is.EqualTo("new-rt"));
            Assert.That(((UserCredential)session.ApiCredential).Token.AccessToken, Is.EqualTo("new-at"));
        }

        //---------------------------------------------------------------------
        // Terminate.
        //---------------------------------------------------------------------

        [Test]
        public void Terminate()
        {
            var session = new WorkforcePoolSession(
                CreateUserCredential("rt", "at"),
                new WorkforcePoolProviderLocator("global", "pool-1", "provider-1"),
                new WorkforcePoolIdentity("global", "pool-1", "subject-1"));

            var eventRaised = false;
            session.Terminated += (_, __) => eventRaised = true;
            session.Terminate();

            Assert.That(eventRaised, Is.True);
        }

        //---------------------------------------------------------------------
        // CreateDomainSpecificServiceUri.
        //---------------------------------------------------------------------

        [Test]
        public void CreateDomainSpecificServiceUri()
        {
            var session = new WorkforcePoolSession(
                CreateUserCredential("rt", "at"),
                new WorkforcePoolProviderLocator("global", "pool-1", "provider-1"),
                new WorkforcePoolIdentity("global", "pool-1", "subject-1"));

            Assert.That(
                session.CreateDomainSpecificServiceUri(new Uri("https://console.cloud.google/")), Is.EqualTo(new Uri("https://auth.cloud.google/signin/locations/global/workforcePools/pool-1/providers/provider-1" +
                    "?continueUrl=https:%2F%2Fconsole.cloud.google%2F")));

        }
    }
}
