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
using Google.Solutions.Iap;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Transport
{
    [TestFixture]
    public class TestIapTunnel
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static readonly IPEndPoint SampleLoopbackEndpoint
            = new IPEndPoint(IPAddress.Loopback, 123);

        private static IapTunnel.Profile CreateTunnelProfile()
        {
            var protocol = new Mock<IProtocol>();
            var policy = new Mock<ITransportPolicy>();

            return new IapTunnel.Profile(
                protocol.Object,
                policy.Object,
                SampleInstance,
                22);
        }

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        [Test]
        public void Statistics()
        {
            var stats = new Iap.Net.NetworkStatistics();
            stats.OnReceiveCompleted(1);
            stats.OnTransmitCompleted(3);

            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalEndpoint).Returns(SampleLoopbackEndpoint);
            listener.SetupGet(l => l.Statistics).Returns(stats);

            using (var tunnel = new IapTunnel(
                listener.Object,
                CreateTunnelProfile(),
                IapTunnelFlags.None))
            {
                Assert.That(tunnel.Statistics.BytesReceived, Is.EqualTo(3));
                Assert.That(tunnel.Statistics.BytesTransmitted, Is.EqualTo(1));
            }
        }

        [Test]
        public void LocalEndpoint()
        {
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalEndpoint).Returns(SampleLoopbackEndpoint);

            using (var tunnel = new IapTunnel(
                listener.Object,
                CreateTunnelProfile(),
                IapTunnelFlags.None))
            {
                Assert.That(
                    tunnel.LocalEndpoint, Is.EqualTo(SampleLoopbackEndpoint));
            }
        }

        [Test]
        public void Details()
        {
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalEndpoint).Returns(SampleLoopbackEndpoint);

            var profile = CreateTunnelProfile();

            using (var tunnel = new IapTunnel(
                listener.Object,
                profile,
                IapTunnelFlags.None))
            {
                Assert.AreSame(profile, tunnel.Details);
            }
        }

        [Test]
        public void TargetInstance()
        {
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalEndpoint).Returns(SampleLoopbackEndpoint);

            var profile = CreateTunnelProfile();

            using (var tunnel = new IapTunnel(
                listener.Object,
                profile,
                IapTunnelFlags.None))
            {
                Assert.That(tunnel.TargetInstance, Is.EqualTo(SampleInstance));
            }
        }

        [Test]
        public void TargetPort()
        {
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalEndpoint).Returns(SampleLoopbackEndpoint);

            var profile = CreateTunnelProfile();

            using (var tunnel = new IapTunnel(
                listener.Object,
                profile,
                IapTunnelFlags.None))
            {
                Assert.That(tunnel.TargetPort, Is.EqualTo(22));
            }
        }

        [Test]
        public void Policy()
        {
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalEndpoint).Returns(SampleLoopbackEndpoint);

            var profile = CreateTunnelProfile();

            using (var tunnel = new IapTunnel(
                listener.Object,
                profile,
                IapTunnelFlags.None))
            {
                Assert.That(tunnel.Policy, Is.EqualTo(profile.Policy));
            }
        }

        [Test]
        public void Protocol()
        {
            var policy = new Mock<ITransportPolicy>().Object;
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalEndpoint).Returns(SampleLoopbackEndpoint);

            var profile = CreateTunnelProfile();

            using (var tunnel = new IapTunnel(
                listener.Object,
                profile,
                IapTunnelFlags.None))
            {
                Assert.That(tunnel.Protocol, Is.EqualTo(profile.Protocol));
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_StopsRelay()
        {
            var token = CancellationToken.None;
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalEndpoint).Returns(SampleLoopbackEndpoint);
            listener
                .Setup(l => l.ListenAsync(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken t) => token = t)
                .Returns(Task.CompletedTask);

            using (new IapTunnel(
                listener.Object,
                CreateTunnelProfile(),
                IapTunnelFlags.None))
            {
                Assert.IsFalse(token.IsCancellationRequested);
            }

            Assert.IsTrue(token.IsCancellationRequested);
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public void Close_StopsRelay()
        {
            Task listenTask = new TaskCompletionSource<object>().Task;
            var token = CancellationToken.None;

            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalEndpoint).Returns(SampleLoopbackEndpoint);
            listener
                .Setup(l => l.ListenAsync(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken t) => token = t)
                .Returns(listenTask);

            using (var tunnel = new IapTunnel(
                listener.Object,
                CreateTunnelProfile(),
                IapTunnelFlags.None))
            {
                Assert.IsFalse(token.IsCancellationRequested);
                Assert.AreSame(listenTask, tunnel.CloseAsync());
                Assert.IsTrue(token.IsCancellationRequested);
            }
        }

        //---------------------------------------------------------------------
        // Factory.
        //---------------------------------------------------------------------

        private class DeniedAccessFactoryMock : IapTunnel.Factory
        {
            internal List<ushort> ProbedPorts { get; } = new List<ushort>();

            public DeniedAccessFactoryMock(IIapClient client) : base(client)
            {
            }

            protected internal override IIapListener CreateListener(
                ISshRelayTarget target,
                ITransportPolicy policy,
                IPEndPoint localEndpoint)
            {
                this.ProbedPorts.Add((ushort)localEndpoint.Port);
                throw new PortAccessDeniedException(localEndpoint);
            }
        }

        [Test]
        public void Factory_WhenPortAccessDenied_ThenCreateTunnelRetriesWithDifferentPorts()
        {
            var protocol = new Mock<IProtocol>();
            var policy = new Mock<ITransportPolicy>();
            var profile = new IapTunnel.Profile(
                protocol.Object,
                policy.Object,
                SampleInstance,
                22,
                null);

            var factory = new DeniedAccessFactoryMock(new Mock<IIapClient>().Object);

            Assert.Throws<PortAccessDeniedException>(
                () => factory.CreateTunnel(
                profile,
                new Mock<ISshRelayTarget>().Object,
                CancellationToken.None));

            Assert.That(factory.ProbedPorts.Count, Is.EqualTo(5));
            Assert.That(factory.ProbedPorts.Distinct().Count(), Is.EqualTo(5));
        }
    }
}
