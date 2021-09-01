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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestInstanceExtensions : ApplicationFixtureBase
    {
        [Test]
        public async Task WhenInstancePopulated_ThenGetInstanceLocatorSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);
            var instance = await adapter.GetInstanceAsync(
                await testInstance,
                CancellationToken.None);

            var zoneLocator = instance.GetZoneLocator();
            var instanceLocator = instance.GetInstanceLocator();

            Assert.AreEqual(TestProject.Zone, zoneLocator.Name);
            Assert.AreEqual(TestProject.Zone, instanceLocator.Zone);

            Assert.AreEqual(await testInstance, instanceLocator);
        }

        [Test]
        public async Task WhenInstanceHasInternalIp_ThenPrivateAddressReturnsRfc1918Ip(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);
            var instance = await adapter.GetInstanceAsync(
                    await testInstance,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(instance.InternalAddress());
            CollectionAssert.Contains(
                new byte[] { 172, 192, 10 },
                instance.InternalAddress().GetAddressBytes()[0],
                "Is RFC1918 address");
        }

        [Test]
        public async Task WhenInstanceLacksPublicIp_ThenPublicAddressReturnsNull(
            [LinuxInstance(PublicIp = false)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);
            var instance = await adapter.GetInstanceAsync(
                    await testInstance,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(instance.PublicAddress());
        }

        [Test]
        public async Task WhenInstanceHasPublicIp_ThenPublicAddressReturnsNonRfc1918Ip(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new ComputeEngineAdapter(await credential);
            var instance = await adapter.GetInstanceAsync(
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
