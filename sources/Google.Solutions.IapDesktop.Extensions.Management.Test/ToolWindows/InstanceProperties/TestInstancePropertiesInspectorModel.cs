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
using System.Linq;
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

        //---------------------------------------------------------------------
        // Load.
        //---------------------------------------------------------------------

        [Test]
        public async Task Load(
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

            Assert.That(model.InstanceName, Is.EqualTo(locator.Name));
            Assert.IsNull(model.Hostname);
            Assert.That(model.Status, Is.EqualTo("RUNNING"));
            Assert.IsNotNull(model.InternalIp);
            Assert.IsNotNull(model.ExternalIp);
            Assert.IsNotNull(model.Licenses);
            Assert.That(model.OsInventory, Is.EqualTo(model.IsOsInventoryInformationPopulated
                ? FeatureFlag.Enabled : FeatureFlag.Disabled));
            Assert.That(model.Diagnostics, Is.EqualTo(FeatureFlag.Disabled));
            Assert.That(model.GuestAttributes, Is.EqualTo(FeatureFlag.Enabled));
            Assert.That(model.IsSoleTenant, Is.False);
            Assert.That(model.MachineType, Is.EqualTo(WindowsInstanceAttribute.DefaultMachineType));
            Assert.That(model.Tags.Any(), Is.False);
        }

        [Test]
        public async Task Load_WhenGuestAttributesDisabledByPolicy_ThenOsPropertiesAreNull(
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

            Assert.That(model.InstanceName, Is.EqualTo(locator.Name));
            Assert.That(model.Status, Is.EqualTo("RUNNING"));

            Assert.That(model.IsOsInventoryInformationPopulated, Is.False);
            Assert.IsNull(model.Architecture);
            Assert.IsNull(model.KernelVersion);
            Assert.IsNull(model.OperatingSystemFullName);
            Assert.IsNull(model.OperatingSystemVersion);
        }

        //---------------------------------------------------------------------
        // Constructor.
        //---------------------------------------------------------------------

        [Test]
        public void Constructor_WhenMetadataIsEmpty_ThenDefaultsAreApplied()
        {
            var project = new Project();
            var instance = new Instance();

            var model = new InstancePropertiesInspectorModel(
                SampleLocator,
                project,
                instance,
                null);
            Assert.That(model.OsInventory, Is.EqualTo(FeatureFlag.Disabled));
            Assert.That(model.Diagnostics, Is.EqualTo(FeatureFlag.Disabled));
            Assert.That(model.SerialPortAccess, Is.EqualTo(FeatureFlag.Disabled));
            Assert.That(model.GuestAttributes, Is.EqualTo(FeatureFlag.Disabled));
        }

        [Test]
        public void Constructor_WhenGuestOsFieldsAreNull_ThenDefaultsAreApplied()
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
        public void Constructor_WhenFlagSetInCommonInstanceMetadataAndInstanceMetadata_ThenInstanceMetadataPrevails()
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
            Assert.That(model.OsLogin, Is.EqualTo(FeatureFlag.Disabled));
            Assert.That(model.OsLogin2FA, Is.EqualTo(FeatureFlag.Disabled));
            Assert.That(model.BlockProjectSshKeys, Is.EqualTo(FeatureFlag.Enabled));
            Assert.That(model.OsLoginWithSecurityKey, Is.EqualTo(FeatureFlag.Disabled));
        }

        [Test]
        public void Constructor_WhenFlagSetInInstanceMetadataOnly_ThenInstanceMetadataPrevails()
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
            Assert.That(model.OsLogin2FA, Is.EqualTo(FeatureFlag.Enabled));
            Assert.That(model.OsLoginWithSecurityKey, Is.EqualTo(FeatureFlag.Enabled));
        }

        //---------------------------------------------------------------------
        // InternalZonalDnsName.
        //---------------------------------------------------------------------

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
            Assert.That(
                model.InternalZonalDnsName, Is.EqualTo(new InternalDnsName.ZonalName(SampleLocator).Name));
        }
    }
}
