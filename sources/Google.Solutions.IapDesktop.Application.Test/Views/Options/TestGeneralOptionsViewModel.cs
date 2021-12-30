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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Options
{
    [TestFixture]
    public class TestGeneralOptionsViewModel : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private const string TestMachinePolicyKeyPath = @"Software\Google\__TestMachinePolicy";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private Mock<IAppProtocolRegistry> protocolRegistryMock;

        [SetUp]
        public void SetUp()
        {
            this.protocolRegistryMock = new Mock<IAppProtocolRegistry>();
        }

        private ApplicationSettingsRepository CreateSettingsRepository(
            IDictionary<string, object> policies = null)
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.hkcu.DeleteSubKeyTree(TestMachinePolicyKeyPath, false);

            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var policyKey = this.hkcu.CreateSubKey(TestMachinePolicyKeyPath);
            foreach (var policy in policies.EnsureNotNull())
            {
                policyKey.SetValue(policy.Key, policy.Value);
            }

            return new ApplicationSettingsRepository(baseKey, policyKey, null);
        }

        //---------------------------------------------------------------------
        // Update check.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsUpdateCheckEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.BoolValue = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsTrue(viewModel.IsUpdateCheckEnabled);
            Assert.IsTrue(viewModel.IsUpdateCheckEditable);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsUpdateCheckEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsFalse(viewModel.IsUpdateCheckEnabled);
            Assert.IsTrue(viewModel.IsUpdateCheckEditable);
        }

        [Test]
        public void WhenSettingDisabledByPolicy_ThenIsUpdateCheckEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "IsUpdateCheckEnabled", 0 }
                });

            var settings = settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsFalse(viewModel.IsUpdateCheckEnabled);
            Assert.IsFalse(viewModel.IsUpdateCheckEditable);
        }

        [Test]
        public void WhenDisablingUpdateCheck_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.BoolValue = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService())
            {
                IsUpdateCheckEnabled = false
            };
            viewModel.ApplyChanges();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsUpdateCheckEnabled.BoolValue);
        }

        [Test]
        public void WhenUpdateCheckChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsUpdateCheckEnabled = !viewModel.IsUpdateCheckEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenLastCheckIsZero_ThenLastUpdateCheckReturnsNever()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.AreEqual("never", viewModel.LastUpdateCheck);
        }

        [Test]
        public void WhenLastCheckIsNonZero_ThenLastUpdateCheckReturnsNever()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.LastUpdateCheck.LongValue = 1234567L;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.AreNotEqual("never", viewModel.LastUpdateCheck);
        }

        //---------------------------------------------------------------------
        // DCA.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsDeviceCertificateAuthenticationEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsTrue(viewModel.IsDeviceCertificateAuthenticationEnabled);
            Assert.IsTrue(viewModel.IsDeviceCertificateAuthenticationEditable);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsDeviceCertificateAuthenticationEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsFalse(viewModel.IsDeviceCertificateAuthenticationEnabled);
            Assert.IsTrue(viewModel.IsDeviceCertificateAuthenticationEditable);
        }

        [Test]
        public void WhenSettingEnabledByPolicy_ThenIsDeviceCertificateAuthenticationEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "IsDeviceCertificateAuthenticationEnabled", 1 }
                });

            var settings = settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsTrue(viewModel.IsDeviceCertificateAuthenticationEnabled);
            Assert.IsFalse(viewModel.IsDeviceCertificateAuthenticationEditable);
        }

        [Test]
        public void WhenDisablingDca_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService())
            {
                IsDeviceCertificateAuthenticationEnabled = false
            };
            viewModel.ApplyChanges();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.BoolValue);
        }

        [Test]
        public void WhenDcaChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsDeviceCertificateAuthenticationEnabled = !viewModel.IsDeviceCertificateAuthenticationEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // Browser integration.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBrowserIntegrationChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsBrowserIntegrationEnabled = !viewModel.IsBrowserIntegrationEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenBrowserIntegrationEnabled_ThenApplyChangesRegistersProtocol()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService())
            {
                IsBrowserIntegrationEnabled = true
            };
            viewModel.ApplyChanges();

            this.protocolRegistryMock.Verify(r => r.Register(
                    It.Is<string>(s => s == IapRdpUrl.Scheme),
                    It.Is<string>(s => s == Globals.FriendlyName),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public void WhenBrowserIntegrationDisabled_ThenApplyChangesUnregistersProtocol()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService())
            {
                IsBrowserIntegrationEnabled = false
            };
            viewModel.ApplyChanges();

            this.protocolRegistryMock.Verify(r => r.Unregister(
                    It.Is<string>(s => s == IapRdpUrl.Scheme)),
                Times.Once);
        }
    }
}
