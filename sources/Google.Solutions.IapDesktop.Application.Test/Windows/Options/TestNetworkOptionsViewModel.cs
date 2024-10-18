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

using Google.Solutions.Common.Linq;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows.Options;
using Google.Solutions.Settings;
using Google.Solutions.Testing.Apis.Platform;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Options
{
    [TestFixture]
    public class TestNetworkOptionsViewModel : ApplicationFixtureBase
    {
        private static ApplicationSettingsRepository CreateSettingsRepository(
            RegistryKey settingsKey,
            IDictionary<string, object>? policies = null)
        {
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
        // System proxy.
        //---------------------------------------------------------------------

        [Test]
        public void Proxy_WhenNoProxyConfigured()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object);

                Assert.IsTrue(viewModel.IsSystemProxyServerEnabled);
                Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
                Assert.IsNull(viewModel.ProxyServer);
                Assert.IsNull(viewModel.ProxyPort);
                Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);

                Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
                Assert.IsNull(viewModel.ProxyUsername);
                Assert.IsNull(viewModel.ProxyPassword);

                Assert.IsFalse(viewModel.IsDirty.Value);
                Assert.IsTrue(viewModel.IsSystemProxyServerEnabled);
            }
        }

        //---------------------------------------------------------------------
        // Custom proxy.
        //---------------------------------------------------------------------

        [Test]
        public void Proxy_WhenCustomProxyConfigured()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var settings = settingsRepository.GetSettings();
                settings.ProxyUrl.Value = "http://proxy-server";
                settingsRepository.SetSettings(settings);

                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object);

                Assert.IsFalse(viewModel.IsSystemProxyServerEnabled);
                Assert.IsFalse(viewModel.IsProxyAutoConfigurationEnabled);
                Assert.IsTrue(viewModel.IsCustomProxyServerEnabled);
                Assert.AreEqual("proxy-server", viewModel.ProxyServer);
                Assert.AreEqual("80", viewModel.ProxyPort);
                Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);
                Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
                Assert.IsNull(viewModel.ProxyUsername);
                Assert.IsNull(viewModel.ProxyPassword);
                Assert.IsFalse(viewModel.IsDirty.Value);
                Assert.IsTrue(viewModel.IsProxyEditable);
            }
        }

        [Test]
        public void Proxy_WhenCustomProxyConfiguredButInvalid()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                // Store an invalid URL.
                settingsKey.SetValue("ProxyUrl", "123", RegistryValueKind.String);

                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object);

                Assert.IsTrue(viewModel.IsSystemProxyServerEnabled);
                Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
                Assert.IsFalse(viewModel.IsProxyAutoConfigurationEnabled);
                Assert.IsNull(viewModel.ProxyServer);
                Assert.IsNull(viewModel.ProxyPort);
                Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);
                Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
                Assert.IsNull(viewModel.ProxyUsername);
                Assert.IsNull(viewModel.ProxyPassword);
                Assert.IsFalse(viewModel.IsDirty.Value);
                Assert.IsTrue(viewModel.IsProxyEditable);
            }
        }

        [Test]
        public void Proxy_WhenCustomProxyConfiguredByPolicy()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(
                    settingsKey,
                    new Dictionary<string, object>
                    {
                        { "ProxyUrl", "http://proxy-server"}
                    });

                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object);

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
                Assert.IsFalse(viewModel.IsDirty.Value);
            }
        }

        [Test]
        public void Proxy_WhenEnablingCustomProxy_ThenProxyHostAndPortSetToDefaults()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
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
                Assert.IsTrue(viewModel.IsDirty.Value);
            }
        }

        [Test]
        public void Proxy_WhenDisablingCustomProxy_ThenProxyHostAndPortAreCleared()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
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
                Assert.IsTrue(viewModel.IsDirty.Value);
            }
        }

        //---------------------------------------------------------------------
        // Proxy autoconfig.
        //---------------------------------------------------------------------

        [Test]
        public void ProxyAutoConfiguration_WhenProxyAutoconfigConfigured()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var settings = settingsRepository.GetSettings();
                settings.ProxyPacUrl.Value = "http://proxy-server/proxy.pac";
                settingsRepository.SetSettings(settings);

                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object);

                Assert.IsFalse(viewModel.IsSystemProxyServerEnabled);
                Assert.IsTrue(viewModel.IsProxyAutoConfigurationEnabled);
                Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
                Assert.IsNull(viewModel.ProxyServer);
                Assert.IsNull(viewModel.ProxyPort);
                Assert.AreEqual("http://proxy-server/proxy.pac", viewModel.ProxyAutoconfigurationAddress);
                Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
                Assert.IsNull(viewModel.ProxyUsername);
                Assert.IsNull(viewModel.ProxyPassword);
                Assert.IsFalse(viewModel.IsDirty.Value);
                Assert.IsTrue(viewModel.IsProxyEditable);
            }
        }

        [Test]
        public void ProxyAutoConfiguration_WhenProxyAutoconfigConfiguredButInvalid()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                // Store an invalid URL.
                settingsKey.SetValue("ProxyPacUrl", "123", RegistryValueKind.String);

                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object);

                Assert.IsTrue(viewModel.IsSystemProxyServerEnabled);
                Assert.IsFalse(viewModel.IsCustomProxyServerEnabled);
                Assert.IsFalse(viewModel.IsProxyAutoConfigurationEnabled);
                Assert.IsNull(viewModel.ProxyServer);
                Assert.IsNull(viewModel.ProxyPort);
                Assert.IsNull(viewModel.ProxyAutoconfigurationAddress);
                Assert.IsFalse(viewModel.IsProxyAuthenticationEnabled);
                Assert.IsNull(viewModel.ProxyUsername);
                Assert.IsNull(viewModel.ProxyPassword);
                Assert.IsFalse(viewModel.IsDirty.Value);
                Assert.IsTrue(viewModel.IsProxyEditable);
            }
        }

        [Test]
        public void ProxyAutoConfiguration_WhenProxyAutoconfigConfiguredByPolicy()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(
                    settingsKey,
                    new Dictionary<string, object>
                    {
                        { "ProxyPacUrl", "http://proxy-server/proxy.pac"}
                    });
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object);

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
                Assert.IsFalse(viewModel.IsDirty.Value);
            }
        }

        [Test]
        public void ProxyAutoConfiguration_WhenEnablingProxyAutoconfig_ThenProxyHostAndPortSetToDefaults()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
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
                Assert.IsTrue(viewModel.IsDirty.Value);
            }
        }

        [Test]
        public void ProxyAutoConfiguration_WhenDisablingProxyAutoconfig_ThenProxyHostAndPortAreCleared()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
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
                Assert.IsTrue(viewModel.IsDirty.Value);
            }
        }

        //---------------------------------------------------------------------
        // Proxy auth.
        //---------------------------------------------------------------------

        [Test]
        public void ProxyAuthentication_WhenProxyAuthConfigured()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var settings = settingsRepository.GetSettings();
                settings.ProxyUrl.Value = "http://proxy-server";
                settings.ProxyUsername.Value = "user";
                settings.ProxyPassword.SetClearTextValue("pass");
                settingsRepository.SetSettings(settings);

                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object);

                Assert.IsFalse(viewModel.IsSystemProxyServerEnabled);
                Assert.IsTrue(viewModel.IsCustomProxyServerEnabled);
                Assert.AreEqual("proxy-server", viewModel.ProxyServer);
                Assert.AreEqual("80", viewModel.ProxyPort);
                Assert.IsTrue(viewModel.IsProxyAuthenticationEnabled);
                Assert.AreEqual("user", viewModel.ProxyUsername);
                Assert.AreEqual("pass", viewModel.ProxyPassword);
                Assert.IsFalse(viewModel.IsDirty.Value);
            }
        }

        [Test]
        public void ProxyAuthentication_WhenEnablingProxyAuth_ThenProxyUsernameSetToDefault()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
                {
                    IsCustomProxyServerEnabled = true,
                    IsProxyAuthenticationEnabled = true
                };

                Assert.AreEqual(Environment.UserName, viewModel.ProxyUsername);
                Assert.IsNull(viewModel.ProxyPassword);
                Assert.IsTrue(viewModel.IsDirty.Value);
            }
        }

        [Test]
        public void ProxyAuthentication_WhenDisablingProxyAuth_ThenProxyUsernameAndPasswordAreCleared()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
                {
                    IsCustomProxyServerEnabled = true,
                    IsProxyAuthenticationEnabled = true,
                    ProxyUsername = "user",
                    ProxyPassword = "pass"
                };
                viewModel.IsProxyAuthenticationEnabled = false;

                Assert.IsNull(viewModel.ProxyUsername);
                Assert.IsNull(viewModel.ProxyPassword);
                Assert.IsTrue(viewModel.IsDirty.Value);
            }
        }

        //---------------------------------------------------------------------
        // ApplyChanges.
        //---------------------------------------------------------------------

        [Test]
        public void ApplyChanges_WhenProxyServerInvalid_ThenApplyChangesThrowsArgumentException()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
                {
                    IsCustomProxyServerEnabled = true,
                    ProxyServer = " .",
                    ProxyPort = "442"
                };

                Assert.Throws<ArgumentException>(() => viewModel.ApplyChangesAsync().Wait());
            }
        }

        [Test]
        public void ApplyChanges_WhenProxyPortIsZero_ThenApplyChangesThrowsArgumentException()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
                {
                    IsCustomProxyServerEnabled = true,
                    ProxyServer = "proxy",
                    ProxyPort = "0"
                };

                Assert.Throws<ArgumentException>(() => viewModel.ApplyChangesAsync().Wait());
            }
        }

        [Test]
        public void ApplyChanges_WhenProxyPortIsOutOfBounds_ThenApplyChangesThrowsArgumentException()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
                {
                    IsCustomProxyServerEnabled = true,
                    ProxyServer = "proxy",
                    ProxyPort = "70000"
                };

                Assert.Throws<ArgumentException>(() => viewModel.ApplyChangesAsync().Wait());
            }
        }

        [Test]
        public void ApplyChanges_WhenProxyAutoconfigUrlInvalid_ThenApplyChangesThrowsArgumentException()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
                {
                    IsProxyAutoConfigurationEnabled = true,
                    ProxyAutoconfigurationAddress = "file:///proxy.pac"
                };

                Assert.Throws<ArgumentException>(() => viewModel.ApplyChangesAsync().Wait());
            }
        }

        [Test]
        public void ApplyChanges_WhenProxyAuthIncomplete_ThenApplyChangesThrowsArgumentException()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
                {
                    IsCustomProxyServerEnabled = true,
                    ProxyServer = "proxy",
                    ProxyPort = "1000",
                    ProxyPassword = "pass"
                };

                Assert.Throws<ArgumentException>(() => viewModel.ApplyChangesAsync().Wait());
            }
        }

        [Test]
        public async Task ApplyChanges_WhenEnablingCustomProxy_ThenProxyAdapterIsUpdated()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var proxyAdapter = new Mock<IHttpProxyAdapter>();
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    proxyAdapter.Object)
                {
                    IsCustomProxyServerEnabled = true
                };

                await viewModel.ApplyChangesAsync();

                proxyAdapter.Verify(m => m.ActivateSettings(
                        It.IsAny<IApplicationSettings>()), Times.Once);
            }
        }

        [Test]
        public async Task ApplyChanges_WhenEnablingOrDisablingCustomProxy_ThenSettingsAreSaved()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
                {
                    // Enable proxy with authentication.
                    IsCustomProxyServerEnabled = true,
                    ProxyServer = " prx ", // Spaces should be ignored.
                    ProxyPort = " 123 ",   // Spaces should be ignored.
                    ProxyUsername = "user",
                    ProxyPassword = "pass"
                };

                await viewModel.ApplyChangesAsync();

                var settings = settingsRepository.GetSettings();
                Assert.AreEqual("http://prx:123", settings.ProxyUrl.Value);
                Assert.AreEqual("user", settings.ProxyUsername.Value);
                Assert.AreEqual("pass", settings.ProxyPassword.GetClearTextValue());

                // Disable authentication.
                viewModel.IsProxyAuthenticationEnabled = false;
                await viewModel.ApplyChangesAsync();

                settings = settingsRepository.GetSettings();
                Assert.AreEqual("http://prx:123", settings.ProxyUrl.Value);
                Assert.IsNull(settings.ProxyUsername.Value);
                Assert.IsNull(settings.ProxyPassword.GetClearTextValue());

                // Revert to system proxy.
                viewModel.IsSystemProxyServerEnabled = true;
                await viewModel.ApplyChangesAsync();

                settings = settingsRepository.GetSettings();
                Assert.IsNull(settings.ProxyUrl.Value);
                Assert.IsNull(settings.ProxyUsername.Value);
                Assert.IsNull(settings.ProxyPassword.GetClearTextValue());
            }
        }


        [Test]
        public async Task ApplyChanges_WhenEnablingOrDisablingProxyAutoconfig_ThenSettingsAreSaved()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
                {
                    // Enable proxy with authentication.
                    IsProxyAutoConfigurationEnabled = true,
                    ProxyAutoconfigurationAddress = " https://www/proxy.pac ", // Spaces should be ignored.
                    ProxyUsername = "user",
                    ProxyPassword = "pass"
                };
                await viewModel.ApplyChangesAsync();

                var settings = settingsRepository.GetSettings();
                Assert.AreEqual("https://www/proxy.pac", settings.ProxyPacUrl.Value);
                Assert.AreEqual("user", settings.ProxyUsername.Value);
                Assert.AreEqual("pass", settings.ProxyPassword.GetClearTextValue());

                // Disable authentication.
                viewModel.IsProxyAuthenticationEnabled = false;
                await viewModel.ApplyChangesAsync();

                settings = settingsRepository.GetSettings();
                Assert.AreEqual("https://www/proxy.pac", settings.ProxyPacUrl.Value);
                Assert.IsNull(settings.ProxyUsername.Value);
                Assert.IsNull(settings.ProxyPassword.GetClearTextValue());

                // Revert to system proxy.
                viewModel.IsSystemProxyServerEnabled = true;
                await viewModel.ApplyChangesAsync();

                settings = settingsRepository.GetSettings();
                Assert.IsNull(settings.ProxyUrl.Value);
                Assert.IsNull(settings.ProxyUsername.Value);
                Assert.IsNull(settings.ProxyPassword.GetClearTextValue());
            }
        }

        [Test]
        public async Task ApplyChanges_ClearsDirtyFlag()
        {
            using (var settingsKey = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings).CreateKey())
            {
                var settingsRepository = CreateSettingsRepository(settingsKey);
                var viewModel = new NetworkOptionsViewModel(
                    settingsRepository,
                    new Mock<IHttpProxyAdapter>().Object)
                {
                    IsCustomProxyServerEnabled = true
                };

                Assert.IsTrue(viewModel.IsDirty.Value);

                await viewModel.ApplyChangesAsync();

                Assert.IsFalse(viewModel.IsDirty.Value);
            }
        }
    }
}
