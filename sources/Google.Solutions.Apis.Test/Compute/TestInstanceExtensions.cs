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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Compute
{
    [TestFixture]
    [UsesCloudResources]
    public class TestInstanceExtensions
    {
        [Test]
        public async Task GetInstanceLocator_WhenInstancePopulated_ThenSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);
            var instance = await client
                .GetInstanceAsync(
                    await testInstance,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var zoneLocator = instance.GetZoneLocator();
            var instanceLocator = instance.GetInstanceLocator();

            Assert.That(zoneLocator.Name, Is.EqualTo(TestProject.Zone));
            Assert.That(instanceLocator.Zone, Is.EqualTo(TestProject.Zone));

            Assert.That(instanceLocator, Is.EqualTo(await testInstance));
        }

        [Test]
        public async Task PrivateAddressReturns_WhenInstanceHasInternalIp_ThenRfc1918Ip(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);
            var instance = await client.GetInstanceAsync(
                    await testInstance,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(instance.PrimaryInternalAddress());
            CollectionAssert.Contains(
                new byte[] { 172, 192, 10 },
                instance.PrimaryInternalAddress().GetAddressBytes()[0],
                "Is RFC1918 address");
        }

        [Test]
        public async Task PublicAddress_WhenInstanceLacksPublicIp_ThenReturnsNull(
            [LinuxInstance(PublicIp = false)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);
            var instance = await client.GetInstanceAsync(
                    await testInstance,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(instance.PublicAddress());
        }

        [Test]
        public async Task PublicAddress_WhenInstanceHasPublicIp_ThenReturnsNonRfc1918Ip(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);
            var instance = await client.GetInstanceAsync(
                    await testInstance,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(instance.PublicAddress());
            CollectionAssert.DoesNotContain(
                new byte[] { 172, 192, 10 },
                instance.PublicAddress().GetAddressBytes()[0],
                "Is not a RFC1918 address");
        }
    }
}
