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

using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Transport
{
    [TestFixture]
    public class TestIapTunnelFactory
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static readonly UserAgent SampleUserAgent = new UserAgent(
            "Test",
            new System.Version(1, 0),
            Environment.OSVersion.VersionString);

        //---------------------------------------------------------------------
        // CreateTunnel.
        //---------------------------------------------------------------------

        [Test]
        public void CreateTunnel_WhenEndpointIsNotLoopback()
        {
            var protocol = new Mock<IProtocol>();
            var policy = new Mock<ITransportPolicy>();

            var profile = new IapTunnel.Profile(
                protocol.Object,
                policy.Object,
                SampleLocator,
                22,
                new IPEndPoint(IPAddress.Parse("10.0.0.1"), 22));

            var iapClient = new IapClient(
                IapClient.CreateEndpoint(),
                TestProject.InvalidAuthorization,
                SampleUserAgent);

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(
                () => new IapTunnel.Factory(iapClient).CreateTunnelAsync(
                    profile,
                    TimeSpan.MaxValue,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void CreateTunnel_WhenProbeDenied_CreateTunnelThrowsException()
        {
            var protocol = new Mock<IProtocol>();
            var policy = new Mock<ITransportPolicy>();

            var iapClient = new IapClient(
                IapClient.CreateEndpoint(),
                TestProject.InvalidAuthorization,
                SampleUserAgent);

            var profile = new IapTunnel.Profile(
                protocol.Object,
                policy.Object,
                SampleLocator,
                22);

            ExceptionAssert.ThrowsAggregateException<SshRelayDeniedException>(
                () => new IapTunnel.Factory(iapClient).CreateTunnelAsync(
                    profile,
                    TimeSpan.FromMinutes(1),
                    CancellationToken.None).Wait());
        }
    }
}
