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
using Google.Solutions.Apis.Client;
using Moq;
using NUnit.Framework;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Auth
{
    [TestFixture]
    public class TestOidcClientBase
    {
        private class SampleClient : OidcClientBase
        {
            public SampleClient(
                IDeviceEnrollment deviceEnrollment,
                IOidcOfflineCredentialStore store)
                : base(deviceEnrollment, store)
            {
            }

            public Func<IOidcSession> ActivateOfflineCredential;
            public Func<IOidcSession> AuthorizeWithBrowser;

            public override IServiceEndpoint Endpoint => throw new System.NotImplementedException();

            protected override Task<IOidcSession> ActivateOfflineCredentialAsync(
                OidcOfflineCredential offlineCredential,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(this.ActivateOfflineCredential());
            }

            protected override Task<IOidcSession> AuthorizeWithBrowserAsync(
                OidcOfflineCredential offlineCredential,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(this.AuthorizeWithBrowser());
            }
        }

        private static UserCredential CreateUserCredential()
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
                    IdToken = "idt"
                }
            };
        }

        private static IJsonWebToken CreateIdToken()
        {
            var jwt = new Mock<IJsonWebToken>();
            jwt
                .SetupGet(j => j.Payload)
                .Returns(new Google.Apis.Auth.GoogleJsonWebSignature.Payload()
                {
                    Email = "x@example.com"
                });
            return jwt.Object;
        }

        //---------------------------------------------------------------------
        // TryAuthorizeSilently.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenOfflineCredentialNotFound_ThenTryAuthorizeSilentlyReturnsNull()
        {
            // Not enrolled.
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            // Empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            OidcOfflineCredential offlineCredential = null;
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(false);

            var client = new SampleClient(enrollment.Object, store.Object);
            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(session);
        }

        [Test]
        public async Task WhenActivatingOfflineCredentialFails_ThenTryAuthorizeSilentlyClearsStore()
        {
            // Not enrolled.
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential("rt", "idt");
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            var client = new SampleClient(enrollment.Object, store.Object)
            {
                ActivateOfflineCredential = () => throw new Exception("mock")
            };

            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(session);
            store.Verify(s => s.Clear(), Times.Once);
        }

        [Test]
        public async Task WhenActivatingOfflineCredentialSucceeds_ThenTryAuthorizeSilentlySavesOfflineCredential()
        {
            // Not enrolled.
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential("rt", "idt");
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            var client = new SampleClient(enrollment.Object, store.Object)
            {
                ActivateOfflineCredential = () => new Mock<IOidcSession>().Object
            };

            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(session);
            store.Verify(s => s.Write(It.IsAny<OidcOfflineCredential>()), Times.Once);
        }

        //---------------------------------------------------------------------
        // AuthorizeAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAuthorizationSucceeds_ThenAuthorizeSavesOfflineCredential()
        {
            // Not enrolled.
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.NotEnrolled);

            // Empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            OidcOfflineCredential offlineCredential = null;
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(false);

            var client = new SampleClient(enrollment.Object, store.Object)
            {
                AuthorizeWithBrowser = () => new Mock<IOidcSession>().Object
            };
            
            var session = await client
                .AuthorizeAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(session);
            store.Verify(s => s.Write(It.IsAny<OidcOfflineCredential>()), Times.Once);
        }
    }
}
