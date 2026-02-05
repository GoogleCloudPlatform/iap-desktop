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

using Google.Solutions.Common;
using Google.Solutions.Common.Linq;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows.Options;
using Google.Solutions.Platform.Net;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Testing.Apis.Platform;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Options
{
    [TestFixture]
    public class TestGeneralOptionsViewModel : ApplicationFixtureBase
    {
        private IRepository<IApplicationSettings> CreateSettingsRepository(
            IDictionary<string, object>? policies = null)
        {
            var settingsKey = RegistryKeyPath
                .ForCurrentTest(RegistryKeyPath.KeyType.Settings)
                .CreateKey();

            var policyKey = RegistryKeyPath
                .ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy)
                .CreateKey();

            foreach (var policy in policies.EnsureNotNull())
            {
                policyKey.SetValue(policy.Key, policy.Value);
            }

            return new ApplicationSettingsRepository(
                settingsKey,
                policyKey,
                null,
                UserProfile.SchemaVersion.Current);
        }

        //---------------------------------------------------------------------
        // Update check.
        //---------------------------------------------------------------------

        [Test]
        public void UpdateCheck_WhenSettingEnabled_ThenIsUpdateCheckEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.IsUpdateCheckEnabled.Value, Is.True);
            Assert.That(viewModel.IsUpdateCheckEditable.Value, Is.True);
        }

        [Test]
        public void UpdateCheck_WhenSettingDisabled_ThenIsUpdateCheckEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.IsUpdateCheckEnabled.Value, Is.False);
            Assert.That(viewModel.IsUpdateCheckEditable.Value, Is.True);
        }

        [Test]
        public void UpdateCheck_WhenSettingDisabledByPolicy_ThenIsUpdateCheckEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "IsUpdateCheckEnabled", 0 }
                });

            var settings = settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.IsUpdateCheckEnabled.Value, Is.False);
            Assert.That(viewModel.IsUpdateCheckEditable.Value, Is.False);
        }

        [Test]
        public async Task UpdateCheck_WhenDisablingUpdateCheck_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());
            viewModel.IsUpdateCheckEnabled.Value = false;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.That(settings.IsUpdateCheckEnabled.Value, Is.False);
        }

        [Test]
        public void UpdateCheck_WhenUpdateCheckChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.IsUpdateCheckEnabled.Value = !viewModel.IsUpdateCheckEnabled.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }

        [Test]
        public void UpdateCheck_WhenLastCheckIsZero_ThenLastUpdateCheckReturnsNever()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.LastUpdateCheck, Is.EqualTo("never"));
        }

        [Test]
        public void UpdateCheck_WhenLastCheckIsNonZero_ThenLastUpdateCheckReturnsNever()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.LastUpdateCheck.Value = 1234567L;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.LastUpdateCheck, Is.Not.EqualTo("never"));
        }

        //---------------------------------------------------------------------
        // Browser integration.
        //---------------------------------------------------------------------

        [Test]
        public void BrowserIntegration_WhenBrowserIntegrationChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.IsBrowserIntegrationEnabled.Value =
                !viewModel.IsBrowserIntegrationEnabled.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }

        [Test]
        public async Task BrowserIntegration_WhenBrowserIntegrationEnabled_ThenApplyChangesRegistersProtocol()
        {
            var settingsRepository = CreateSettingsRepository();
            var protocolRegistryMock = new Mock<IBrowserProtocolRegistry>();

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                protocolRegistryMock.Object,
                new HelpClient());
            viewModel.IsBrowserIntegrationEnabled.Value = true;

            await viewModel.ApplyChangesAsync();

            protocolRegistryMock.Verify(r => r.Register(
                    It.Is<string>(s => s == IapRdpUrl.Scheme),
                    It.Is<string>(s => s == Install.ProductName),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public async Task BrowserIntegration_WhenBrowserIntegrationDisabled_ThenApplyChangesUnregistersProtocol()
        {
            var settingsRepository = CreateSettingsRepository();
            var protocolRegistryMock = new Mock<IBrowserProtocolRegistry>();

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                protocolRegistryMock.Object,
                new HelpClient());
            viewModel.IsBrowserIntegrationEnabled.Value = false;

            await viewModel.ApplyChangesAsync();

            protocolRegistryMock.Verify(r => r.Unregister(
                    It.Is<string>(s => s == IapRdpUrl.Scheme)),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // Telemetry.
        //---------------------------------------------------------------------

        [Test]
        public void Telemetry_WhenSettingEnabled_ThenIsTelemetryEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsTelemetryEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.IsTelemetryEnabled.Value, Is.True);
            Assert.That(viewModel.IsTelemetryEditable.Value, Is.True);
        }

        [Test]
        public void Telemetry_WhenSettingDisabled_ThenIsTelemetryEnabledIsTrue()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsTelemetryEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.IsTelemetryEnabled.Value, Is.False);
            Assert.That(viewModel.IsTelemetryEditable.Value, Is.True);
        }

        [Test]
        public void Telemetry_WhenSettingDisabledByPolicy_ThenIsTelemetryEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "IsTelemetryEnabled", 0 }
                });

            var settings = settingsRepository.GetSettings();
            settings.IsTelemetryEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.IsTelemetryEnabled.Value, Is.False);
            Assert.That(viewModel.IsTelemetryEditable.Value, Is.False);
        }

        [Test]
        public async Task Telemetry_WhenEnablingTelemetry_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsTelemetryEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            TelemetryLog.Current.Enabled = false;

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());
            viewModel.IsTelemetryEnabled.Value = true;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.That(settings.IsTelemetryEnabled.Value, Is.True);
            Assert.That(TelemetryLog.Current.Enabled, Is.True);
        }

        [Test]
        public async Task Telemetry_WhenDisablingTelemetry_ThenChangeIsApplied()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsTelemetryEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            TelemetryLog.Current.Enabled = true;

            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());
            viewModel.IsTelemetryEnabled.Value = false;

            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.That(settings.IsTelemetryEnabled.Value, Is.False);
            Assert.That(TelemetryLog.Current.Enabled, Is.False);
        }

        [Test]
        public void IsDirty_WhenTelemetryChanged()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new GeneralOptionsViewModel(
                settingsRepository,
                new Mock<IBrowserProtocolRegistry>().Object,
                new HelpClient());

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.IsTelemetryEnabled.Value = !viewModel.IsTelemetryEnabled.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }
    }
}
