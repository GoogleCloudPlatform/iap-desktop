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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Host.Adapters;
using Google.Solutions.IapDesktop.Application.Windows.Options;
using Google.Solutions.Platform.Net;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Solutions.IapDesktop.Application.Host.Diagnostics;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Options
{
    [TestFixture]
    public class TestGeneralOptionsViewModel : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private const string TestMachinePolicyKeyPath = @"Software\Google\__TestMachinePolicy";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private Mock<IBrowserProtocolRegistry> protocolRegistryMock;
        private Mock<ITelemetryCollector> telemetryCollectorMock;

        [SetUp]
        public void SetUp()
        {
            this.protocolRegistryMock = new Mock<IBrowserProtocolRegistry>();
            this.telemetryCollectorMock = new Mock<ITelemetryCollector>();
        }

        private IRepository<IApplicationSettings> CreateSettingsRepository(
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
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

            Assert.IsTrue(viewModel.IsUpdateCheckEnabled.Value);
            Assert.IsTrue(viewModel.IsUpdateCheckEditable.Value);
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
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

            Assert.IsFalse(viewModel.IsUpdateCheckEnabled.Value);
            Assert.IsTrue(viewModel.IsUpdateCheckEditable.Value);
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
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

            Assert.IsFalse(viewModel.IsUpdateCheckEnabled.Value);
            Assert.IsFalse(viewModel.IsUpdateCheckEditable.Value);
        }

        [Test]
        public async Task WhenDisablingUpdateCheck_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.BoolValue = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                this.telemetryCollectorMock.Object,
                new HelpAdapter());
            viewModel.IsUpdateCheckEnabled.Value = false;

            await viewModel.ApplyChangesAsync();

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
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsUpdateCheckEnabled.Value = !viewModel.IsUpdateCheckEnabled.Value;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        [Test]
        public void WhenLastCheckIsZero_ThenLastUpdateCheckReturnsNever()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

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
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

            Assert.AreNotEqual("never", viewModel.LastUpdateCheck);
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
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsBrowserIntegrationEnabled.Value =
                !viewModel.IsBrowserIntegrationEnabled.Value;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        [Test]
        public async Task WhenBrowserIntegrationEnabled_ThenApplyChangesRegistersProtocol()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                this.telemetryCollectorMock.Object,
                new HelpAdapter());
            viewModel.IsBrowserIntegrationEnabled.Value = true;

            await viewModel.ApplyChangesAsync();

            this.protocolRegistryMock.Verify(r => r.Register(
                    It.Is<string>(s => s == IapRdpUrl.Scheme),
                    It.Is<string>(s => s == Install.ProductName),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public async Task WhenBrowserIntegrationDisabled_ThenApplyChangesUnregistersProtocol()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                this.telemetryCollectorMock.Object,
                new HelpAdapter());
            viewModel.IsBrowserIntegrationEnabled.Value = false;

            await viewModel.ApplyChangesAsync();

            this.protocolRegistryMock.Verify(r => r.Unregister(
                    It.Is<string>(s => s == IapRdpUrl.Scheme)),
                Times.Once);
        }


        //---------------------------------------------------------------------
        // Telemetry.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsTelemetryEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsTelemetryEnabled.BoolValue = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

            Assert.IsTrue(viewModel.IsTelemetryEnabled.Value);
            Assert.IsTrue(viewModel.IsTelemetryEditable.Value);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsTelemetryEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsTelemetryEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

            Assert.IsFalse(viewModel.IsTelemetryEnabled.Value);
            Assert.IsTrue(viewModel.IsTelemetryEditable.Value);
        }

        [Test]
        public void WhenSettingDisabledByPolicy_ThenIsTelemetryEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "IsTelemetryEnabled", 0 }
                });

            var settings = settingsRepository.GetSettings();
            settings.IsTelemetryEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

            Assert.IsFalse(viewModel.IsTelemetryEnabled.Value);
            Assert.IsFalse(viewModel.IsTelemetryEditable.Value);
        }

        [Test]
        public async Task WhenEnablingTelemetry_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsTelemetryEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                this.telemetryCollectorMock.Object,
                new HelpAdapter());
            viewModel.IsTelemetryEnabled.Value = true;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsTrue(settings.IsTelemetryEnabled.BoolValue);

            this.telemetryCollectorMock.VerifySet(t => t.Enabled = true, Times.Once);
        }

        [Test]
        public async Task WhenDisablingTelemetry_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsTelemetryEnabled.BoolValue = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                this.telemetryCollectorMock.Object,
                new HelpAdapter());
            viewModel.IsTelemetryEnabled.Value = false;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsTelemetryEnabled.BoolValue);

            this.telemetryCollectorMock.VerifySet(t => t.Enabled = false, Times.Once);
        }

        [Test]
        public void WhenTelemetryChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                this.protocolRegistryMock.Object,
                this.telemetryCollectorMock.Object,
                new HelpAdapter());

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsTelemetryEnabled.Value = !viewModel.IsTelemetryEnabled.Value;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }
    }
}
