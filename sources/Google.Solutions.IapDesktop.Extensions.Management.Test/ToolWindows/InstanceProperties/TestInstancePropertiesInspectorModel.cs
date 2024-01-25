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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory;
using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.InstanceProperties;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows.InstanceProperties
{
    [TestFixture]
    [UsesCloudResources]
    public class TestInstancePropertiesInspectorModel : ApplicationFixtureBase
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        [Test]
        public async Task WhenLoadAsyncCompletes_ThenPropertiesArePopulated(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;

            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var model = await InstancePropertiesInspectorModel
                .LoadAsync(
                    await testInstance,
                    computeClient,
                    new Management.GuestOs.Inventory.GuestOsInventory(computeClient),
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreEqual(locator.Name, model.InstanceName);
            Assert.IsNull(model.Hostname);
            Assert.AreEqual("RUNNING", model.Status);
            Assert.IsNotNull(model.InternalIp);
            Assert.IsNotNull(model.ExternalIp);
            Assert.IsNotNull(model.Licenses);
            Assert.AreEqual(model.IsOsInventoryInformationPopulated
                ? FeatureFlag.Enabled : FeatureFlag.Disabled, model.OsInventory);
            Assert.AreEqual(FeatureFlag.Disabled, model.Diagnostics);
            Assert.AreEqual(FeatureFlag.Enabled, model.GuestAttributes);
            Assert.IsFalse(model.IsSoleTenant);
            Assert.AreEqual(WindowsInstanceAttribute.DefaultMachineType, model.MachineType);
            Assert.IsNull(model.Tags);
        }

        [Test]
        public async Task WhenGuestAttributesDisabledByPolicy_ThenOsPropertiesAreNull(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var locator = await testInstance;

            var computeClient = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);
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

            var model = await InstancePropertiesInspectorModel
                .LoadAsync(
                    await testInstance,
                    computeClient,
                    packageInventory.Object,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.AreEqual(locator.Name, model.InstanceName);
            Assert.AreEqual("RUNNING", model.Status);

            Assert.IsFalse(model.IsOsInventoryInformationPopulated);
            Assert.IsNull(model.Architecture);
            Assert.IsNull(model.KernelVersion);
            Assert.IsNull(model.OperatingSystemFullName);
            Assert.IsNull(model.OperatingSystemVersion);
        }

        [Test]
        public void WhenMetadataIsEmpty_ThenDefaultsAreApplied()
        {
            var project = new Project();
            var instance = new Instance();

            var model = new InstancePropertiesInspectorModel(
                SampleLocator,
                project,
                instance,
                null);
            Assert.AreEqual(FeatureFlag.Disabled, model.OsInventory);
            Assert.AreEqual(FeatureFlag.Disabled, model.Diagnostics);
            Assert.AreEqual(FeatureFlag.Disabled, model.SerialPortAccess);
            Assert.AreEqual(FeatureFlag.Disabled, model.GuestAttributes);
        }

        [Test]
        public void WhenGuestOsFieldsAreNull_ThenDefaultsAreApplied()
        {
            var model = new InstancePropertiesInspectorModel(
                SampleLocator,
                new Project(),
                new Instance(),
                new GuestOsInfo(
                    SampleLocator,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null));
            Assert.IsNull(model.Architecture);
            Assert.IsNull(model.KernelVersion);
            Assert.IsNull(model.OperatingSystemFullName);
            Assert.IsNull(model.OperatingSystemVersion);
        }

        [Test]
        public void WhenFlagSetInCommonInstanceMetadataAndInstanceMetadata_ThenInstanceMetadataPrevails()
        {
            var project = new Project()
            {
                CommonInstanceMetadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "VmDnsSetting",
                            Value = "ZonalOnly"
                        },
                        new Metadata.ItemsData()
                        {
                            Key = "enable-oslogin",
                            Value = "true"
                        },
                        new Metadata.ItemsData()
                        {
                            Key = "enable-oslogin-2fa",
                            Value = "true"
                        },
                        new Metadata.ItemsData()
                        {
                            Key = "block-project-ssh-keys",
                            Value = "false"
                        }
                    }
                }
            };

            var instance = new Instance
            {
                Metadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "VmDnsSetting",
                            Value = "ZonalPreferred"
                        },
                        new Metadata.ItemsData()
                        {
                            Key = "enable-oslogin",
                            Value = "false"
                        },
                        new Metadata.ItemsData()
                        {
                            Key = "enable-oslogin-2fa",
                            Value = "false"
                        },
                        new Metadata.ItemsData()
                        {
                            Key = "block-project-ssh-keys",
                            Value = "true"
                        }
                    }
                }
            };

            var model = new InstancePropertiesInspectorModel(
                SampleLocator,
                project,
                instance,
                null);
            Assert.AreEqual(FeatureFlag.Disabled, model.OsLogin);
            Assert.AreEqual(FeatureFlag.Disabled, model.OsLogin2FA);
            Assert.AreEqual(FeatureFlag.Enabled, model.BlockProjectSshKeys);
            Assert.AreEqual(FeatureFlag.Disabled, model.OsLoginWithSecurityKey);
        }

        [Test]
        public void WhenFlagSetInInstanceMetadataOnly_ThenInstanceMetadataPrevails()
        {
            var project = new Project();
            var instance = new Instance
            {
                Metadata = new Metadata()
                {
                    Items = new[]
                    {
                        new Metadata.ItemsData()
                        {
                            Key = "VmDnsSetting",
                            Value = "ZonalPreferred"
                        },
                        new Metadata.ItemsData()
                        {
                            Key = "enable-oslogin-2fa",
                            Value = "true"
                        },
                        new Metadata.ItemsData()
                        {
                            Key = "enable-oslogin-sk",
                            Value = "true"
                        }
                    }
                }
            };

            var model = new InstancePropertiesInspectorModel(
                SampleLocator,
                project,
                instance,
                null);
            Assert.AreEqual(FeatureFlag.Enabled, model.OsLogin2FA);
            Assert.AreEqual(FeatureFlag.Enabled, model.OsLoginWithSecurityKey);
        }

        [Test]
        public void InternalZonalDnsName()
        {
            var project = new Project();
            var instance = new Instance();

            var model = new InstancePropertiesInspectorModel(
                SampleLocator,
                project,
                instance,
                null);
            Assert.AreEqual(
                new InternalDnsName.ZonalName(SampleLocator).Name,
                model.InternalZonalDnsName);
        }
    }
}
