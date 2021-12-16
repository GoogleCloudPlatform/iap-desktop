//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapTunneling.Iap;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Tunnel
{
    [TestFixture]
    public class TestTunnelBrokerService : CommonFixtureBase
    {
        [Test]
        public async Task WhenConnectSuccessful_ThenOpenTunnelsIncludesTunnel()
        {
            var mockTunnelService = new Mock<ITunnelService>();

            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelOpenedEvent>()))
                .Returns(Task.FromResult(true));

            var mockTunnel = new Mock<ITunnel>();
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));

            var broker = new TunnelBrokerService(mockTunnelService.Object, mockEventService.Object);
            var vmInstanceRef = new InstanceLocator("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(
                    destination,
                    It.IsAny<ISshRelayPolicy>()))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel = await broker
                .ConnectAsync(
                    destination,
                    new AllowAllRelayPolicy(),
                    TimeSpan.FromMinutes(1))
                .ConfigureAwait(false);

            Assert.IsNotNull(tunnel);
            Assert.AreEqual(1, broker.OpenTunnels.Count());
            Assert.IsTrue(broker.IsConnected(destination));

            Assert.AreSame(tunnel, broker.OpenTunnels.First());
        }

        [Test]
        public async Task WhenConnectSuccessful_OpenEventIsFired()
        {
            var mockTunnelService = new Mock<ITunnelService>();

            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelOpenedEvent>()))
                .Returns(Task.FromResult(true));

            var mockTunnel = new Mock<ITunnel>();
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));

            var broker = new TunnelBrokerService(mockTunnelService.Object, mockEventService.Object);
            var vmInstanceRef = new InstanceLocator("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(
                    destination,
                    It.IsAny<ISshRelayPolicy>()))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel = await broker
                .ConnectAsync(
                    destination,
                    new AllowAllRelayPolicy(),
                    TimeSpan.FromMinutes(1))
                .ConfigureAwait(false);

            mockEventService.Verify(s => s.FireAsync(It.IsAny<TunnelOpenedEvent>()), Times.Once);
        }

        [Test]
        public async Task WhenConnectingTwice_ExistingTunnelIsReturned()
        {
            var mockTunnelService = new Mock<ITunnelService>();

            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelOpenedEvent>()))
                .Returns(Task.FromResult(true));

            var mockTunnel = new Mock<ITunnel>();
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));
            var broker = new TunnelBrokerService(mockTunnelService.Object, mockEventService.Object);

            var vmInstanceRef = new InstanceLocator("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(
                    destination,
                    It.IsAny<ISshRelayPolicy>()))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel1 = await broker
                .ConnectAsync(
                    destination,
                    new AllowAllRelayPolicy(),
                    TimeSpan.FromMinutes(1))
                .ConfigureAwait(false);

            var tunnel2 = await broker
                .ConnectAsync(
                    destination,
                    new AllowAllRelayPolicy(),
                    TimeSpan.FromMinutes(1))
                .ConfigureAwait(false);

            Assert.IsNotNull(tunnel1);
            Assert.IsNotNull(tunnel2);
            Assert.AreSame(tunnel1, tunnel2);
            Assert.AreEqual(1, broker.OpenTunnels.Count());
        }

        [Test]
        public void WhenConnectFails_ThenOpenTunnelsDoesNotIncludeTunnel()
        {
            var mockTunnelService = new Mock<ITunnelService>();

            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelOpenedEvent>()))
                .Returns(Task.FromResult(true));

            var broker = new TunnelBrokerService(mockTunnelService.Object, mockEventService.Object);
            var vmInstanceRef = new InstanceLocator("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(
                    destination,
                    It.IsAny<ISshRelayPolicy>()))
                .Returns(Task.FromException<ITunnel>(new ApplicationException()));

            ExceptionAssert.ThrowsAggregateException<ApplicationException>(() =>
            {
                broker.ConnectAsync(
                    destination,
                    new AllowAllRelayPolicy(),
                    TimeSpan.FromMinutes(1)).Wait();
            });

            Assert.AreEqual(0, broker.OpenTunnels.Count());
        }

        [Test]
        public void WhenProbeFails_ThenOpenTunnelsDoesNotIncludeTunnel()
        {
            var mockTunnelService = new Mock<ITunnelService>();

            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelOpenedEvent>()))
                .Returns(Task.FromResult(true));

            var mockTunnel = new Mock<ITunnel>();
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromException(new ApplicationException()));

            var broker = new TunnelBrokerService(mockTunnelService.Object, mockEventService.Object);
            var vmInstanceRef = new InstanceLocator("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(
                    destination,
                    It.IsAny<ISshRelayPolicy>()))
                .Returns(Task.FromResult(mockTunnel.Object));

            ExceptionAssert.ThrowsAggregateException<ApplicationException>(() =>
            {
                broker.ConnectAsync(
                    destination,
                    new AllowAllRelayPolicy(),
                    TimeSpan.FromMinutes(1)).Wait();
            });

            Assert.AreEqual(0, broker.OpenTunnels.Count());
        }

        [Test]
        public async Task WhenClosingTunnel_ThenTunnelIsRemovedFromOpenTunnels()
        {
            var mockTunnelService = new Mock<ITunnelService>();

            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelOpenedEvent>()))
                .Returns(Task.FromResult(true));

            var mockTunnel = new Mock<ITunnel>();
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));
            mockTunnel.Setup(t => t.Close());

            var broker = new TunnelBrokerService(mockTunnelService.Object, mockEventService.Object);
            var vmInstanceRef = new InstanceLocator("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(
                    destination,
                    It.IsAny<ISshRelayPolicy>()))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel = await broker
                .ConnectAsync(
                    destination,
                    new AllowAllRelayPolicy(),
                    TimeSpan.FromMinutes(1))
                .ConfigureAwait(false);

            Assert.AreEqual(1, broker.OpenTunnels.Count());

            await broker
                .DisconnectAsync(destination)
                .ConfigureAwait(false);

            Assert.AreEqual(0, broker.OpenTunnels.Count());
        }

        [Test]
        public async Task WhenClosingAllTunnels_AllTunnelsAreClosed()
        {
            var mockTunnelService = new Mock<ITunnelService>();

            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelOpenedEvent>()))
                .Returns(Task.FromResult(true));

            var mockTunnel = new Mock<ITunnel>();
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));
            mockTunnel.Setup(t => t.Close());

            var broker = new TunnelBrokerService(mockTunnelService.Object, mockEventService.Object);
            var vmInstanceRef = new InstanceLocator("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(
                    destination,
                    It.IsAny<ISshRelayPolicy>()))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel = await broker
                .ConnectAsync(
                    destination,
                    new AllowAllRelayPolicy(),
                    TimeSpan.FromMinutes(1))
                .ConfigureAwait(false);

            Assert.AreEqual(1, broker.OpenTunnels.Count());

            await broker
                .DisconnectAllAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(0, broker.OpenTunnels.Count());
        }

        [Test]
        public async Task WhenClosingTunnel_CloseEventIsFired()
        {
            var mockTunnelService = new Mock<ITunnelService>();

            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelOpenedEvent>()))
                .Returns(Task.FromResult(true));
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelClosedEvent>()))
                .Returns(Task.FromResult(true));

            var mockTunnel = new Mock<ITunnel>();
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));
            mockTunnel.Setup(t => t.Close());

            var broker = new TunnelBrokerService(mockTunnelService.Object, mockEventService.Object);
            var vmInstanceRef = new InstanceLocator("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(
                    destination,
                    It.IsAny<ISshRelayPolicy>()))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel = await broker
                .ConnectAsync(
                    destination,
                    new AllowAllRelayPolicy(),
                    TimeSpan.FromMinutes(1))
                .ConfigureAwait(false);
            await broker
                .DisconnectAsync(destination)
                .ConfigureAwait(false);

            mockEventService.Verify(s => s.FireAsync(It.IsAny<TunnelClosedEvent>()), Times.Once);
        }

        [Test]
        public async Task WhenClosingAllTunnels_CloseEventsAreFired()
        {
            var mockTunnelService = new Mock<ITunnelService>();

            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelOpenedEvent>()))
                .Returns(Task.FromResult(true));
            mockEventService.Setup(s => s.FireAsync(It.IsAny<TunnelClosedEvent>()))
                .Returns(Task.FromResult(true));

            var mockTunnel = new Mock<ITunnel>();
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));
            mockTunnel.Setup(t => t.Close());

            var broker = new TunnelBrokerService(mockTunnelService.Object, mockEventService.Object);
            var vmInstanceRef = new InstanceLocator("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(
                    destination,
                    It.IsAny<ISshRelayPolicy>()))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel = await broker
                .ConnectAsync(
                    destination,
                    new AllowAllRelayPolicy(),
                    TimeSpan.FromMinutes(1))
                .ConfigureAwait(false);
            await broker
                .DisconnectAllAsync()
                .ConfigureAwait(false);

            mockEventService.Verify(s => s.FireAsync(It.IsAny<TunnelClosedEvent>()), Times.Once);
        }
    }
}
