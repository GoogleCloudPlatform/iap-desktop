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

using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Options
{
    [TestFixture]
    public class TestNetworkOptionsViewModel : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        private RegistryKey settingsKey;

        private ApplicationSettingsRepository settingsRepository;
        private Mock<IHttpProxyAdapter> proxyAdapterMock;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.settingsKey = hkcu.CreateSubKey(TestKeyPath);

            this.settingsRepository = new ApplicationSettingsRepository(this.settingsKey);
            this.proxyAdapterMock = new Mock<IHttpProxyAdapter>();
        }

        //---------------------------------------------------------------------
        // System proxy.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoProxyConfigured_ThenPropertiesAreInitializedCorrectly()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            Assert.IsTrue(viewModel.IsSystemProxyServerEnabled);
            Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
            Assert.IsNull(viewModel.ProxyServer);
            Assert.IsNull(viewModel.ProxyPort);
            Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);

            Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
            Assert.IsNull(viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);

            Assert.IsFalse(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // Custom proxy.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCustomProxyConfigured_ThenPropertiesAreInitializedCorrectly()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.ProxyUrl.StringValue = "http://proxy-server";
            this.settingsRepository.SetSettings(settings);

            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            Assert.IsFalse(viewModel.IsSystemProxyServerEnabled);
            Assert.IsFalse(viewModel.IsProxyAutoConfigurationEnabled);
            Assert.IsTrue(viewModel.IsCustomProxyServerEnabled);
            Assert.AreEqual("proxy-server", viewModel.ProxyServer);
            Assert.AreEqual("80", viewModel.ProxyPort);
            Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);
            Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
            Assert.IsNull(viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsFalse(viewModel.IsDirty);
        }

        [Test]
        public void WhenCustomProxyConfiguredButInvalid_ThenDefaultsAreUsed()
        {
            // Store an invalid URL.
            this.settingsKey.SetValue("ProxyUrl", "123", RegistryValueKind.String);

            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            Assert.IsTrue(viewModel.IsSystemProxyServerEnabled);
            Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
            Assert.IsFalse(viewModel.IsProxyAutoConfigurationEnabled);
            Assert.IsNull(viewModel.ProxyServer);
            Assert.IsNull(viewModel.ProxyPort);
            Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);
            Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
            Assert.IsNull(viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsFalse(viewModel.IsDirty);
        }

        [Test]
        public void WhenEnablingCustomProxy_ThenProxyHostAndPortSetToDefaults()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsCustomProxyServerEnabled = true;

            Assert.IsFalse(viewModel.IsSystemProxyServerEnabled);
            Assert.IsTrue(viewModel.IsCustomProxyServerEnabled);
            Assert.IsFalse(viewModel.IsProxyAutoConfigurationEnabled);
            Assert.AreEqual("proxy", viewModel.ProxyServer);
            Assert.AreEqual("3128", viewModel.ProxyPort);
            Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);
            Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
            Assert.IsNull(viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenDisablingCustomProxy_ThenProxyHostAndPortAreCleared()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsCustomProxyServerEnabled = true;
            viewModel.ProxyServer = "myserver";
            viewModel.ProxyPort = "442";
            viewModel.IsSystemProxyServerEnabled = true;

            Assert.IsTrue(viewModel.IsSystemProxyServerEnabled);
            Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
            Assert.IsFalse(viewModel.IsProxyAutoConfigurationEnabled);
            Assert.IsNull(viewModel.ProxyServer);
            Assert.IsNull(viewModel.ProxyPort);
            Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);
            Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
            Assert.IsNull(viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // Proxy autoconfig.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProxyAutoconfigConfigured_ThenPropertiesAreInitializedCorrectly()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.ProxyPacUrl.StringValue = "http://proxy-server/proxy.pac";
            this.settingsRepository.SetSettings(settings);

            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            Assert.IsFalse(viewModel.IsSystemProxyServerEnabled);
            Assert.IsTrue(viewModel.IsProxyAutoConfigurationEnabled);
            Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
            Assert.IsNull(viewModel.ProxyServer);
            Assert.IsNull(viewModel.ProxyPort);
            Assert.AreEqual("http://proxy-server/proxy.pac", viewModel.ProxyAutoconfigurationAddress);
            Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
            Assert.IsNull(viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsFalse(viewModel.IsDirty);
        }

        [Test]
        public void WhenProxyAutoconfigConfiguredButInvalid_ThenDefaultsAreUsed()
        {
            // Store an invalid URL.
            this.settingsKey.SetValue("ProxyPacUrl", "123", RegistryValueKind.String);

            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            Assert.IsTrue(viewModel.IsSystemProxyServerEnabled);
            Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
            Assert.IsFalse(viewModel.IsProxyAutoConfigurationEnabled);
            Assert.IsNull(viewModel.ProxyServer);
            Assert.IsNull(viewModel.ProxyPort);
            Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);
            Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
            Assert.IsNull(viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsFalse(viewModel.IsDirty);
        }

        [Test]
        public void WhenEnablingProxyAutoconfig_ThenProxyHostAndPortSetToDefaults()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsProxyAutoConfigurationEnabled = true;

            Assert.IsFalse(viewModel.IsSystemProxyServerEnabled);
            Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
            Assert.IsTrue(viewModel.IsProxyAutoConfigurationEnabled);
            Assert.IsNull(viewModel.ProxyServer);
            Assert.IsNull(viewModel.ProxyPort);
            Assert.AreEqual("http://proxy/proxy.pac", viewModel.ProxyAutoconfigurationAddress);
            Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
            Assert.IsNull(viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenDisablingProxyAutoconfig_ThenProxyHostAndPortAreCleared()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsProxyAutoConfigurationEnabled = true;
            viewModel.ProxyAutoconfigurationAddress = "http://proxy-server/proxy.pac";
            viewModel.IsSystemProxyServerEnabled = true;

            Assert.IsTrue(viewModel.IsSystemProxyServerEnabled);
            Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
            Assert.IsFalse(viewModel.IsProxyAutoConfigurationEnabled);
            Assert.IsNull(viewModel.ProxyServer);
            Assert.IsNull(viewModel.ProxyPort);
            Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);
            Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
            Assert.IsNull(viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // Proxy auth.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProxyAuthConfigured_ThenPropertiesAreInitializedCorrectly()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.ProxyUrl.StringValue = "http://proxy-server";
            settings.ProxyUsername.StringValue = "user";
            settings.ProxyPassword.ClearTextValue = "pass";
            this.settingsRepository.SetSettings(settings);

            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            Assert.IsFalse(viewModel.IsSystemProxyServerEnabled);
            Assert.IsTrue(viewModel.IsCustomProxyServerEnabled);
            Assert.AreEqual("proxy-server", viewModel.ProxyServer);
            Assert.AreEqual("80", viewModel.ProxyPort);
            Assert.IsTrue(viewModel.IsProxyAuthenticationEnabled);
            Assert.AreEqual("user", viewModel.ProxyUsername);
            Assert.AreEqual("pass", viewModel.ProxyPassword);
            Assert.IsFalse(viewModel.IsDirty);
        }

        [Test]
        public void WhenEnablingProxyAuth_ThenProxyUsernameSetToDefault()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsCustomProxyServerEnabled = true;
            viewModel.IsProxyAuthenticationEnabled = true;

            Assert.AreEqual(Environment.UserName, viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenDisablingProxyAuth_ThenProxyUsernameAndPasswordAreCleared()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsCustomProxyServerEnabled = true;
            viewModel.IsProxyAuthenticationEnabled = true;
            viewModel.ProxyUsername = "user";
            viewModel.ProxyPassword = "pass";
            viewModel.IsProxyAuthenticationEnabled = false;

            Assert.IsNull(viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // ApplyChanges.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProxyServerInvalid_ThenApplyChangesThrowsArgumentException()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsCustomProxyServerEnabled = true;
            viewModel.ProxyServer = " .";
            viewModel.ProxyPort = "442";

            Assert.Throws<ArgumentException>(() => viewModel.ApplyChanges());
        }

        [Test]
        public void WhenProxyPortIsZero_ThenApplyChangesThrowsArgumentException()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsCustomProxyServerEnabled = true;
            viewModel.ProxyServer = "proxy";
            viewModel.ProxyPort = "0";

            Assert.Throws<ArgumentException>(() => viewModel.ApplyChanges());
        }

        [Test]
        public void WhenProxyPortIsOutOfBounds_ThenApplyChangesThrowsArgumentException()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsCustomProxyServerEnabled = true;
            viewModel.ProxyServer = "proxy";
            viewModel.ProxyPort = "70000";

            Assert.Throws<ArgumentException>(() => viewModel.ApplyChanges());
        }

        [Test]
        public void WhenProxyAutoconfigUrlInvalid_ThenApplyChangesThrowsArgumentException()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsProxyAutoConfigurationEnabled = true;
            viewModel.ProxyAutoconfigurationAddress = "file:///proxy.pac";

            Assert.Throws<ArgumentException>(() => viewModel.ApplyChanges());
        }

        [Test]
        public void WhenProxyAuthIncomplete_ThenApplyChangesThrowsArgumentException()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsCustomProxyServerEnabled = true;
            viewModel.ProxyServer = "proxy";
            viewModel.ProxyPort = "1000";
            viewModel.ProxyPassword = "pass";

            Assert.Throws<ArgumentException>(() => viewModel.ApplyChanges());
        }

        [Test]
        public void WhenEnablingCustomProxy_ThenProxyAdapterIsUpdated()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsCustomProxyServerEnabled = true;
            viewModel.ApplyChanges();

            this.proxyAdapterMock.Verify(m => m.ActivateSettings(
                    It.IsAny<ApplicationSettings>()), Times.Once);
        }

        [Test]
        public void WhenEnablingOrDisablingCustomProxy_ThenSettingsAreSaved()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            // Enable proxy with authentication.
            viewModel.IsCustomProxyServerEnabled = true;
            viewModel.ProxyServer = "prx";
            viewModel.ProxyPort = "123";
            viewModel.ProxyUsername = "user";
            viewModel.ProxyPassword = "pass";
            viewModel.ApplyChanges();

            var settings = this.settingsRepository.GetSettings();
            Assert.AreEqual("http://prx:123", settings.ProxyUrl.StringValue);
            Assert.AreEqual("user", settings.ProxyUsername.StringValue);
            Assert.AreEqual("pass", settings.ProxyPassword.ClearTextValue);

            // Disable authentication.
            viewModel.IsProxyAuthenticationEnabled = false;
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.AreEqual("http://prx:123", settings.ProxyUrl.StringValue);
            Assert.IsNull(settings.ProxyUsername.StringValue);
            Assert.IsNull(settings.ProxyPassword.ClearTextValue);

            // Revert to system proxy.
            viewModel.IsSystemProxyServerEnabled = true;
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsNull(settings.ProxyUrl.StringValue);
            Assert.IsNull(settings.ProxyUsername.StringValue);
            Assert.IsNull(settings.ProxyPassword.ClearTextValue);
        }


        [Test]
        public void WhenEnablingOrDisablingProxyAutoconfig_ThenSettingsAreSaved()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            // Enable proxy with authentication.
            viewModel.IsProxyAutoConfigurationEnabled = true;
            viewModel.ProxyAutoconfigurationAddress = "https://www/proxy.pac";
            viewModel.ProxyUsername = "user";
            viewModel.ProxyPassword = "pass";
            viewModel.ApplyChanges();

            var settings = this.settingsRepository.GetSettings();
            Assert.AreEqual("https://www/proxy.pac", settings.ProxyPacUrl.StringValue);
            Assert.AreEqual("user", settings.ProxyUsername.StringValue);
            Assert.AreEqual("pass", settings.ProxyPassword.ClearTextValue);

            // Disable authentication.
            viewModel.IsProxyAuthenticationEnabled = false;
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.AreEqual("https://www/proxy.pac", settings.ProxyPacUrl.StringValue);
            Assert.IsNull(settings.ProxyUsername.StringValue);
            Assert.IsNull(settings.ProxyPassword.ClearTextValue);

            // Revert to system proxy.
            viewModel.IsSystemProxyServerEnabled = true;
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsNull(settings.ProxyUrl.StringValue);
            Assert.IsNull(settings.ProxyUsername.StringValue);
            Assert.IsNull(settings.ProxyPassword.ClearTextValue);
        }

        [Test]
        public void WhenChangesApplied_ThenDirtyFlagIsCleared()
        {
            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            viewModel.IsCustomProxyServerEnabled = true;

            Assert.IsTrue(viewModel.IsDirty);

            viewModel.ApplyChanges();

            Assert.IsFalse(viewModel.IsDirty);
        }
    }
}
