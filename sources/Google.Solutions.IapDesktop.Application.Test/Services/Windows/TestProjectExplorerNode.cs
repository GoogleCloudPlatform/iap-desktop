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
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Util;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Windows
{
    [TestFixture]
    public class TestProjectExplorerNode : WindowTestFixtureBase
    {
        private ProjectNode projectNode;

        [SetUp]
        public void PrepareNodes()
        {
            var settingsService = this.serviceProvider.GetService<ConnectionSettingsRepository>();
            settingsService.SetProjectSettings(new ProjectConnectionSettings()
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
            this.projectNode.SettingsEditor.Username = username;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;
            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            instanceA.SettingsEditor.Username = null;
            instanceB.SettingsEditor.Username = "";

            // Inherited value is shown...
            Assert.AreEqual(username, instanceA.SettingsEditor.Username);
            Assert.AreEqual(username, instanceB.SettingsEditor.Username);

            // ...and takes effect.
            Assert.AreEqual(username, instanceA.CreateConnectionSettings().Username);
            Assert.AreEqual(username, instanceB.CreateConnectionSettings().Username);

            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeUsername());
            Assert.IsFalse(instanceB.SettingsEditor.ShouldSerializeUsername());
        }

        [Test]
        public void WhenDomainSetInProject_ProjectValueIsInheritedDownToVm(
            [Values("domain", null)]
            string domain)
        {
            this.projectNode.SettingsEditor.Domain = domain;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;
            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            instanceA.SettingsEditor.Domain = null;
            instanceB.SettingsEditor.Domain = "";

            // Inherited value is shown...
            Assert.AreEqual(domain, instanceA.SettingsEditor.Domain);
            Assert.AreEqual(domain, instanceB.SettingsEditor.Domain);

            // ...and takes effect
            Assert.AreEqual(domain, instanceA.CreateConnectionSettings().Domain);
            Assert.AreEqual(domain, instanceB.CreateConnectionSettings().Domain);

            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeDomain());
            Assert.IsFalse(instanceB.SettingsEditor.ShouldSerializeDomain());
        }

        [Test]
        public void WhenPasswordSetInProject_ProjectValueIsInheritedDownToVm()
        {
            var password = "secret";
            this.projectNode.SettingsEditor.CleartextPassword = password;
            Assert.IsTrue(this.projectNode.SettingsEditor.ShouldSerializeCleartextPassword());

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;
            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            instanceA.SettingsEditor.CleartextPassword = null;
            instanceB.SettingsEditor.CleartextPassword = "";

            // Inherited value is not shown...
            Assert.AreEqual("********", instanceA.SettingsEditor.CleartextPassword);
            Assert.AreEqual("********", instanceB.SettingsEditor.CleartextPassword);

            // ...but takes effect
            Assert.AreEqual(password, instanceA.CreateConnectionSettings().Password.AsClearText());
            Assert.AreEqual(password, instanceB.CreateConnectionSettings().Password.AsClearText());

            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeCleartextPassword());
            Assert.IsFalse(instanceB.SettingsEditor.ShouldSerializeCleartextPassword());
        }

        [Test]
        public void WhenSettingPassword_CleartextPasswordIsMasked()
        {
            this.projectNode.SettingsEditor.CleartextPassword = "actual password";

            Assert.AreEqual("********", this.projectNode.SettingsEditor.CleartextPassword);
        }

        [Test]
        public void WhenSettingPassword_EffectiveSettingsContainRealPassword()
        {
            this.projectNode.SettingsEditor.CleartextPassword = "actual password";

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var effective = instanceA.CreateConnectionSettings();

            Assert.AreEqual("actual password", effective.Password.AsClearText());
        }

        [Test]
        public void WhenUsernameSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.SettingsEditor.Username = "root-value";

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;

            zoneA.SettingsEditor.Username = "overriden-value";
            zoneB.SettingsEditor.Username = null;
            Assert.AreEqual("overriden-value", zoneA.SettingsEditor.Username);
            Assert.AreEqual("root-value", zoneB.SettingsEditor.Username);
            Assert.IsTrue(zoneA.SettingsEditor.ShouldSerializeUsername());
            Assert.IsFalse(zoneB.SettingsEditor.ShouldSerializeUsername());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            Assert.AreEqual("overriden-value", instanceA.SettingsEditor.Username);
            Assert.AreEqual("root-value", instanceB.SettingsEditor.Username);

            Assert.AreEqual("overriden-value", instanceA.CreateConnectionSettings().Username);
            Assert.AreEqual("root-value", instanceB.CreateConnectionSettings().Username);

            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeUsername());
            Assert.IsFalse(instanceB.SettingsEditor.ShouldSerializeUsername());
        }

        [Test]
        public void WhenDesktopSizeSetInProject_ProjectValueIsInheritedDownToVm(
            [Values(RdpDesktopSize.ClientSize, RdpDesktopSize.ScreenSize)]
            RdpDesktopSize size
            )
        {
            this.projectNode.SettingsEditor.DesktopSize = size;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;

            Assert.AreEqual(size, zoneA.SettingsEditor.DesktopSize);
            Assert.AreEqual(size, zoneB.SettingsEditor.DesktopSize);

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            Assert.AreEqual(size, instanceA.SettingsEditor.DesktopSize);
            Assert.AreEqual(size, instanceB.SettingsEditor.DesktopSize);

            Assert.AreEqual(size, instanceA.CreateConnectionSettings().DesktopSize);
            Assert.AreEqual(size, instanceB.CreateConnectionSettings().DesktopSize);

            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeDesktopSize());
            Assert.IsFalse(instanceB.SettingsEditor.ShouldSerializeDesktopSize());
        }

        [Test]
        public void WhenDesktopSizeSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.SettingsEditor.DesktopSize = RdpDesktopSize.ClientSize;
            Assert.AreNotEqual(RdpDesktopSize._Default, this.projectNode.SettingsEditor.DesktopSize);

            var zoneA = (ZoneNode)this.projectNode.FirstNode;
            var zoneB = (ZoneNode)this.projectNode.FirstNode.NextNode;

            zoneA.SettingsEditor.DesktopSize = RdpDesktopSize.ScreenSize;
            zoneB.SettingsEditor.DesktopSize = RdpDesktopSize._Default;
            Assert.AreEqual(RdpDesktopSize.ScreenSize, zoneA.SettingsEditor.DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, zoneB.SettingsEditor.DesktopSize);
            Assert.IsTrue(zoneA.SettingsEditor.ShouldSerializeDesktopSize());
            Assert.IsFalse(zoneB.SettingsEditor.ShouldSerializeDesktopSize());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            var instanceB = (VmInstanceNode)zoneB.FirstNode;

            Assert.AreEqual(RdpDesktopSize.ScreenSize, instanceA.SettingsEditor.DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, instanceB.SettingsEditor.DesktopSize);

            Assert.AreEqual(RdpDesktopSize.ScreenSize, instanceA.CreateConnectionSettings().DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, instanceB.CreateConnectionSettings().DesktopSize);

            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeDesktopSize());
            Assert.IsFalse(instanceB.SettingsEditor.ShouldSerializeDesktopSize());
        }

        [Test]
        public void WhenDesktopSizeSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.projectNode.SettingsEditor.DesktopSize = RdpDesktopSize.ScreenSize;
            Assert.AreNotEqual(RdpDesktopSize._Default, this.projectNode.SettingsEditor.DesktopSize);

            var instanceA = (VmInstanceNode)this.projectNode.FirstNode.FirstNode;
            instanceA.SettingsEditor.DesktopSize = RdpDesktopSize._Default;

            Assert.AreEqual(this.projectNode.SettingsEditor.DesktopSize, instanceA.SettingsEditor.DesktopSize);
            Assert.AreEqual(this.projectNode.SettingsEditor.DesktopSize, instanceA.CreateConnectionSettings().DesktopSize);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeDesktopSize());
        }

        [Test]
        public void WhenAuthenticationLevelSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.SettingsEditor.AuthenticationLevel = RdpAuthenticationLevel.RequireServerAuthentication;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;

            zoneA.SettingsEditor.AuthenticationLevel = RdpAuthenticationLevel.AttemptServerAuthentication;
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, zoneA.SettingsEditor.AuthenticationLevel);
            Assert.IsTrue(zoneA.SettingsEditor.ShouldSerializeAuthenticationLevel());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, instanceA.SettingsEditor.AuthenticationLevel);
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, instanceA.CreateConnectionSettings().AuthenticationLevel);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeAuthenticationLevel());
        }
            
        [Test]
        public void WhenAuthenticationLevelSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.projectNode.SettingsEditor.AuthenticationLevel = RdpAuthenticationLevel.RequireServerAuthentication;
            Assert.AreNotEqual(RdpAuthenticationLevel._Default, this.projectNode.SettingsEditor.AuthenticationLevel);

            var instanceA = (VmInstanceNode)this.projectNode.FirstNode.FirstNode;
            instanceA.SettingsEditor.AuthenticationLevel = RdpAuthenticationLevel._Default;

            Assert.AreEqual(this.projectNode.SettingsEditor.AuthenticationLevel, instanceA.SettingsEditor.AuthenticationLevel);
            Assert.AreEqual(this.projectNode.SettingsEditor.AuthenticationLevel, instanceA.CreateConnectionSettings().AuthenticationLevel);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeAuthenticationLevel());
        }

        [Test]
        public void WhenBitmapPersistenceSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.SettingsEditor.BitmapPersistence = RdpBitmapPersistence.Disabled;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;

            zoneA.SettingsEditor.BitmapPersistence = RdpBitmapPersistence.Enabled;
            Assert.AreEqual(RdpBitmapPersistence.Enabled, zoneA.SettingsEditor.BitmapPersistence);
            Assert.IsTrue(zoneA.SettingsEditor.ShouldSerializeBitmapPersistence());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            Assert.AreEqual(RdpBitmapPersistence.Enabled, instanceA.SettingsEditor.BitmapPersistence);
            Assert.AreEqual(RdpBitmapPersistence.Enabled, instanceA.CreateConnectionSettings().BitmapPersistence);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeBitmapPersistence());
        }

        [Test]
        public void WhenBitmapPersistenceSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.projectNode.SettingsEditor.BitmapPersistence = RdpBitmapPersistence.Enabled;
            Assert.AreNotEqual(RdpBitmapPersistence._Default, this.projectNode.SettingsEditor.BitmapPersistence);

            var instanceA = (VmInstanceNode)this.projectNode.FirstNode.FirstNode;
            instanceA.SettingsEditor.BitmapPersistence = RdpBitmapPersistence._Default;

            Assert.AreEqual(this.projectNode.SettingsEditor.BitmapPersistence, instanceA.SettingsEditor.BitmapPersistence);
            Assert.AreEqual(this.projectNode.SettingsEditor.BitmapPersistence, instanceA.CreateConnectionSettings().BitmapPersistence);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeBitmapPersistence());
        }

        [Test]
        public void WhenAudioModeSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.SettingsEditor.AudioMode = RdpAudioMode.DoNotPlay;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;

            zoneA.SettingsEditor.AudioMode = RdpAudioMode.PlayOnServer;
            Assert.AreEqual(RdpAudioMode.PlayOnServer, zoneA.SettingsEditor.AudioMode);
            Assert.IsTrue(zoneA.SettingsEditor.ShouldSerializeAudioMode());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            Assert.AreEqual(RdpAudioMode.PlayOnServer, instanceA.SettingsEditor.AudioMode);
            Assert.AreEqual(RdpAudioMode.PlayOnServer, instanceA.CreateConnectionSettings().AudioMode);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeAudioMode());
        }

        [Test]
        public void WhenAudioModeSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.projectNode.SettingsEditor.AudioMode = RdpAudioMode.PlayOnServer;
            Assert.AreNotEqual(RdpAudioMode._Default, this.projectNode.SettingsEditor.AudioMode);

            var instanceA = (VmInstanceNode)this.projectNode.FirstNode.FirstNode;
            instanceA.SettingsEditor.AudioMode = RdpAudioMode._Default;

            Assert.AreEqual(this.projectNode.SettingsEditor.AudioMode, instanceA.SettingsEditor.AudioMode);
            Assert.AreEqual(this.projectNode.SettingsEditor.AudioMode, instanceA.CreateConnectionSettings().AudioMode);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeAudioMode());
        }

        [Test]
        public void WhenColorDepthSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.SettingsEditor.ColorDepth = RdpColorDepth.HighColor;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;

            zoneA.SettingsEditor.ColorDepth = RdpColorDepth.DeepColor;
            Assert.AreEqual(RdpColorDepth.DeepColor, zoneA.SettingsEditor.ColorDepth);
            Assert.IsTrue(zoneA.SettingsEditor.ShouldSerializeColorDepth());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            Assert.AreEqual(RdpColorDepth.DeepColor, instanceA.SettingsEditor.ColorDepth);
            Assert.AreEqual(RdpColorDepth.DeepColor, instanceA.CreateConnectionSettings().ColorDepth);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeColorDepth());
        }

        [Test]
        public void WhenColorDepthSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.projectNode.SettingsEditor.ColorDepth = RdpColorDepth.DeepColor;
            Assert.AreNotEqual(RdpColorDepth._Default, this.projectNode.SettingsEditor.ColorDepth);

            var instanceA = (VmInstanceNode)this.projectNode.FirstNode.FirstNode;
            instanceA.SettingsEditor.ColorDepth = RdpColorDepth._Default;

            Assert.AreEqual(this.projectNode.SettingsEditor.ColorDepth, instanceA.SettingsEditor.ColorDepth);
            Assert.AreEqual(this.projectNode.SettingsEditor.ColorDepth, instanceA.CreateConnectionSettings().ColorDepth);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeColorDepth());
        }

        [Test]
        public void WhenConnectionBarSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.projectNode.SettingsEditor.ConnectionBar = RdpConnectionBarState.Off;

            var zoneA = (ZoneNode)this.projectNode.FirstNode;

            zoneA.SettingsEditor.ConnectionBar = RdpConnectionBarState.Pinned;
            Assert.AreEqual(RdpConnectionBarState.Pinned, zoneA.SettingsEditor.ConnectionBar);
            Assert.IsTrue(zoneA.SettingsEditor.ShouldSerializeConnectionBar());

            var instanceA = (VmInstanceNode)zoneA.FirstNode;
            Assert.AreEqual(RdpConnectionBarState.Pinned, instanceA.SettingsEditor.ConnectionBar);
            Assert.AreEqual(RdpConnectionBarState.Pinned, instanceA.CreateConnectionSettings().ConnectionBar);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeConnectionBar());
        }

        [Test]
        public void WhenConnectionBarSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.projectNode.SettingsEditor.ConnectionBar = RdpConnectionBarState.Off;
            Assert.AreNotEqual(RdpConnectionBarState._Default, this.projectNode.SettingsEditor.ConnectionBar);

            var instanceA = (VmInstanceNode)this.projectNode.FirstNode.FirstNode;
            instanceA.SettingsEditor.ConnectionBar = RdpConnectionBarState._Default;

            Assert.AreEqual(this.projectNode.SettingsEditor.ConnectionBar, instanceA.SettingsEditor.ConnectionBar);
            Assert.AreEqual(this.projectNode.SettingsEditor.ConnectionBar, instanceA.CreateConnectionSettings().ConnectionBar);
            Assert.IsFalse(instanceA.SettingsEditor.ShouldSerializeConnectionBar());
        }
    }
}
