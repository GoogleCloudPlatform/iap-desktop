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


using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Security;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Session
{
    [TestFixture]
    public class TestTransport
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // CreateIapTransportAsync.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSshRelayDenied_ThenCreateIapTransportThrowsException()
        {
            var timeout = TimeSpan.FromMinutes(1);
            var tunnelBroker = new Mock<ITunnelBrokerService>();
            tunnelBroker
                .Setup(t => t.ConnectAsync(
                    It.IsAny<TunnelDestination>(),
                    It.IsAny<ISshRelayPolicy>(),
                    timeout))
                .ThrowsAsync(new SshRelayDeniedException("mock"));

            ExceptionAssert.ThrowsAggregateException<ConnectionFailedException>(
                () => Transport.CreateIapTransportAsync(
                    tunnelBroker.Object,
                    SampleInstance,
                    22,
                    timeout).Wait());
        }

        [Test]
        public void WhenNetworkStreamClosedException_ThenCreateIapTransportThrowsException()
        {
            var timeout = TimeSpan.FromMinutes(1);
            var tunnelBroker = new Mock<ITunnelBrokerService>();
            tunnelBroker
                .Setup(t => t.ConnectAsync(
                    It.IsAny<TunnelDestination>(),
                    It.IsAny<ISshRelayPolicy>(),
                    timeout))
                .ThrowsAsync(new NetworkStreamClosedException("mock"));

            ExceptionAssert.ThrowsAggregateException<ConnectionFailedException>(
                () => Transport.CreateIapTransportAsync(
                    tunnelBroker.Object,
                    SampleInstance,
                    22,
                    timeout).Wait());
        }

        [Test]
        public void WhenWebSocketConnectionDeniedException_ThenCreateIapTransportThrowsException()
        {
            var timeout = TimeSpan.FromMinutes(1);
            var tunnelBroker = new Mock<ITunnelBrokerService>();
            tunnelBroker
                .Setup(t => t.ConnectAsync(
                    It.IsAny<TunnelDestination>(),
                    It.IsAny<ISshRelayPolicy>(),
                    timeout))
                .ThrowsAsync(new WebSocketConnectionDeniedException());

            ExceptionAssert.ThrowsAggregateException<ConnectionFailedException>(
                () => Transport.CreateIapTransportAsync(
                    tunnelBroker.Object,
                    SampleInstance,
                    22,
                    timeout).Wait());
        }

        [Test]
        public async Task WhenTunnelCreated_ThenCreateIapTransportReturnLoopbackEndpoint()
        {
            var timeout = TimeSpan.FromMinutes(1);
            var tunnel = new Mock<ITunnel>();
            tunnel.SetupGet(t => t.LocalPort).Returns(123);

            var tunnelBroker = new Mock<ITunnelBrokerService>();
            tunnelBroker
                .Setup(t => t.ConnectAsync(
                    It.IsAny<TunnelDestination>(),
                    It.IsAny<ISshRelayPolicy>(),
                    timeout))
                .ReturnsAsync(tunnel.Object);

            var transport = await Transport
                .CreateIapTransportAsync(
                    tunnelBroker.Object,
                    SampleInstance,
                    22,
                    timeout)
                .ConfigureAwait(false);

            Assert.AreEqual(IPAddress.Loopback, transport.Endpoint.Address);
            Assert.AreEqual(123, transport.Endpoint.Port);
        }
    }
}
