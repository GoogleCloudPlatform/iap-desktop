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
using Google.Solutions.IapDesktop.Application.Profile.Auth;
using Google.Solutions.Platform.Net;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Auth
{
    [TestFixture]
    public class TestAuthorization
    {
        private static Mock<IOidcClient> CreateClient()
        {
            var client = new Mock<IOidcClient>();
            client
                .SetupGet(c => c.Registration)
                .Returns(new OidcClientRegistration(OidcIssuer.Gaia, "client-id", "", "/"));
            return client;
        }

        //---------------------------------------------------------------------
        // Session.
        //---------------------------------------------------------------------

        [Test]
        public void Session_WhenNotAuthorized_ThenSessionThrowsException()
        {
            var client = CreateClient();
            var authorization = new Authorization(
                client.Object,
                new Mock<IDeviceEnrollment>().Object);

            Assert.Throws<InvalidOperationException>(
                () => authorization.Session.ToString());
        }

        //---------------------------------------------------------------------
        // TryAuthorizeSilently.
        //---------------------------------------------------------------------

        [Test]
        public async Task TryAuthorizeSilently_WhenSuccessful_ThenTryAuthorizeSilentlySetsSession()
        {
            var session = new Mock<IOidcSession>();
            var client = CreateClient();
            client
                .Setup(c => c.TryAuthorizeSilentlyAsync(CancellationToken.None))
                .ReturnsAsync(session.Object);

            var authorization = new Authorization(
                client.Object,
                new Mock<IDeviceEnrollment>().Object);

            await authorization
                .TryAuthorizeSilentlyAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreSame(session.Object, authorization.Session);
        }

        //---------------------------------------------------------------------
        // Authorize.
        //---------------------------------------------------------------------

        [Test]
        public async Task Authorize_WhenSessionIsNull_ThenAuthorizeSetsSession()
        {
            var session = new Mock<IOidcSession>();
            var client = CreateClient();
            client
                .Setup(c => c.AuthorizeAsync(It.IsAny<ICodeReceiver>(), CancellationToken.None))
                .ReturnsAsync(session.Object);

            var authorization = new Authorization(
                client.Object,
                new Mock<IDeviceEnrollment>().Object);

            var eventsRaised = 0;
            authorization.Reauthorized += (_, __) => eventsRaised++;

            await authorization
                .AuthorizeAsync(BrowserPreference.Default, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreSame(session.Object, authorization.Session);
            Assert.AreEqual(0, eventsRaised);
        }

        [Test]
        public async Task Authorize_WhenSessionExists_ThenAuthorizeSplicesSessionsAndRaisesEvent()
        {
            var client = CreateClient();
            var authorization = new Authorization(
                client.Object,
                new Mock<IDeviceEnrollment>().Object);

            var eventsRaised = 0;
            authorization.Reauthorized += (_, __) => eventsRaised++;

            // First session.
            var firstSession = new Mock<IOidcSession>();
            client
                .Setup(c => c.AuthorizeAsync(It.IsAny<ICodeReceiver>(), CancellationToken.None))
                .ReturnsAsync(firstSession.Object);
            await authorization
                .AuthorizeAsync(BrowserPreference.Default, CancellationToken.None)
                .ConfigureAwait(false);

            // Second session.
            var secondSession = new Mock<IOidcSession>();
            client
                .Setup(c => c.AuthorizeAsync(It.IsAny<ICodeReceiver>(), CancellationToken.None))
                .ReturnsAsync(secondSession.Object);
            await authorization
                .AuthorizeAsync(BrowserPreference.Default, CancellationToken.None)
                .ConfigureAwait(false);

            firstSession.Verify(s => s.Splice(secondSession.Object), Times.Once);

            Assert.AreSame(firstSession.Object, authorization.Session);
            Assert.AreEqual(1, eventsRaised);
        }
    }
}
