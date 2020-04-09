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
            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;
            
            instanceA.Username = null;
            instanceB.Username = "";

            // Inherited value is shown...
            Assert.AreEqual(username, instanceA.Username);
            Assert.AreEqual(username, instanceB.Username);

            // ...and takes effect.
            Assert.AreEqual(username, instanceA.EffectiveSettingsWithInheritanceApplied.Username);
            Assert.AreEqual(username, instanceB.EffectiveSettingsWithInheritanceApplied.Username);

            Assert.IsFalse(instanceA.ShouldSerializeUsername());
            Assert.IsFalse(instanceB.ShouldSerializeUsername());
        }

        [Test]
        public void WhenDomainSetInProject_ProjectValueIsInheritedDownToVm(
            [Values("domain", null)]
            string domain)
        {
            this.projectNode.Domain = domain;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;
            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            instanceA.Domain = null;
            instanceB.Domain = "";

            // Inherited value is shown...
            Assert.AreEqual(domain, instanceA.Domain);
            Assert.AreEqual(domain, instanceB.Domain);
                            
            // ...and takes effect
            Assert.AreEqual(domain, instanceA.EffectiveSettingsWithInheritanceApplied.Domain);
            Assert.AreEqual(domain, instanceB.EffectiveSettingsWithInheritanceApplied.Domain);

            Assert.IsFalse(instanceA.ShouldSerializeDomain());
            Assert.IsFalse(instanceB.ShouldSerializeDomain());
        }

        [Test]
        public void WhenPasswordSetInProject_ProjectValueIsInheritedDownToVm()
        {
            var password = "secret";
            this.projectNode.CleartextPassword = password;
            Assert.IsTrue(this.projectNode.ShouldSerializeCleartextPassword());

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;
            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            instanceA.CleartextPassword = null;
            instanceB.CleartextPassword = "";

            // Inherited value is not shown...
            Assert.AreEqual("********", instanceA.CleartextPassword);
            Assert.AreEqual("********", instanceB.CleartextPassword);

            // ...but takes effect
            Assert.AreEqual(password, instanceA.EffectiveSettingsWithInheritanceApplied.Password.AsClearText());
            Assert.AreEqual(password, instanceB.EffectiveSettingsWithInheritanceApplied.Password.AsClearText());

            Assert.IsFalse(instanceA.ShouldSerializeCleartextPassword());
            Assert.IsFalse(instanceB.ShouldSerializeCleartextPassword());
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

            Assert.AreEqual(size, instanceA.EffectiveSettingsWithInheritanceApplied.DesktopSize);
            Assert.AreEqual(size, instanceB.EffectiveSettingsWithInheritanceApplied.DesktopSize);

            Assert.IsFalse(instanceA.ShouldSerializeDesktopSize());
            Assert.IsFalse(instanceB.ShouldSerializeDesktopSize());
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
            Assert.IsTrue(zoneA.ShouldSerializeUsername());
            Assert.IsFalse(zoneB.ShouldSerializeUsername());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            Assert.AreEqual("overriden-value", instanceA.Username);
            Assert.AreEqual("root-value", instanceB.Username);

            Assert.AreEqual("overriden-value", instanceA.EffectiveSettingsWithInheritanceApplied.Username);
            Assert.AreEqual("root-value", instanceB.EffectiveSettingsWithInheritanceApplied.Username);
            
            Assert.IsFalse(instanceA.ShouldSerializeUsername());
            Assert.IsFalse(instanceB.ShouldSerializeUsername());
        }

        [Test]
        public void WhenDesktopSizeSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.DesktopSize = RdpDesktopSize.ClientSize;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;

            zoneA.DesktopSize = RdpDesktopSize.ScreenSize;
            zoneB.DesktopSize = RdpDesktopSize._Default;
            Assert.AreEqual(RdpDesktopSize.ScreenSize, zoneA.DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, zoneB.DesktopSize);
            Assert.IsTrue(zoneA.ShouldSerializeDesktopSize());
            Assert.IsFalse(zoneB.ShouldSerializeDesktopSize());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            Assert.AreEqual(RdpDesktopSize.ScreenSize, instanceA.DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, instanceB.DesktopSize);

            Assert.AreEqual(RdpDesktopSize.ScreenSize, instanceA.EffectiveSettingsWithInheritanceApplied.DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, instanceB.EffectiveSettingsWithInheritanceApplied.DesktopSize);
            
            Assert.IsFalse(instanceA.ShouldSerializeDesktopSize());
            Assert.IsFalse(instanceB.ShouldSerializeDesktopSize());
        }

        [Test]
        public void WhenAuthenticationLevelSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.AuthenticationLevel = RdpAuthenticationLevel.RequireServerAuthentication;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;

            zoneA.AuthenticationLevel = RdpAuthenticationLevel.AttemptServerAuthentication;
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, zoneA.AuthenticationLevel);
            Assert.IsTrue(zoneA.ShouldSerializeAuthenticationLevel());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, instanceA.AuthenticationLevel);
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, instanceA.EffectiveSettingsWithInheritanceApplied.AuthenticationLevel);
            Assert.IsFalse(instanceA.ShouldSerializeAuthenticationLevel());
        }

        [Test]
        public void WhenBitmapPersistenceSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.BitmapPersistence = RdpBitmapPersistence.Disabled;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;

            zoneA.BitmapPersistence = RdpBitmapPersistence.Enabled;
            Assert.AreEqual(RdpBitmapPersistence.Enabled, zoneA.BitmapPersistence );
            Assert.IsTrue(zoneA.ShouldSerializeBitmapPersistence());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            Assert.AreEqual(RdpBitmapPersistence.Enabled, instanceA.BitmapPersistence);
            Assert.AreEqual(RdpBitmapPersistence.Enabled, instanceA.EffectiveSettingsWithInheritanceApplied.BitmapPersistence);
            Assert.IsFalse(instanceA.ShouldSerializeBitmapPersistence());
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
