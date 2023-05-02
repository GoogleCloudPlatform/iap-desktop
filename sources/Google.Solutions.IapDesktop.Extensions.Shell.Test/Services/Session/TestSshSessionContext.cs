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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.Ssh.Auth;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Session
{
    [TestFixture]
    public class TestSshSessionContext
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // AuthorizeCredential.
        //---------------------------------------------------------------------

        [Test]
        public void AuthorizeCredentialReturnsCredential()
        {
            var authorizedKey = AuthorizedKeyPair.ForMetadata(
                new Mock<ISshKeyPair>().Object,
                "username",
                false,
                new Mock<IAuthorization>().Object);

            var key = new Mock<ISshKeyPair>().Object;
            var keyAuthService = new Mock<IKeyAuthorizationService>();
            keyAuthService
                .Setup(s => s.AuthorizeKeyAsync(
                    SampleInstance,
                    key,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string>(),
                    KeyAuthorizationMethods.All,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(authorizedKey);

            var context = new SshSessionContext(
                new Mock<ITunnelBrokerService>().Object,
                keyAuthService.Object,
                new Mock<IComputeEngineAdapter>().Object,
                SampleInstance,
                key);

            Assert.AreSame(
                authorizedKey,
                context.AuthorizeCredentialAsync(CancellationToken.None)
                    .Result
                    .Key);
        }

        //---------------------------------------------------------------------
        // ConnectTransport.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenTransportTypeIsIap_ThenConnectTransportCreatesIapTunnel()
        {
            var tunnel = new Mock<ITunnel>();
            tunnel.SetupGet(t => t.LocalPort).Returns(123);

            var tunnelBroker = new Mock<ITunnelBrokerService>();
            tunnelBroker
                .Setup(b => b.ConnectAsync(
                    It.IsAny<TunnelDestination>(),
                    It.IsAny<ISshRelayPolicy>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(tunnel.Object);

            var context = new SshSessionContext(
                tunnelBroker.Object,
                new Mock<IKeyAuthorizationService>().Object,
                new Mock<IComputeEngineAdapter>().Object,
                SampleInstance,
                new Mock<ISshKeyPair>().Object);
            context.Parameters.TransportType = Transport.TransportType.IapTunnel;

            var transport = await context
                .ConnectTransportAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(Transport.TransportType.IapTunnel, transport.Type);
            Assert.AreEqual(123, transport.Endpoint.Port);
        }

        [Test]
        public async Task WhenTransportTypeIsVpcInternal_ThenConnectTransportCreatesVpcInternalTunnel()
        {
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter
                .Setup(a => a.GetInstanceAsync(SampleInstance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Instance()
                {
                    NetworkInterfaces = new[]
                    {
                        new NetworkInterface()
                        {
                            Name = "nic0",
                            StackType = "IPV4_ONLY",
                            NetworkIP = "20.21.22.23"
                        }
                    }
                });

            var context = new SshSessionContext(
                new Mock<ITunnelBrokerService>().Object,
                new Mock<IKeyAuthorizationService>().Object,
                computeEngineAdapter.Object,
                SampleInstance,
                new Mock<ISshKeyPair>().Object);
            context.Parameters.TransportType = Transport.TransportType.Vpc;

            var transport = await context
                .ConnectTransportAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(Transport.TransportType.Vpc, transport.Type);
            Assert.AreEqual(context.Parameters.Port, transport.Endpoint.Port);
            Assert.AreEqual("20.21.22.23", transport.Endpoint.Address.ToString());
        }
    }
}
