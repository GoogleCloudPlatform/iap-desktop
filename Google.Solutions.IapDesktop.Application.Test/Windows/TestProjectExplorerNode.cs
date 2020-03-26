//
// Copyright 2010 Google LLC
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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Registry;
using Google.Solutions.IapDesktop.Application.Settings;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    public class TestProjectExplorerNode : WindowTestFixtureBase
    {
        private ProjectNode projectNode;

        [SetUp]
        public void PrepareNodes()
        {
            var settingsService = this.serviceProvider.GetService<InventorySettingsRepository>();
            settingsService.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "project-1"
            });

            // Add some instances.
            var instances = new[]
            {
                CreateInstance("instance-1a", "antarctica1-a", true),
                CreateInstance("instance-1b", "antarctica1-b", true)
            };

            this.projectNode = new ProjectNode(settingsService, "project-1");
            this.projectNode.Populate(instances, _ => false);
        }

        [Test]
        public void WhenUsernameSetInProject_ProjectValueIsInheritedDownToVm(
            [Values("user", null)]
            string username)
        {
            this.projectNode.Username = username;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;

            Assert.AreEqual(username, zoneA.Username);
            Assert.AreEqual(username, zoneB.Username);

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            Assert.AreEqual(username, instanceA.Username);
            Assert.AreEqual(username, instanceB.Username);
        }

        [Test]

        public void WhenDesktopSizeSetInProject_ProjectValueIsInheritedDownToVm(
            [Values(RdpDesktopSize.ClientSize, RdpDesktopSize.ScreenSize)]
            RdpDesktopSize size
            )
        {
            this.projectNode.DesktopSize = size;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;

            Assert.AreEqual(size, zoneA.DesktopSize);
            Assert.AreEqual(size, zoneB.DesktopSize);

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            Assert.AreEqual(size, instanceA.DesktopSize);
            Assert.AreEqual(size, instanceB.DesktopSize);
        }

        [Test]
        public void WhenUsernameSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.Username = "root-value";

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;

            zoneA.Username = "overriden-value";
            zoneB.Username = null;
            Assert.AreEqual("overriden-value", zoneA.Username);
            Assert.AreEqual("root-value", zoneB.Username);

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            Assert.AreEqual("overriden-value", instanceA.Username);
            Assert.AreEqual("root-value", instanceB.Username);
        }

        [Test]
        public void WhenDesktopSizeSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.DesktopSize = RdpDesktopSize.ClientSize;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;

            zoneA.DesktopSize = RdpDesktopSize.ScreenSize;
            zoneB.DesktopSize = RdpDesktopSize.ClientSize;
            Assert.AreEqual(RdpDesktopSize.ScreenSize, zoneA.DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, zoneB.DesktopSize);

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            Assert.AreEqual(RdpDesktopSize.ScreenSize, instanceA.DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, instanceB.DesktopSize);
        }

        [Test]
        public void WhenSettingsAppliedOnProjectAndZone_ThenEffectiveSettingsReflectThese()
        {
            this.projectNode.AuthenticationLevel = RdpAuthenticationLevel.RequireServerAuthentication;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            zoneA.Username = "zone";

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            instanceA.Domain = "instance";

            var effective = instanceA.EffectiveSettingsWithInheritanceApplied;

            Assert.AreEqual(RdpAuthenticationLevel.RequireServerAuthentication, effective.AuthenticationLevel);
            Assert.AreEqual("zone", effective.Username);
            Assert.AreEqual("instance", effective.Domain);
        }

        [Test]
        public void WhenSettingPassword_CleartextPasswordIsMasked()
        {
            this.projectNode.CleartextPassword = "actual password";

            Assert.AreEqual("********", this.projectNode.CleartextPassword);
        }

        [Test]
        public void WhenSettingPassword_EffectiveSettingsContainRealPassword()
        {
            this.projectNode.CleartextPassword = "actual password";

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var effective = instanceA.EffectiveSettingsWithInheritanceApplied;

            Assert.AreEqual("actual password", effective.Password.AsClearText());
        }
    }
}
