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

using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Options
{
    [TestFixture]
    public class TestGeneralOptionsViewModel : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private ApplicationSettingsRepository settingsRepository;
        private Mock<IAppProtocolRegistry> protocolRegistryMock;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var baseKey = hkcu.CreateSubKey(TestKeyPath);

            this.settingsRepository = new ApplicationSettingsRepository(baseKey, null);// TODO: Test policy
            this.protocolRegistryMock = new Mock<IAppProtocolRegistry>();
        }

        //---------------------------------------------------------------------
        // Update check.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsUpdateCheckEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsTrue(viewModel.IsUpdateCheckEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsUpdateCheckEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsFalse(viewModel.IsUpdateCheckEnabled);
        }

        [Test]
        public void WhenDisablingUpdateCheck_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsUpdateCheckEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService())
            {
                IsUpdateCheckEnabled = false
            };
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsUpdateCheckEnabled.BoolValue);
        }

        [Test]
        public void WhenUpdateCheckChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsUpdateCheckEnabled = !viewModel.IsUpdateCheckEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenLastCheckIsZero_ThenLastUpdateCheckReturnsNever()
        {
            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.AreEqual("never", viewModel.LastUpdateCheck);
        }

        [Test]
        public void WhenLastCheckIsNonZero_ThenLastUpdateCheckReturnsNever()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.LastUpdateCheck.LongValue = 1234567L;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
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
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsTrue(viewModel.IsDeviceCertificateAuthenticationEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsDeviceCertificateAuthenticationEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsFalse(viewModel.IsDeviceCertificateAuthenticationEnabled);
        }

        [Test]
        public void WhenDisablingDca_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService())
            {
                IsDeviceCertificateAuthenticationEnabled = false
            };
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsDeviceCertificateAuthenticationEnabled.BoolValue);
        }

        [Test]
        public void WhenDcaChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
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
            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService());

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsBrowserIntegrationEnabled = !viewModel.IsBrowserIntegrationEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenBrowserIntegrationEnabled_ThenApplyChangesRegistersProtocol()
        {
            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
                this.protocolRegistryMock.Object,
                new HelpService())
            {
                IsBrowserIntegrationEnabled = true
            };
            viewModel.ApplyChanges();

            this.protocolRegistryMock.Verify(r => r.Register(
                    It.Is<string>(s => s == IapRdpUrl.Scheme),
                    It.Is<string>(s => s == GeneralOptionsViewModel.FriendlyName),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public void WhenBrowserIntegrationDisabled_ThenApplyChangesUnregistersProtocol()
        {
            var viewModel = new GeneralOptionsViewModel(
                this.settingsRepository,
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
