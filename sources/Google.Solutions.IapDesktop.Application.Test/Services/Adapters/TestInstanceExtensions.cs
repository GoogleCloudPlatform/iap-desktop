﻿//
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

using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestInstanceExtensions : FixtureBase
    {
        [Test]
        public async Task WhenInstancePopulated_ThenGetInstanceLocatorSucceeds(
            [LinuxInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            var adapter = new ComputeEngineAdapter(await credential.GetCredentialAsync());
            var instance = await adapter.GetInstanceAsync(await testInstance.GetInstanceAsync());

            var zoneLocator = instance.GetZoneLocator();
            var instanceLocator = instance.GetInstanceLocator();

            Assert.AreEqual(TestProject.Zone, zoneLocator.Name);
            Assert.AreEqual(TestProject.Zone, instanceLocator.Zone);

            Assert.AreEqual(await testInstance.GetInstanceAsync(), instanceLocator);
        }
    }
}
