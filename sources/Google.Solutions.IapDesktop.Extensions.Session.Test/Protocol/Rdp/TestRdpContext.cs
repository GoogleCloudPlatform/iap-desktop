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


using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Rdp
{
    [TestFixture]
    public class TestRdpContext
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // AuthorizeCredential.
        //---------------------------------------------------------------------

        [Test]
        public void AuthorizeCredential_ReturnsCredential()
        {
            var credential = new RdpCredential("user", null, null);
            var context = new RdpContext(
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                SampleInstance,
                credential,
                RdpParameters.ParameterSources.Inventory);

            Assert.That(
                context.AuthorizeCredentialAsync(CancellationToken.None).Result, Is.SameAs(credential));
        }

        //---------------------------------------------------------------------
        // ConnectTransport.
        //---------------------------------------------------------------------

        [Test]
        public async Task ConnectTransport_WhenTransportTypeIsIap()
        {
            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Endpoint).Returns(new IPEndPoint(IPAddress.Loopback, 123));

            var factory = new Mock<IIapTransportFactory>();
            factory
                .Setup(b => b.CreateTransportAsync(
                    RdpProtocol.Protocol,
                    It.IsAny<ITransportPolicy>(),
                    SampleInstance,
                    3389,
                    It.IsAny<IPEndPoint>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(transport.Object);

            var context = new RdpContext(
                factory.Object,
                new Mock<IDirectTransportFactory>().Object,
                SampleInstance,
                RdpCredential.Empty,
                RdpParameters.ParameterSources.Inventory);
            context.Parameters.TransportType = SessionTransportType.IapTunnel;

            var rdpTransport = await context
                .ConnectTransportAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(rdpTransport, Is.SameAs(transport.Object));
        }

        [Test]
        public async Task ConnectTransport_WhenTransportTypeIsVpcInternal()
        {
            var transport = new Mock<ITransport>();
            var factory = new Mock<IDirectTransportFactory>();
            factory
                .Setup(b => b.CreateTransportAsync(
                    RdpProtocol.Protocol,
                    SampleInstance,
                    NetworkInterfaceType.PrimaryInternal,
                    3389,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(transport.Object);

            var context = new RdpContext(
                new Mock<IIapTransportFactory>().Object,
                factory.Object,
                SampleInstance,
                RdpCredential.Empty,
                RdpParameters.ParameterSources.Inventory);
            context.Parameters.TransportType = SessionTransportType.Vpc;

            var rdpTransport = await context
                .ConnectTransportAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(rdpTransport, Is.SameAs(transport.Object));
        }
    }
}
