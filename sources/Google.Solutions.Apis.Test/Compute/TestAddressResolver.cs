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
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Compute
{
    [TestFixture]
    public class TestAddressResolver
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // GetAddressAsync.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInstanceLookupFails_ThenGetPrimaryInternalAddressThrowsException()
        {
            var computeClient = new Mock<IComputeEngineClient>();
            computeClient
                .Setup(a => a.GetInstanceAsync(SampleInstance, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceNotFoundException("mock", null));

            var resolver = new AddressResolver(computeClient.Object);

            ExceptionAssert.ThrowsAggregateException<ResourceNotFoundException>(
                () => resolver.GetAddressAsync(
                    SampleInstance,
                    NetworkInterfaceType.PrimaryInternal,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // GetAddressAsync - PrimaryInternal.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInstanceLacksInternalIp_ThenGetPrimaryInternalAddressThrowsException()
        {
            var computeClient = new Mock<IComputeEngineClient>();
            computeClient
                .Setup(a => a.GetInstanceAsync(SampleInstance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Instance());

            var resolver = new AddressResolver(computeClient.Object);

            ExceptionAssert.ThrowsAggregateException<AddressNotFoundException>(
                () => resolver.GetAddressAsync(
                    SampleInstance,
                    NetworkInterfaceType.PrimaryInternal,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenInstanceHasMultipleNics_ThenGetPrimaryInternalAddressReturnsEndpointForNic0()
        {
            var instance = new Instance()
            {
                NetworkInterfaces = new[]
                {
                    new NetworkInterface()
                    {
                        Name = "nic1",
                        StackType = "IPV4_ONLY",
                        NetworkIP = "20.21.22.23"
                    },
                    new NetworkInterface()
                    {
                        Name = "nic0",
                        StackType = "IPV4_ONLY",
                        NetworkIP = "10.11.12.13"
                    }
                }
            };

            var computeClient = new Mock<IComputeEngineClient>();
            computeClient
                .Setup(a => a.GetInstanceAsync(SampleInstance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(instance);

            var resolver = new AddressResolver(computeClient.Object);
            var address = await resolver
                .GetAddressAsync(
                    SampleInstance,
                    NetworkInterfaceType.PrimaryInternal,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("10.11.12.13", address.ToString());
        }

        [Test]
        public async Task WhenInstanceHasDualStackNic_ThenGetPrimaryInternalAddressReturnsIpv4()
        {
            var instance = new Instance()
            {
                NetworkInterfaces = new[]
                {
                    new NetworkInterface()
                    {
                        Name = "nic0",
                        StackType = "IPV4_IPV6",
                        Ipv6AccessType = "INTERNAL",
                        NetworkIP = "10.11.12.13",
                        Ipv6Address = "fd20:2d:fc4b:3000:0:0:0:0",
                        AccessConfigs = new []
                        {
                            new AccessConfig()
                            {
                                Type = "ONE_TO_ONE_NAT",
                                Name = "External NAT",
                                NatIP = "1.1.1.1"
                            }
                        }
                    }
                }
            };

            var computeClient = new Mock<IComputeEngineClient>();
            computeClient
                .Setup(a => a.GetInstanceAsync(SampleInstance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(instance);

            var resolver = new AddressResolver(computeClient.Object);
            var address = await resolver
                .GetAddressAsync(
                    SampleInstance,
                    NetworkInterfaceType.PrimaryInternal,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("10.11.12.13", address.ToString());
        }

        //---------------------------------------------------------------------
        // GetAddressAsync - External.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInstanceHasNoExternalAddress_ThenGeExternalAddressThrowsException()
        {
            var instance = new Instance()
            {
                NetworkInterfaces = new[]
                {
                    new NetworkInterface()
                    {
                        Name = "nic0",
                        StackType = "IPV4_ONLY",
                        NetworkIP = "10.11.12.13"
                    }
                }
            };

            var computeClient = new Mock<IComputeEngineClient>();
            computeClient
                .Setup(a => a.GetInstanceAsync(SampleInstance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(instance);

            var resolver = new AddressResolver(computeClient.Object);

            ExceptionAssert.ThrowsAggregateException<AddressNotFoundException>(
                () => resolver.GetAddressAsync(
                    SampleInstance,
                    NetworkInterfaceType.External,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenInstanceHasExternalAddress_ThenGetExternalAddressReturnsIpv4()
        {
            var instance = new Instance()
            {
                NetworkInterfaces = new[]
                {
                    new NetworkInterface()
                    {
                        Name = "nic0",
                        StackType = "IPV4_IPV6",
                        Ipv6AccessType = "INTERNAL",
                        NetworkIP = "10.11.12.13",
                        Ipv6Address = "fd20:2d:fc4b:3000:0:0:0:0",
                        AccessConfigs = new []
                        {
                            new AccessConfig()
                            {
                                Type = "ONE_TO_ONE_NAT",
                                Name = "External NAT",
                                NatIP = "1.1.1.1"
                            }
                        }
                    }
                }
            };

            var computeClient = new Mock<IComputeEngineClient>();
            computeClient
                .Setup(a => a.GetInstanceAsync(SampleInstance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(instance);

            var resolver = new AddressResolver(computeClient.Object);
            var address = await resolver
                .GetAddressAsync(
                    SampleInstance,
                    NetworkInterfaceType.External,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("1.1.1.1", address.ToString());
        }
    }
}
