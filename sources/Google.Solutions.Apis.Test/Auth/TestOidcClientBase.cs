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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Auth
{
    [TestFixture]
    public class TestOidcClientBase
    {
        private class SampleClient : OidcClientBase
        {
            public SampleClient(IOidcOfflineCredentialStore store)
                : base(
                      store,
                      new OidcClientRegistration(
                          OidcIssuer.Gaia,
                          "client-id",
                          "client-secret",
                          "/"))
            {
            }

            public Func<IOidcSession>? ActivateOfflineCredential;
            public Func<IOidcSession>? AuthorizeWithBrowser;

            public override IServiceEndpoint Endpoint => throw new System.NotImplementedException();

            protected override Task<IOidcSession> ActivateOfflineCredentialAsync(
                OidcOfflineCredential offlineCredential,
                CancellationToken cancellationToken)
            {
                if (this.ActivateOfflineCredential == null)
                {
                    throw new InvalidOperationException();
                }

                return Task.FromResult(this.ActivateOfflineCredential());
            }

            protected override Task<IOidcSession> AuthorizeWithBrowserAsync(
                OidcOfflineCredential? offlineCredential,
                ICodeReceiver codeReceiver,
                CancellationToken cancellationToken)
            {
                if (this.AuthorizeWithBrowser == null)
                {
                    throw new InvalidOperationException();
                }

                return Task.FromResult(this.AuthorizeWithBrowser());
            }
        }

        private static Mock<IOidcSession> CreateSession()
        {
            var session = new Mock<IOidcSession>();
            session
                .SetupGet(s => s.OfflineCredential)
                .Returns(new OidcOfflineCredential(OidcIssuer.Gaia, "scope", "rt", "idt"));
            return session;
        }

        //---------------------------------------------------------------------
        // TryAuthorizeSilently.
        //---------------------------------------------------------------------

        [Test]
        public async Task TryAuthorizeSilently_WhenOfflineCredentialNotFound_ThenReturnsNull()
        {
            // Empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            OidcOfflineCredential? offlineCredential = null;
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(false);

            var client = new SampleClient(store.Object);
            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(session);
        }

        [Test]
        public async Task TryAuthorizeSilently_WhenOfflineCredentialFromWrongIssuer_ThenRetainsStore()
        {
            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential(
                OidcIssuer.Sts, "openid", "rt", "idt");
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            var client = new SampleClient(store.Object);
            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(session);
        }

        [Test]
        public async Task TryAuthorizeSilently_WhenActivatingOfflineCredentialFails_ThenRetainsStore()
        {
            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential(
                OidcIssuer.Gaia, "openid", "rt", "idt");
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            var client = new SampleClient(store.Object)
            {
                ActivateOfflineCredential = () => throw new Exception("mock")
            };

            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(session);
            store.Verify(s => s.Clear(), Times.Never);
        }

        [Test]
        public async Task TryAuthorizeSilently_WhenActivatingOfflineCredentialSucceeds_ThenSavesOfflineCredential()
        {
            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential(
                OidcIssuer.Gaia, "openid", "rt", "idt");
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            var client = new SampleClient(store.Object)
            {
                ActivateOfflineCredential = () => CreateSession().Object
            };

            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(session, Is.Not.Null);
            store.Verify(s => s.Write(It.IsAny<OidcOfflineCredential>()), Times.Once);
        }

        //---------------------------------------------------------------------
        // AuthorizeAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task Authorize_WhenAuthorizationSucceeds_ThenSavesOfflineCredential()
        {
            // Empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            OidcOfflineCredential? offlineCredential = null;
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(false);

            var client = new SampleClient(store.Object)
            {
                AuthorizeWithBrowser = () => CreateSession().Object
            };

            var session = await client
                .AuthorizeAsync(
                    new Mock<ICodeReceiver>().Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(session, Is.Not.Null);
            store.Verify(s => s.Write(It.IsAny<OidcOfflineCredential>()), Times.Once);
        }

        [Test]
        public async Task Authorize_WhenOfflineCredentialFromDifferentIssuer_ThenIgnoresOfflineCredential()
        {
            // Wrong offline credential.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential(OidcIssuer.Sts, "scope", "rt", null);
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            var client = new SampleClient(store.Object)
            {
                AuthorizeWithBrowser = () => CreateSession().Object
            };

            var session = await client
                .AuthorizeAsync(
                    new Mock<ICodeReceiver>().Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(session, Is.Not.Null);
            store.Verify(s => s.Write(It.IsAny<OidcOfflineCredential>()), Times.Once);
        }
    }
}
