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
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Session
{
    [TestFixture]
    public class TestRdpSessionContext
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // AuthorizeCredential.
        //---------------------------------------------------------------------

        [Test]
        public void AuthorizeCredentialReturnsCredential()
        {
            var credential = new RdpCredential("user", null, null);
            var context = new RdpSessionContext(
                new Mock<ITunnelBrokerService>().Object,
                new Mock<IComputeEngineAdapter>().Object,
                SampleInstance,
                credential,
                RdpSessionParameters.ParameterSources.Inventory);

            Assert.AreSame(
                credential,
                context.AuthorizeCredentialAsync(CancellationToken.None).Result);
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

            var context = new RdpSessionContext(
                tunnelBroker.Object,
                new Mock<IComputeEngineAdapter>().Object,
                SampleInstance,
                RdpCredential.Empty,
                RdpSessionParameters.ParameterSources.Inventory);
            context.Parameters.TransportType = SessionTransportType.IapTunnel;

            var transport = await context
                .ConnectTransportAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(SessionTransportType.IapTunnel, transport.Type);
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

            var context = new RdpSessionContext(
                new Mock<ITunnelBrokerService>().Object,
                computeEngineAdapter.Object,
                SampleInstance,
                RdpCredential.Empty,
                RdpSessionParameters.ParameterSources.Inventory);
            context.Parameters.TransportType = SessionTransportType.Vpc;

            var transport = await context
                .ConnectTransportAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(SessionTransportType.Vpc, transport.Type);
            Assert.AreEqual(context.Parameters.Port, transport.Endpoint.Port);
            Assert.AreEqual("20.21.22.23", transport.Endpoint.Address.ToString());
        }
    }
}
