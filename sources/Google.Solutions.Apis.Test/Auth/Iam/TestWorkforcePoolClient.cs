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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Iam;
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
        private static Mock<IDeviceEnrollment> CreateDisabledEnrollment()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Disabled);
            return enrollment;
        }

        private class OfflineStore : IOidcOfflineCredentialStore
        {
            public OidcOfflineCredential StoredCredential { get; private set; }

            public void Clear()
            {
                this.StoredCredential = null;
            }

            public bool TryRead(out OidcOfflineCredential credential)
            {
                credential = this.StoredCredential;
                return credential != null;
            }

            public void Write(OidcOfflineCredential credential)
            {
                this.StoredCredential = credential;
            }
        }

        [Test]
        [InteractiveTest] 
        public async Task __TestAuth() // TODO: Remove this test
        {
            var store = new OfflineStore();

            var secret = Environment.GetEnvironmentVariable("WWAUTH_CLIENT_SECRET").Split(':');
            var registration = new OidcClientRegistration(
                OidcIssuer.Iam,
                secret[0],
                secret[1],
                "/");

            var provider = new WorkforcePoolProviderLocator(
                "global",
                "ntdev-azuread",
                "ntdev-azuread-saml");

            var client = new WorkforcePoolClient(
                WorkforcePoolClient.CreateEndpoint(),
                CreateDisabledEnrollment().Object,
                store,
                provider,
                registration,
                TestProject.UserAgent);

            var session = await client
                .AuthorizeAsync(
                    new LoopbackCodeReceiver(
                        "/",
                        "done!!1!"),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(session);
            Assert.IsNotNull(session.Username);
            Assert.IsNotNull(session.OfflineCredential);
            Assert.IsNotNull(store.StoredCredential);

            var reactivatedSession = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(reactivatedSession);
            Assert.AreEqual(session.Username, reactivatedSession.Username);
            Assert.IsNotNull(session.OfflineCredential);
            Assert.IsNotNull(store.StoredCredential);
        }
    }
}
