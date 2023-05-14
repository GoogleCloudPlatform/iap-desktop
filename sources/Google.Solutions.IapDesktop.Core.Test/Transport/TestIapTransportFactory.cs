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
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.Auth;
using Google.Solutions.IapDesktop.Core.Transport;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.Transport
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

        private static IapTransportFactory.TunnelSpecification CreateTunnelSpecification(
            InstanceLocator instance,
            ushort port)
        {
            var protocol = new Mock<IProtocol>();
            protocol.SetupGet(p => p.Id).Returns("mock");

            var policy = new Mock<ISshRelayPolicy>();
            policy.SetupGet(p => p.Id).Returns("mock");

            return new IapTransportFactory.TunnelSpecification(
                protocol.Object,
                policy.Object,
                instance,
                port,
                LoopbackEndpoint);
        }

        private static IapTransportFactory.Tunnel CreateTunnel(
            IapTransportFactory.TunnelSpecification specification)
        {
            var listener = new Mock<ISshRelayListener>();
            listener.SetupGet(l => l.LocalPort).Returns(specification.LocalEndpoint.Port);
            listener.SetupGet(l => l.Statistics).Returns(new Iap.Net.ConnectionStatistics());

            return new IapTransportFactory.Tunnel(
                listener.Object,
                LoopbackEndpoint,
                IapTunnelFlags.None);
        }

        //---------------------------------------------------------------------
        // Pool.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoTransportsCreated_ThenPoolIsEmpty()
        {
            var tunnelFactory = new Mock<IapTransportFactory.TunnelFactory>();
            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            CollectionAssert.IsEmpty(factory.Pool);
        }

        [Test]
        public void PoolIgnoresFaultedTunnels()
        {
            var validSpec = CreateTunnelSpecification(SampleInstance, 22);
            var faultingSpec = CreateTunnelSpecification(SampleInstance, 23);
            var tunnelFactory = new Mock<IapTransportFactory.TunnelFactory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    validSpec,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateTunnel(validSpec));
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    faultingSpec,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApplicationException("mock"));

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            //
            // Create two tunnels, one of them faulting.
            //
            var validTransport = factory.CreateIapTransportAsync(
                validSpec.Protocol,
                validSpec.Policy,
                validSpec.TargetInstance,
                validSpec.TargetPort,
                validSpec.LocalEndpoint,
                SampleTimeout,
                CancellationToken.None);
            var faultingTransport = factory.CreateIapTransportAsync(
                 faultingSpec.Protocol,
                 faultingSpec.Policy,
                 faultingSpec.TargetInstance,
                 faultingSpec.TargetPort,
                 faultingSpec.LocalEndpoint,
                 SampleTimeout,
                 CancellationToken.None);

            var pool = factory.Pool;
            Assert.AreEqual(1, pool.Count());

            validTransport.Result.Dispose();
        }

        [Test]
        public void PoolIgnoresIncompleteTunnels()
        {
            var validSpec = CreateTunnelSpecification(SampleInstance, 22);
            var tunnelTask = new TaskCompletionSource<IapTransportFactory.Tunnel>();
            var tunnelFactory = new Mock<IapTransportFactory.TunnelFactory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    validSpec,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Returns(tunnelTask.Task); // Task not complete!

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            var validButIncompleteTransport = factory.CreateIapTransportAsync(
                validSpec.Protocol,
                validSpec.Policy,
                validSpec.TargetInstance,
                validSpec.TargetPort,
                validSpec.LocalEndpoint,
                SampleTimeout,
                CancellationToken.None);

            CollectionAssert.IsEmpty(factory.Pool);
        }

        //---------------------------------------------------------------------
        // CreateIapTransport.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMatchFoundInPoolButTunnelFaulted_ThenCreateIapTransportCreatesNewTunnel()
        {
            var faultingSpec = CreateTunnelSpecification(SampleInstance, 23);
            var tunnelFactory = new Mock<IapTransportFactory.TunnelFactory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    It.IsAny<IapTransportFactory.TunnelSpecification>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApplicationException("mock"));

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            var faultingTransport1 = factory.CreateIapTransportAsync(
                 faultingSpec.Protocol,
                 faultingSpec.Policy,
                 faultingSpec.TargetInstance,
                 faultingSpec.TargetPort,
                 faultingSpec.LocalEndpoint,
                 SampleTimeout,
                 CancellationToken.None);

            //
            // Await task to make sure it's really faulted before we make
            // the next request.
            //
            ExceptionAssert.ThrowsAggregateException<ApplicationException>(
                () => faultingTransport1.Wait());

            var faultingTransport2 = factory.CreateIapTransportAsync(
                 faultingSpec.Protocol,
                 faultingSpec.Policy,
                 faultingSpec.TargetInstance,
                 faultingSpec.TargetPort,
                 faultingSpec.LocalEndpoint,
                 SampleTimeout,
                 CancellationToken.None);

            Assert.AreNotEqual(faultingTransport1, faultingTransport2);

            tunnelFactory
                .Verify(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    It.IsAny<IapTransportFactory.TunnelSpecification>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public void WhenMatchFoundInPoolButTunnelNotCompletedYet_ThenCreateIapTransportReturnsPooledTunnel()
        {
            var validSpec = CreateTunnelSpecification(SampleInstance, 22);
            var tunnelTask = new TaskCompletionSource<IapTransportFactory.Tunnel>();
            var tunnelFactory = new Mock<IapTransportFactory.TunnelFactory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    validSpec,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Returns(tunnelTask.Task); // Task not complete!

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            var validButIncompleteTransport1 = factory.CreateIapTransportAsync(
                validSpec.Protocol,
                validSpec.Policy,
                validSpec.TargetInstance,
                validSpec.TargetPort,
                validSpec.LocalEndpoint,
                SampleTimeout,
                CancellationToken.None);
            var validButIncompleteTransport2 = factory.CreateIapTransportAsync(
                validSpec.Protocol,
                validSpec.Policy,
                validSpec.TargetInstance,
                validSpec.TargetPort,
                validSpec.LocalEndpoint,
                SampleTimeout,
                CancellationToken.None);

            Assert.AreNotSame(validButIncompleteTransport1, validButIncompleteTransport2);

            tunnelFactory
                .Verify(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    It.IsAny<IapTransportFactory.TunnelSpecification>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WhenMatchFoundInPool_ThenCreateIapTransportReturnsPooledTunnel()
        {
            var validSpec = CreateTunnelSpecification(SampleInstance, 22);
            var tunnelTask = new TaskCompletionSource<IapTransportFactory.Tunnel>();
            var tunnelFactory = new Mock<IapTransportFactory.TunnelFactory>();
            tunnelFactory
                .Setup(f => f.CreateTunnelAsync(
                    It.IsAny<IAuthorization>(),
                    SampleUserAgent,
                    validSpec,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateTunnel(validSpec));

            var factory = new IapTransportFactory(
                CreateAuthorization().Object,
                SampleUserAgent,
                tunnelFactory.Object);

            var transport1 = await factory
                .CreateIapTransportAsync(
                    validSpec.Protocol,
                    validSpec.Policy,
                    validSpec.TargetInstance,
                    validSpec.TargetPort,
                    validSpec.LocalEndpoint,
                    SampleTimeout,
                    CancellationToken.None)
                .ConfigureAwait(false);
            var transport2 = await factory
                .CreateIapTransportAsync(
                    validSpec.Protocol,
                    validSpec.Policy,
                    validSpec.TargetInstance,
                    validSpec.TargetPort,
                    validSpec.LocalEndpoint,
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
    }
}
