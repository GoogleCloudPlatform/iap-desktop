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

using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Views.ConnectionSettings
{
    [TestFixture]
    public class TestConnectionSettingsEditor : FixtureBase
    {
        //
        // Test structure:
        //
        // + project
        //   + zoneA
        //     + instanceA
        //   + zoneB
        //     + instanceA
        //

        private ConnectionSettingsEditor project;
        private ConnectionSettingsEditor zoneA, zoneB;
        private ConnectionSettingsEditor instanceA, instanceB;

        [SetUp]
        public void PrepareNodes()
        {
            this.project = new ConnectionSettingsEditor(
                new ProjectConnectionSettings(),
                settings => { },
                null);

            this.zoneA = new ConnectionSettingsEditor(
                new ZoneConnectionSettings(),
                settings => { },
                this.project);

            this.zoneB = new ConnectionSettingsEditor(
                new ZoneConnectionSettings(),
                settings => { },
                this.project);

            this.instanceA = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                settings => { },
                this.zoneA);

            this.instanceB = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                settings => { },
                this.zoneB);
        }

        [Test]
        public void WhenUsernameSetInProject_ProjectValueIsInheritedDownToVm(
            [Values("user", null)]
            string username)
        {
            this.project.Username = username;

            this.instanceA.Username = null;
            this.instanceB.Username = "";

            // Inherited value is shown...
            Assert.AreEqual(username, instanceA.Username);
            Assert.AreEqual(username, instanceB.Username);

            // ...and takes effect.
            Assert.AreEqual(username, instanceA.CreateConnectionSettings("instance").Username);
            Assert.AreEqual(username, instanceB.CreateConnectionSettings("instance").Username);

            Assert.IsFalse(instanceA.ShouldSerializeUsername());
            Assert.IsFalse(instanceB.ShouldSerializeUsername());
        }

        [Test]
        public void WhenDomainSetInProject_ProjectValueIsInheritedDownToVm(
            [Values("domain", null)]
            string domain)
        {
            this.project.Domain = domain;

            this.instanceA.Domain = null;
            this.instanceB.Domain = "";

            // Inherited value is shown...
            Assert.AreEqual(domain, instanceA.Domain);
            Assert.AreEqual(domain, instanceB.Domain);

            // ...and takes effect
            Assert.AreEqual(domain, instanceA.CreateConnectionSettings("instance").Domain);
            Assert.AreEqual(domain, instanceB.CreateConnectionSettings("instance").Domain);

            Assert.IsFalse(instanceA.ShouldSerializeDomain());
            Assert.IsFalse(instanceB.ShouldSerializeDomain());
        }

        [Test]
        public void WhenPasswordSetInProject_ProjectValueIsInheritedDownToVm()
        {
            var password = "secret";
            this.project.CleartextPassword = password;
            Assert.IsTrue(this.project.ShouldSerializeCleartextPassword());

            instanceA.CleartextPassword = null;
            instanceB.CleartextPassword = "";

            // Inherited value is not shown...
            Assert.AreEqual("********", instanceA.CleartextPassword);
            Assert.AreEqual("********", instanceB.CleartextPassword);

            // ...but takes effect
            Assert.AreEqual(password, instanceA.CreateConnectionSettings("instance").Password.AsClearText());
            Assert.AreEqual(password, instanceB.CreateConnectionSettings("instance").Password.AsClearText());

            Assert.IsFalse(instanceA.ShouldSerializeCleartextPassword());
            Assert.IsFalse(instanceB.ShouldSerializeCleartextPassword());
        }

        [Test]
        public void WhenSettingPassword_CleartextPasswordIsMasked()
        {
            this.project.CleartextPassword = "actual password";

            Assert.AreEqual("********", this.project.CleartextPassword);
        }

        [Test]
        public void WhenSettingPassword_EffectiveSettingsContainRealPassword()
        {
            this.project.CleartextPassword = "actual password";

            var effective = instanceA.CreateConnectionSettings("instance");

            Assert.AreEqual("actual password", effective.Password.AsClearText());
        }

        [Test]
        public void WhenUsernameSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.project.Username = "root-value";

            zoneA.Username = "overriden-value";
            zoneB.Username = null;
            Assert.AreEqual("overriden-value", zoneA.Username);
            Assert.AreEqual("root-value", zoneB.Username);
            Assert.IsTrue(zoneA.ShouldSerializeUsername());
            Assert.IsFalse(zoneB.ShouldSerializeUsername());

            Assert.AreEqual("overriden-value", instanceA.Username);
            Assert.AreEqual("root-value", instanceB.Username);

            Assert.AreEqual("overriden-value", instanceA.CreateConnectionSettings("instance").Username);
            Assert.AreEqual("root-value", instanceB.CreateConnectionSettings("instance").Username);

            Assert.IsFalse(instanceA.ShouldSerializeUsername());
            Assert.IsFalse(instanceB.ShouldSerializeUsername());
        }

        [Test]
        public void WhenDesktopSizeSetInProject_ProjectValueIsInheritedDownToVm(
            [Values(RdpDesktopSize.ClientSize, RdpDesktopSize.ScreenSize)]
            RdpDesktopSize size
            )
        {
            this.project.DesktopSize = size;

            Assert.AreEqual(size, zoneA.DesktopSize);
            Assert.AreEqual(size, zoneB.DesktopSize);

            Assert.AreEqual(size, instanceA.DesktopSize);
            Assert.AreEqual(size, instanceB.DesktopSize);

            Assert.AreEqual(size, instanceA.CreateConnectionSettings("instance").DesktopSize);
            Assert.AreEqual(size, instanceB.CreateConnectionSettings("instance").DesktopSize);

            Assert.IsFalse(instanceA.ShouldSerializeDesktopSize());
            Assert.IsFalse(instanceB.ShouldSerializeDesktopSize());
        }

        [Test]
        public void WhenDesktopSizeSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.project.DesktopSize = RdpDesktopSize.ClientSize;
            Assert.AreNotEqual(RdpDesktopSize._Default, this.project.DesktopSize);

            zoneA.DesktopSize = RdpDesktopSize.ScreenSize;
            zoneB.DesktopSize = RdpDesktopSize._Default;
            Assert.AreEqual(RdpDesktopSize.ScreenSize, zoneA.DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, zoneB.DesktopSize);
            Assert.IsTrue(zoneA.ShouldSerializeDesktopSize());
            Assert.IsFalse(zoneB.ShouldSerializeDesktopSize());

            Assert.AreEqual(RdpDesktopSize.ScreenSize, instanceA.DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, instanceB.DesktopSize);

            Assert.AreEqual(RdpDesktopSize.ScreenSize, instanceA.CreateConnectionSettings("instance").DesktopSize);
            Assert.AreEqual(RdpDesktopSize.ClientSize, instanceB.CreateConnectionSettings("instance").DesktopSize);

            Assert.IsFalse(instanceA.ShouldSerializeDesktopSize());
            Assert.IsFalse(instanceB.ShouldSerializeDesktopSize());
        }

        [Test]
        public void WhenDesktopSizeSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.project.DesktopSize = RdpDesktopSize.ScreenSize;
            Assert.AreNotEqual(RdpDesktopSize._Default, this.project.DesktopSize);

            instanceA.DesktopSize = RdpDesktopSize._Default;

            Assert.AreEqual(this.project.DesktopSize, instanceA.DesktopSize);
            Assert.AreEqual(this.project.DesktopSize, instanceA.CreateConnectionSettings("instance").DesktopSize);
            Assert.IsFalse(instanceA.ShouldSerializeDesktopSize());
        }

        [Test]
        public void WhenAuthenticationLevelSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.project.AuthenticationLevel = RdpAuthenticationLevel.RequireServerAuthentication;

            zoneA.AuthenticationLevel = RdpAuthenticationLevel.AttemptServerAuthentication;
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, zoneA.AuthenticationLevel);
            Assert.IsTrue(zoneA.ShouldSerializeAuthenticationLevel());

            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, instanceA.AuthenticationLevel);
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, instanceA.CreateConnectionSettings("instance").AuthenticationLevel);
            Assert.IsFalse(instanceA.ShouldSerializeAuthenticationLevel());
        }

        [Test]
        public void WhenAuthenticationLevelSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.project.AuthenticationLevel = RdpAuthenticationLevel.RequireServerAuthentication;
            Assert.AreNotEqual(RdpAuthenticationLevel._Default, this.project.AuthenticationLevel);

            instanceA.AuthenticationLevel = RdpAuthenticationLevel._Default;

            Assert.AreEqual(this.project.AuthenticationLevel, instanceA.AuthenticationLevel);
            Assert.AreEqual(this.project.AuthenticationLevel, instanceA.CreateConnectionSettings("instance").AuthenticationLevel);
            Assert.IsFalse(instanceA.ShouldSerializeAuthenticationLevel());
        }

        [Test]
        public void WhenBitmapPersistenceSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.project.BitmapPersistence = RdpBitmapPersistence.Disabled;

            zoneA.BitmapPersistence = RdpBitmapPersistence.Enabled;
            Assert.AreEqual(RdpBitmapPersistence.Enabled, zoneA.BitmapPersistence);
            Assert.IsTrue(zoneA.ShouldSerializeBitmapPersistence());

            Assert.AreEqual(RdpBitmapPersistence.Enabled, instanceA.BitmapPersistence);
            Assert.AreEqual(RdpBitmapPersistence.Enabled, instanceA.CreateConnectionSettings("instance").BitmapPersistence);
            Assert.IsFalse(instanceA.ShouldSerializeBitmapPersistence());
        }

        [Test]
        public void WhenBitmapPersistenceSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.project.BitmapPersistence = RdpBitmapPersistence.Enabled;
            Assert.AreNotEqual(RdpBitmapPersistence._Default, this.project.BitmapPersistence);

            instanceA.BitmapPersistence = RdpBitmapPersistence._Default;

            Assert.AreEqual(this.project.BitmapPersistence, instanceA.BitmapPersistence);
            Assert.AreEqual(this.project.BitmapPersistence, instanceA.CreateConnectionSettings("instance").BitmapPersistence);
            Assert.IsFalse(instanceA.ShouldSerializeBitmapPersistence());
        }

        [Test]
        public void WhenAudioModeSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.project.AudioMode = RdpAudioMode.DoNotPlay;

            zoneA.AudioMode = RdpAudioMode.PlayOnServer;
            Assert.AreEqual(RdpAudioMode.PlayOnServer, zoneA.AudioMode);
            Assert.IsTrue(zoneA.ShouldSerializeAudioMode());

            Assert.AreEqual(RdpAudioMode.PlayOnServer, instanceA.AudioMode);
            Assert.AreEqual(RdpAudioMode.PlayOnServer, instanceA.CreateConnectionSettings("instance").AudioMode);
            Assert.IsFalse(instanceA.ShouldSerializeAudioMode());
        }

        [Test]
        public void WhenAudioModeSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.project.AudioMode = RdpAudioMode.PlayOnServer;
            Assert.AreNotEqual(RdpAudioMode._Default, this.project.AudioMode);

            instanceA.AudioMode = RdpAudioMode._Default;

            Assert.AreEqual(this.project.AudioMode, instanceA.AudioMode);
            Assert.AreEqual(this.project.AudioMode, instanceA.CreateConnectionSettings("instance").AudioMode);
            Assert.IsFalse(instanceA.ShouldSerializeAudioMode());
        }

        [Test]
        public void WhenColorDepthSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.project.ColorDepth = RdpColorDepth.HighColor;

            zoneA.ColorDepth = RdpColorDepth.DeepColor;
            Assert.AreEqual(RdpColorDepth.DeepColor, zoneA.ColorDepth);
            Assert.IsTrue(zoneA.ShouldSerializeColorDepth());

            Assert.AreEqual(RdpColorDepth.DeepColor, instanceA.ColorDepth);
            Assert.AreEqual(RdpColorDepth.DeepColor, instanceA.CreateConnectionSettings("instance").ColorDepth);
            Assert.IsFalse(instanceA.ShouldSerializeColorDepth());
        }

        [Test]
        public void WhenColorDepthSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.project.ColorDepth = RdpColorDepth.DeepColor;
            Assert.AreNotEqual(RdpColorDepth._Default, this.project.ColorDepth);

            instanceA.ColorDepth = RdpColorDepth._Default;

            Assert.AreEqual(this.project.ColorDepth, instanceA.ColorDepth);
            Assert.AreEqual(this.project.ColorDepth, instanceA.CreateConnectionSettings("instance").ColorDepth);
            Assert.IsFalse(instanceA.ShouldSerializeColorDepth());
        }

        [Test]
        public void WhenConnectionBarSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.project.ConnectionBar = RdpConnectionBarState.Off;

            zoneA.ConnectionBar = RdpConnectionBarState.Pinned;
            Assert.AreEqual(RdpConnectionBarState.Pinned, zoneA.ConnectionBar);
            Assert.IsTrue(zoneA.ShouldSerializeConnectionBar());

            Assert.AreEqual(RdpConnectionBarState.Pinned, instanceA.ConnectionBar);
            Assert.AreEqual(RdpConnectionBarState.Pinned, instanceA.CreateConnectionSettings("instance").ConnectionBar);
            Assert.IsFalse(instanceA.ShouldSerializeConnectionBar());
        }

        [Test]
        public void WhenConnectionBarSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.project.ConnectionBar = RdpConnectionBarState.Off;
            Assert.AreNotEqual(RdpConnectionBarState._Default, this.project.ConnectionBar);

            instanceA.ConnectionBar = RdpConnectionBarState._Default;

            Assert.AreEqual(this.project.ConnectionBar, instanceA.ConnectionBar);
            Assert.AreEqual(this.project.ConnectionBar, instanceA.CreateConnectionSettings("instance").ConnectionBar);
            Assert.IsFalse(instanceA.ShouldSerializeConnectionBar());
        }

        [Test]
        public void WhenCredentialGenerationBehaviorSetInProjectAndZone_ZoneValueIsInheritedDownToVm()
        {
            this.project.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Disallow;

            zoneA.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Allow;
            Assert.AreEqual(RdpCredentialGenerationBehavior.Allow, zoneA.CredentialGenerationBehavior);
            Assert.IsTrue(zoneA.ShouldSerializeCredentialGenerationBehavior());

            Assert.AreEqual(RdpCredentialGenerationBehavior.Allow, instanceA.CredentialGenerationBehavior);
            Assert.AreEqual(RdpCredentialGenerationBehavior.Allow, instanceA.CreateConnectionSettings("instance").CredentialGenerationBehavior);
            Assert.IsFalse(instanceA.ShouldSerializeCredentialGenerationBehavior());
        }

        [Test]
        public void WhenCredentialGenerationBehaviorSetInProjectAndResetToDefaultInVm_InheritedValueStillApplies()
        {
            this.project.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Allow;
            Assert.AreNotEqual(RdpCredentialGenerationBehavior._Default, this.project.CredentialGenerationBehavior);

            instanceA.CredentialGenerationBehavior = RdpCredentialGenerationBehavior._Default;

            Assert.AreEqual(this.project.CredentialGenerationBehavior, instanceA.CredentialGenerationBehavior);
            Assert.AreEqual(this.project.CredentialGenerationBehavior, instanceA.CreateConnectionSettings("instance").CredentialGenerationBehavior);
            Assert.IsFalse(instanceA.ShouldSerializeCredentialGenerationBehavior());
        }
    }
}
