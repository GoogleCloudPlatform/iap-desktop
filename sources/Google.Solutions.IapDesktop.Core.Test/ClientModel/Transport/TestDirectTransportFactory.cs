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

using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
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
    public class TestDirectTransportFactory
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // CreateTransport.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateTransport()
        {
            var address = IPAddress.Parse("10.0.0.1");

            var resolver = new Mock<IAddressResolver>();
            resolver.Setup(
                r => r.GetAddressAsync(
                    SampleInstance,
                    NetworkInterfaceType.PrimaryInternal,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(address);

            var factory = new DirectTransportFactory(resolver.Object);

            var protocol = new Mock<IProtocol>();

            using (var transport = await factory
                .CreateTransportAsync(
                    protocol.Object,
                    SampleInstance,
                    NetworkInterfaceType.PrimaryInternal,
                    22,
                    CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.AreSame(protocol.Object, transport.Protocol);
                Assert.AreSame(address, transport.Endpoint.Address);
                Assert.That(transport.Endpoint.Port, Is.EqualTo(22));
                Assert.AreSame(SampleInstance, transport.Target);
            }
        }
    }
}
