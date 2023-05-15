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
// Profileific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.Auth;
using Google.Solutions.IapDesktop.Core.Net.Protocol;
using Google.Solutions.IapDesktop.Core.Net.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.Net.Transport
{
    [TestFixture]
    public class TestIapTransportFactory
    {
        private static readonly UserAgent SampleUserAgent
            = new UserAgent("Test", new System.Version(1, 0));

        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static readonly TimeSpan SampleTimeout = TimeSpan.FromSeconds(10);

        private static readonly IPEndPoint LoopbackEndpoint
            = new IPEndPoint(IPAddress.Loopback, 8000);

        private static Mock<IAuthorization> CreateAuthorization()
        {
            var authz = new Mock<IAuthorization>();
            return authz;
        }

        private static IapTunnel.Profile CreateTunnelProfile(
            InstanceLocator instance,
            ushort port)
        {
            var protocol = new Mock<IProtocol>();
            protocol.SetupGet(p => p.Id).Returns("mock");

            var policy = new Mock<ISshRelayPolicy>();
            policy.SetupGet(p => p.Id).Returns("mock");

            return new IapTunnel.Profile(
                protocol.Object,
                policy.Object,
                instance,
                port,
                LoopbackEndpoint);
        }

        private static IapTunnel CreateTunnel(IapTunnel.Profile profile)
        {
            var listener = new Mock<ISshRelayListener>();
            listener.SetupGet(l => l.LocalPort).Returns(profile.LocalEndpoint.Port);
            listener.SetupGet(l => l.Statistics).Returns(new Iap.Net.ConnectionStatistics());

            return new IapTunnel(
                listener.Object,
                profile,
                IapTunnelFlags.None);
        }

        //---------------------------------------------------------------------
        // Pool.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoTransportsCreated_ThenPoolIsEmpty()
        {
            var tunnelFactory = new Mock<IapTunnel.Factory>();
            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                new Mock<IEventQueue>().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            CollectionAssert.IsEmpty(factory.Pool);
        }

        [Test]
        public void PoolIgnoresFaultedTunnels()
        {
            var validProfile = CreateTunnelProfile(SampleInstance, 22);
            var faultingProfile = CreateTunnelProfile(SampleInstance, 23);
            var tunnelFactory = new Mock<IapTunnel.Factory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    validProfile,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateTunnel(validProfile));
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    faultingProfile,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApplicationException("mock"));

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                new Mock<IEventQueue>().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            //
            // Create two tunnels, one of them faulting.
            //
            var validTransport = factory.CreateIapTransportAsync(
                validProfile.Protocol,
                validProfile.Policy,
                validProfile.TargetInstance,
                validProfile.TargetPort,
                validProfile.LocalEndpoint,
                SampleTimeout,
                CancellationToken.None);
            var faultingTransport = factory.CreateIapTransportAsync(
                 faultingProfile.Protocol,
                 faultingProfile.Policy,
                 faultingProfile.TargetInstance,
                 faultingProfile.TargetPort,
                 faultingProfile.LocalEndpoint,
                 SampleTimeout,
                 CancellationToken.None);

            var pool = factory.Pool;
            Assert.AreEqual(1, pool.Count());

            validTransport.Result.Dispose();
        }

        [Test]
        public void PoolIgnoresIncompleteTunnels()
        {
            var validProfile = CreateTunnelProfile(SampleInstance, 22);
            var tunnelTask = new TaskCompletionSource<IapTunnel>();
            var tunnelFactory = new Mock<IapTunnel.Factory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    validProfile,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Returns(tunnelTask.Task); // Task not complete!

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                new Mock<IEventQueue>().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            var validButIncompleteTransport = factory.CreateIapTransportAsync(
                validProfile.Protocol,
                validProfile.Policy,
                validProfile.TargetInstance,
                validProfile.TargetPort,
                validProfile.LocalEndpoint,
                SampleTimeout,
                CancellationToken.None);

            CollectionAssert.IsEmpty(factory.Pool);
        }

        //---------------------------------------------------------------------
        // CreateIapTransport - pooling.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMatchFoundInPoolButTunnelFaulted_ThenCreateIapTransportCreatesNewTunnel()
        {
            var faultingProfile = CreateTunnelProfile(SampleInstance, 23);
            var tunnelFactory = new Mock<IapTunnel.Factory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    It.IsAny<IapTunnel.Profile>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApplicationException("mock"));

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                new Mock<IEventQueue>().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            var faultingTransport1 = factory.CreateIapTransportAsync(
                 faultingProfile.Protocol,
                 faultingProfile.Policy,
                 faultingProfile.TargetInstance,
                 faultingProfile.TargetPort,
                 faultingProfile.LocalEndpoint,
                 SampleTimeout,
                 CancellationToken.None);

            //
            // Await task to make sure it's really faulted before we make
            // the next request.
            //
            ExceptionAssert.ThrowsAggregateException<ApplicationException>(
                () => faultingTransport1.Wait());

            var faultingTransport2 = factory.CreateIapTransportAsync(
                 faultingProfile.Protocol,
                 faultingProfile.Policy,
                 faultingProfile.TargetInstance,
                 faultingProfile.TargetPort,
                 faultingProfile.LocalEndpoint,
                 SampleTimeout,
                 CancellationToken.None);

            Assert.AreNotEqual(faultingTransport1, faultingTransport2);

            tunnelFactory
                .Verify(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    It.IsAny<IapTunnel.Profile>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public void WhenMatchFoundInPoolButTunnelNotCompletedYet_ThenCreateIapTransportReturnsPooledTunnel()
        {
            var validProfile = CreateTunnelProfile(SampleInstance, 22);
            var tunnelTask = new TaskCompletionSource<IapTunnel>();
            var tunnelFactory = new Mock<IapTunnel.Factory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    validProfile,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Returns(tunnelTask.Task); // Task not complete!

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                new Mock<IEventQueue>().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            var validButIncompleteTransport1 = factory.CreateIapTransportAsync(
                validProfile.Protocol,
                validProfile.Policy,
                validProfile.TargetInstance,
                validProfile.TargetPort,
                validProfile.LocalEndpoint,
                SampleTimeout,
                CancellationToken.None);
            var validButIncompleteTransport2 = factory.CreateIapTransportAsync(
                validProfile.Protocol,
                validProfile.Policy,
                validProfile.TargetInstance,
                validProfile.TargetPort,
                validProfile.LocalEndpoint,
                SampleTimeout,
                CancellationToken.None);

            Assert.AreNotSame(validButIncompleteTransport1, validButIncompleteTransport2);

            tunnelFactory
                .Verify(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    It.IsAny<IapTunnel.Profile>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WhenMatchFoundInPool_ThenCreateIapTransportReturnsPooledTunnel()
        {
            var validProfile = CreateTunnelProfile(SampleInstance, 22);
            var tunnelTask = new TaskCompletionSource<IapTunnel>();
            var tunnelFactory = new Mock<IapTunnel.Factory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    validProfile,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateTunnel(validProfile));

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                new Mock<IEventQueue>().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            var transport1 = await factory
                .CreateIapTransportAsync(
                    validProfile.Protocol,
                    validProfile.Policy,
                    validProfile.TargetInstance,
                    validProfile.TargetPort,
                    validProfile.LocalEndpoint,
                    SampleTimeout,
                    CancellationToken.None)
                .ConfigureAwait(false);
            var transport2 = await factory
                .CreateIapTransportAsync(
                    validProfile.Protocol,
                    validProfile.Policy,
                    validProfile.TargetInstance,
                    validProfile.TargetPort,
                    validProfile.LocalEndpoint,
                    SampleTimeout,
                    CancellationToken.None)
                .ConfigureAwait(false);

            //
            // Two different transports that use the same tunnel.
            //
            Assert.AreNotSame(transport1, transport2);
            Assert.AreSame(
                ((IapTransportFactory.Transport)transport1).Tunnel,
                ((IapTransportFactory.Transport)transport2).Tunnel);

            transport1.Dispose();
            transport2.Dispose();
        }

        //---------------------------------------------------------------------
        // CreateIapTransport - events.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenTunnelCreatedOrClosed_ThenCreateIapTransportPublishesEvent()
        {
            var eventQueue = new Mock<IEventQueue>();

            var validProfile = CreateTunnelProfile(SampleInstance, 22);
            var tunnelFactory = new Mock<IapTunnel.Factory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    validProfile,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateTunnel(validProfile));

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                eventQueue.Object,
                SampleUserAgent,
                tunnelFactory.Object);

            using (var transport = await factory
                .CreateIapTransportAsync(
                    validProfile.Protocol,
                    validProfile.Policy,
                    validProfile.TargetInstance,
                    validProfile.TargetPort,
                    validProfile.LocalEndpoint,
                    SampleTimeout,
                    CancellationToken.None)
                .ConfigureAwait(false))
            {
                eventQueue.Verify(
                    q => q.Publish(It.IsAny<TunnelEvents.TunnelCreated>()),
                    Times.Once);

                eventQueue.Verify(
                    q => q.Publish(It.IsAny<TunnelEvents.TunnelClosed>()),
                    Times.Never);
            }

            eventQueue.Verify(
                q => q.Publish(It.IsAny<TunnelEvents.TunnelClosed>()),
                Times.Once);
        }
    }
}
