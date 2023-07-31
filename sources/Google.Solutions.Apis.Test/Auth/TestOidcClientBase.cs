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
                : base(store)
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

        //---------------------------------------------------------------------
        // TryAuthorizeSilently.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenOfflineCredentialNotFound_ThenTryAuthorizeSilentlyReturnsNull()
        {
            // Empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            OidcOfflineCredential offlineCredential = null;
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(false);

            var client = new SampleClient(store.Object);
            var session = await client
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(session);
        }

        [Test]
        public async Task WhenActivatingOfflineCredentialFails_ThenTryAuthorizeSilentlyClearsStore()
        {
            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential("rt", "idt");
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            var client = new SampleClient(store.Object)
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
            // Non-empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            var offlineCredential = new OidcOfflineCredential("rt", "idt");
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(true);

            var client = new SampleClient(store.Object)
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
            // Empty store.
            var store = new Mock<IOidcOfflineCredentialStore>();
            OidcOfflineCredential offlineCredential = null;
            store.Setup(s => s.TryRead(out offlineCredential)).Returns(false);

            var client = new SampleClient(store.Object)
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
