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
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.Auth;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.Testing.Common;
using Google.Solutions.Testing.Common.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Transport
{
    [TestFixture]
    public class TestIapTunnelFactory
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static readonly UserAgent SampleUserAgent
            = new UserAgent("Test", new System.Version(1, 0));

        private static Mock<IAuthorization> CreateAuthorizationWithInvalidToken()
        {
            var cred = new Mock<ICredential>();
            cred
                .Setup(c => c.GetAccessTokenForRequestAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("notatoken");

            var authz = new Mock<IAuthorization>();
            authz.SetupGet(a => a.Credential).Returns(cred.Object);

            return authz;
        }

        //---------------------------------------------------------------------
        // CreateTunnel.
        //---------------------------------------------------------------------

        [Test]
        public void WhenEndpointIsNotLoopback_ThenCreateTunnelThrowsException()
        {
            var protocol = new Mock<IProtocol>();
            var policy = new Mock<ITransportPolicy>();

            var profile = new IapTunnel.Profile(
                protocol.Object,
                policy.Object,
                SampleLocator,
                22,
                new IPEndPoint(IPAddress.Parse("10.0.0.1"), 22));

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(
                () => new IapTunnel.Factory().CreateTunnelAsync(
                    CreateAuthorizationWithInvalidToken().Object,
                    SampleUserAgent,
                    profile,
                    TimeSpan.MaxValue,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenProbeDenied_CreateTunnelThrowsException()
        {
            var protocol = new Mock<IProtocol>();
            var policy = new Mock<ITransportPolicy>();

            var profile = new IapTunnel.Profile(
                protocol.Object,
                policy.Object,
                SampleLocator,
                22);

            ExceptionAssert.ThrowsAggregateException<SshRelayDeniedException>(
                () => new IapTunnel.Factory().CreateTunnelAsync(
                    CreateAuthorizationWithInvalidToken().Object,
                    SampleUserAgent,
                    profile,
                    TimeSpan.FromMinutes(1),
                    CancellationToken.None).Wait());
        }
    }
}
