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
using Google.Solutions.Apis;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
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
            var factory = new Mock<IIapTransportFactory>();
            factory
                .Setup(t => t.CreateTransportAsync(
                    SshProtocol.Protocol,
                    It.IsAny<ITransportPolicy>(),
                    SampleInstance,
                    22,
                    It.IsAny<IPEndPoint>(),
                    timeout,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SshRelayDeniedException("mock"));

            ExceptionAssert.ThrowsAggregateException<TransportFailedException>(
                () => Transport.CreateIapTransportAsync(
                    factory.Object,
                    SshProtocol.Protocol,
                    SampleInstance,
                    22,
                    timeout).Wait());
        }

        [Test]
        public void WhenNetworkStreamClosedException_ThenCreateIapTransportThrowsException()
        {
            var timeout = TimeSpan.FromMinutes(1);
            var factory = new Mock<IIapTransportFactory>();
            factory
                .Setup(t => t.CreateTransportAsync(
                    SshProtocol.Protocol,
                    It.IsAny<ITransportPolicy>(),
                    SampleInstance,
                    22,
                    It.IsAny<IPEndPoint>(),
                    timeout,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NetworkStreamClosedException("mock"));

            ExceptionAssert.ThrowsAggregateException<TransportFailedException>(
                () => Transport.CreateIapTransportAsync(
                    factory.Object,
                    SshProtocol.Protocol,
                    SampleInstance,
                    22,
                    timeout).Wait());
        }

        [Test]
        public void WhenWebSocketConnectionDeniedException_ThenCreateIapTransportThrowsException()
        {
            var timeout = TimeSpan.FromMinutes(1);
            var factory = new Mock<IIapTransportFactory>();
            factory
                .Setup(t => t.CreateTransportAsync(
                    SshProtocol.Protocol,
                    It.IsAny<ITransportPolicy>(),
                    SampleInstance,
                    22,
                    It.IsAny<IPEndPoint>(),
                    timeout,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebSocketConnectionDeniedException());

            ExceptionAssert.ThrowsAggregateException<TransportFailedException>(
                () => Transport.CreateIapTransportAsync(
                    factory.Object,
                    SshProtocol.Protocol,
                    SampleInstance,
                    22,
                    timeout).Wait());
        }

        [Test]
        public async Task WhenTunnelCreated_ThenCreateIapTransportReturnsTransport()
        {
            var timeout = TimeSpan.FromMinutes(1);
            var factory = new Mock<IIapTransportFactory>();
            factory
                .Setup(t => t.CreateTransportAsync(
                    SshProtocol.Protocol,
                    It.IsAny<ITransportPolicy>(),
                    SampleInstance,
                    22,
                    It.IsAny<IPEndPoint>(),
                    timeout,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<ITransport>().Object);

            var transport = await Transport
                .CreateIapTransportAsync(
                    factory.Object,
                    SshProtocol.Protocol,
                    SampleInstance,
                    22,
                    timeout)
                .ConfigureAwait(false);

            Assert.IsNotNull(transport);
        }

        //---------------------------------------------------------------------
        // CreateVpcTransportAsync.
        //---------------------------------------------------------------------

        [Test]
        public void WhenAddressLookupFails_ThenCreateVpcTransportThrowsException()
        {
            var addressResolver = new Mock<IAddressResolver>();
            addressResolver.Setup(
                r => r.GetAddressAsync(
                    SampleInstance,
                    NetworkInterfaceType.PrimaryInternal,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("mock", null));

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => Transport.CreateVpcTransportAsync(
                    new DirectTransportFactory(),
                    SshProtocol.Protocol,
                    addressResolver.Object,
                    SampleInstance,
                    22,
                    CancellationToken.None).Wait());
        }
    }
}
