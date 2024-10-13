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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory;
using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.PackageInventory;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows.PackageInventory
{
    [TestFixture]
    public class TestPackageInventoryModel : ApplicationFixtureBase
    {
        [Test]
        public async Task Packages_WhenGuestAttributesDisabledByPolicy_ThenIsEmpty()
        {
            var packageInventory = new Mock<IGuestOsInventory>();
            packageInventory.Setup(s => s.GetInstanceInventoryAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<CancellationToken>()))
                .Throws(new GoogleApiException("mock", "mock")
                {
                    Error = new Google.Apis.Requests.RequestError()
                    {
                        Code = 412
                    }
                });


            var node = new Mock<IProjectModelZoneNode>();
            node.SetupGet(n => n.Zone).Returns(new ZoneLocator("project-1", "zone-1"));
            node.SetupGet(n => n.DisplayName).Returns("zone-1");

            var model = await PackageInventoryModel
                .LoadAsync(
                    packageInventory.Object,
                    PackageInventoryType.AvailablePackages,
                    node.Object,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.IsFalse(model?.IsInventoryDataAvailable);
            Assert.IsNotNull(model?.Packages);
            Assert.IsFalse(model?.Packages.Any());
        }
    }
}
