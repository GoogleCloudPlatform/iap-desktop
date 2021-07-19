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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Options
{
    [TestFixture]
    public class TestNetworkOptionsViewModel : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private const string TestPolicyKeyPath = @"Software\Google\__TestPolicy";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        private RegistryKey settingsKey;

        private Mock<IHttpProxyAdapter> proxyAdapterMock;

        [SetUp]
        public void SetUp()
        {
            this.proxyAdapterMock = new Mock<IHttpProxyAdapter>();

            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.settingsKey = this.hkcu.CreateSubKey(TestKeyPath);
        }

        private ApplicationSettingsRepository CreateSettingsRepository(
            IDictionary<string, object> policies = null)
        {
            this.hkcu.DeleteSubKeyTree(TestPolicyKeyPath, false);

            var policyKey = this.hkcu.CreateSubKey(TestPolicyKeyPath);
            foreach (var policy in policies.EnsureNotNull())
            {
                policyKey.SetValue(policy.Key, policy.Value);
            }

            return new ApplicationSettingsRepository(this.settingsKey, policyKey);
        }

        //---------------------------------------------------------------------
        // System proxy.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoProxyConfigured_ThenPropertiesAreInitializedCorrectly()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
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
            Assert.IsTrue(viewModel.IsSystemProxyServerEnabled);
        }

        //---------------------------------------------------------------------
        // Custom proxy.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCustomProxyConfigured_ThenPropertiesAreInitializedCorrectly()
        {
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.ProxyUrl.StringValue = "http://proxy-server";
            settingsRepository.SetSettings(settings);

            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
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
            Assert.IsTrue(viewModel.IsProxyEditable);
        }

        [Test]
        public void WhenCustomProxyConfiguredButInvalid_ThenDefaultsAreUsed()
        {
            // Store an invalid URL.
            this.settingsKey.SetValue("ProxyUrl", "123", RegistryValueKind.String);

            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
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
            Assert.IsTrue(viewModel.IsProxyEditable);
        }

        [Test]
        public void WhenCustomProxyConfiguredByPolicy_ThenIsProxyEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "ProxyUrl", "http://proxy-server"}
                });
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object);

            Assert.IsFalse(viewModel.IsProxyEditable);
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
        public void WhenEnablingCustomProxy_ThenProxyHostAndPortSetToDefaults()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsCustomProxyServerEnabled = true
            };

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
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsCustomProxyServerEnabled = true,
                ProxyServer = "myserver",
                ProxyPort = "442",
                IsSystemProxyServerEnabled = true
            };

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
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.ProxyPacUrl.StringValue = "http://proxy-server/proxy.pac";
            settingsRepository.SetSettings(settings);

            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
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
            Assert.IsTrue(viewModel.IsProxyEditable);
        }

        [Test]
        public void WhenProxyAutoconfigConfiguredButInvalid_ThenDefaultsAreUsed()
        {
            // Store an invalid URL.
            this.settingsKey.SetValue("ProxyPacUrl", "123", RegistryValueKind.String);

            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
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
            Assert.IsTrue(viewModel.IsProxyEditable);
        }

        [Test]
        public void WhenProxyAutoconfigConfiguredByPolicy_ThenIsProxyEditableIsFalse()
        {
            var settingsRepository = CreateSettingsRepository(
                new Dictionary<string, object>
                {
                    { "ProxyPacUrl", "http://proxy-server/proxy.pac"}
                });
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object);

            Assert.IsFalse(viewModel.IsProxyEditable);

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
        public void WhenEnablingProxyAutoconfig_ThenProxyHostAndPortSetToDefaults()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsProxyAutoConfigurationEnabled = true
            };

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
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsProxyAutoConfigurationEnabled = true,
                ProxyAutoconfigurationAddress = "http://proxy-server/proxy.pac",
                IsSystemProxyServerEnabled = true
            };

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
            var settingsRepository = CreateSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.ProxyUrl.StringValue = "http://proxy-server";
            settings.ProxyUsername.StringValue = "user";
            settings.ProxyPassword.ClearTextValue = "pass";
            settingsRepository.SetSettings(settings);

            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
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
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsCustomProxyServerEnabled = true,
                IsProxyAuthenticationEnabled = true
            };

            Assert.AreEqual(Environment.UserName, viewModel.ProxyUsername);
            Assert.IsNull(viewModel.ProxyPassword);
            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenDisablingProxyAuth_ThenProxyUsernameAndPasswordAreCleared()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsCustomProxyServerEnabled = true,
                IsProxyAuthenticationEnabled = true,
                ProxyUsername = "user",
                ProxyPassword = "pass"
            };
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
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsCustomProxyServerEnabled = true,
                ProxyServer = " .",
                ProxyPort = "442"
            };

            Assert.Throws<ArgumentException>(() => viewModel.ApplyChanges());
        }

        [Test]
        public void WhenProxyPortIsZero_ThenApplyChangesThrowsArgumentException()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsCustomProxyServerEnabled = true,
                ProxyServer = "proxy",
                ProxyPort = "0"
            };

            Assert.Throws<ArgumentException>(() => viewModel.ApplyChanges());
        }

        [Test]
        public void WhenProxyPortIsOutOfBounds_ThenApplyChangesThrowsArgumentException()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsCustomProxyServerEnabled = true,
                ProxyServer = "proxy",
                ProxyPort = "70000"
            };

            Assert.Throws<ArgumentException>(() => viewModel.ApplyChanges());
        }

        [Test]
        public void WhenProxyAutoconfigUrlInvalid_ThenApplyChangesThrowsArgumentException()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsProxyAutoConfigurationEnabled = true,
                ProxyAutoconfigurationAddress = "file:///proxy.pac"
            };

            Assert.Throws<ArgumentException>(() => viewModel.ApplyChanges());
        }

        [Test]
        public void WhenProxyAuthIncomplete_ThenApplyChangesThrowsArgumentException()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsCustomProxyServerEnabled = true,
                ProxyServer = "proxy",
                ProxyPort = "1000",
                ProxyPassword = "pass"
            };

            Assert.Throws<ArgumentException>(() => viewModel.ApplyChanges());
        }

        [Test]
        public void WhenEnablingCustomProxy_ThenProxyAdapterIsUpdated()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsCustomProxyServerEnabled = true
            };
            viewModel.ApplyChanges();

            this.proxyAdapterMock.Verify(m => m.ActivateSettings(
                    It.IsAny<ApplicationSettings>()), Times.Once);
        }

        [Test]
        public void WhenEnablingOrDisablingCustomProxy_ThenSettingsAreSaved()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {

                // Enable proxy with authentication.
                IsCustomProxyServerEnabled = true,
                ProxyServer = "prx",
                ProxyPort = "123",
                ProxyUsername = "user",
                ProxyPassword = "pass"
            };
            viewModel.ApplyChanges();

            var settings = settingsRepository.GetSettings();
            Assert.AreEqual("http://prx:123", settings.ProxyUrl.StringValue);
            Assert.AreEqual("user", settings.ProxyUsername.StringValue);
            Assert.AreEqual("pass", settings.ProxyPassword.ClearTextValue);

            // Disable authentication.
            viewModel.IsProxyAuthenticationEnabled = false;
            viewModel.ApplyChanges();

            settings = settingsRepository.GetSettings();
            Assert.AreEqual("http://prx:123", settings.ProxyUrl.StringValue);
            Assert.IsNull(settings.ProxyUsername.StringValue);
            Assert.IsNull(settings.ProxyPassword.ClearTextValue);

            // Revert to system proxy.
            viewModel.IsSystemProxyServerEnabled = true;
            viewModel.ApplyChanges();

            settings = settingsRepository.GetSettings();
            Assert.IsNull(settings.ProxyUrl.StringValue);
            Assert.IsNull(settings.ProxyUsername.StringValue);
            Assert.IsNull(settings.ProxyPassword.ClearTextValue);
        }


        [Test]
        public void WhenEnablingOrDisablingProxyAutoconfig_ThenSettingsAreSaved()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {

                // Enable proxy with authentication.
                IsProxyAutoConfigurationEnabled = true,
                ProxyAutoconfigurationAddress = "https://www/proxy.pac",
                ProxyUsername = "user",
                ProxyPassword = "pass"
            };
            viewModel.ApplyChanges();

            var settings = settingsRepository.GetSettings();
            Assert.AreEqual("https://www/proxy.pac", settings.ProxyPacUrl.StringValue);
            Assert.AreEqual("user", settings.ProxyUsername.StringValue);
            Assert.AreEqual("pass", settings.ProxyPassword.ClearTextValue);

            // Disable authentication.
            viewModel.IsProxyAuthenticationEnabled = false;
            viewModel.ApplyChanges();

            settings = settingsRepository.GetSettings();
            Assert.AreEqual("https://www/proxy.pac", settings.ProxyPacUrl.StringValue);
            Assert.IsNull(settings.ProxyUsername.StringValue);
            Assert.IsNull(settings.ProxyPassword.ClearTextValue);

            // Revert to system proxy.
            viewModel.IsSystemProxyServerEnabled = true;
            viewModel.ApplyChanges();

            settings = settingsRepository.GetSettings();
            Assert.IsNull(settings.ProxyUrl.StringValue);
            Assert.IsNull(settings.ProxyUsername.StringValue);
            Assert.IsNull(settings.ProxyPassword.ClearTextValue);
        }

        [Test]
        public void WhenChangesApplied_ThenDirtyFlagIsCleared()
        {
            var settingsRepository = CreateSettingsRepository();
            var viewModel = new NetworkOptionsViewModel(
                settingsRepository,
                this.proxyAdapterMock.Object)
            {
                IsCustomProxyServerEnabled = true
            };

            Assert.IsTrue(viewModel.IsDirty);

            viewModel.ApplyChanges();

            Assert.IsFalse(viewModel.IsDirty);
        }
    }
}
