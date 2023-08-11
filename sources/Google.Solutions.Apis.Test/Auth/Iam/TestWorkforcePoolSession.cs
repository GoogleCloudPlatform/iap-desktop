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

using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2;
using Google.Solutions.Apis.Auth.Iam;
using Moq;
using NUnit.Framework;

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
        // Terminate.
        //---------------------------------------------------------------------

        [Test]
        public void TerminateRaisesEvent()
        {
            var session = new WorkforcePoolSession(
                CreateUserCredential("rt", "at"),
                new WorkforcePoolIdentity("global", "pool-1", "subject-1"));

            bool eventRaised = false;
            session.Terminated += (_, __) => eventRaised = true;
            session.Terminate();

            Assert.IsTrue(eventRaised);
        }
    }
}
