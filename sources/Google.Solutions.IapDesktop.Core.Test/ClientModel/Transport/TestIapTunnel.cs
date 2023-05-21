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
            var stats = new Iap.Net.ConnectionStatistics();
            stats.OnReceiveCompleted(1);
            stats.OnTransmitCompleted(3);

            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalPort).Returns(123);
            listener.SetupGet(l => l.Statistics).Returns(stats);

            using (var tunnel = new IapTunnel(
                listener.Object,
                CreateTunnelProfile(),
                IapTunnelFlags.None))
            {
                Assert.AreEqual(3, tunnel.Statistics.BytesReceived);
                Assert.AreEqual(1, tunnel.Statistics.BytesTransmitted);
            }
        }

        [Test]
        public void LocalEndpoint()
        {
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalPort).Returns(123);

            using (var tunnel = new IapTunnel(
                listener.Object,
                CreateTunnelProfile(),
                IapTunnelFlags.None))
            {
                Assert.AreEqual(
                    new IPEndPoint(IPAddress.Loopback, 123), 
                    tunnel.LocalEndpoint);
            }
        }

        [Test]
        public void Details()
        {
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalPort).Returns(123);

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
            listener.SetupGet(l => l.LocalPort).Returns(123);

            var profile = CreateTunnelProfile();

            using (var tunnel = new IapTunnel(
                listener.Object,
                profile,
                IapTunnelFlags.None))
            {
                Assert.AreEqual(SampleInstance, tunnel.TargetInstance);
            }
        }

        [Test]
        public void TargetPort()
        {
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalPort).Returns(123);

            var profile = CreateTunnelProfile();

            using (var tunnel = new IapTunnel(
                listener.Object,
                profile,
                IapTunnelFlags.None))
            {
                Assert.AreEqual(22, tunnel.TargetPort);
            }
        }

        [Test]
        public void Policy()
        {
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalPort).Returns(123);

            var profile = CreateTunnelProfile();

            using (var tunnel = new IapTunnel(
                listener.Object,
                profile,
                IapTunnelFlags.None))
            {
                Assert.AreEqual(profile.Policy, tunnel.Policy);
            }
        }

        [Test]
        public void Protocol()
        {
            var policy = new Mock<ITransportPolicy>().Object;
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalPort).Returns(123);

            var profile = CreateTunnelProfile();

            using (var tunnel = new IapTunnel(
                listener.Object,
                profile,
                IapTunnelFlags.None))
            {
                Assert.AreEqual(profile.Protocol, tunnel.Protocol);
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void DisposeStopsRelay()
        {
            CancellationToken token;
            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalPort).Returns(123);
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
        public void CloseStopsRelay()
        {
            Task listenTask = new TaskCompletionSource<object>().Task;
            CancellationToken token;

            var listener = new Mock<IIapListener>();
            listener.SetupGet(l => l.LocalPort).Returns(123);
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
    }
}
