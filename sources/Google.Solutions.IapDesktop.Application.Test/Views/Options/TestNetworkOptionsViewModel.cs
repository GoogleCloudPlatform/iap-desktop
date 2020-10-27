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
    public class TestNetworkOptionsViewModel : FixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private ApplicationSettingsRepository settingsRepository;
        private Mock<IHttpProxyAdapter> proxyAdapterMock;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            
            this.settingsRepository = new ApplicationSettingsRepository(baseKey);
            this.proxyAdapterMock = new Mock<IHttpProxyAdapter>();
        }

        //---------------------------------------------------------------------
        // Update check.
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
            Assert.IsFalse(viewModel.IsDirty);
        }

        [Test]
        public void WhenProxyConfigured_ThenPropertiesAreInitializedCorrectly()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.ProxyUrl.StringValue = "http://proxy-server";
            this.settingsRepository.SetSettings(settings);

            var viewModel = new NetworkOptionsViewModel(
                this.settingsRepository,
                this.proxyAdapterMock.Object);

            Assert.IsFalse(viewModel.IsSystemProxyServerEnabled);
            Assert.IsTrue(viewModel.IsCustomProxyServerEnabled);
            Assert.AreEqual("proxy-server", viewModel.ProxyServer);
            Assert.AreEqual("80", viewModel.ProxyPort);
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
            Assert.AreEqual("proxy", viewModel.ProxyServer);
            Assert.AreEqual("3128", viewModel.ProxyPort);
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
            Assert.IsNull(viewModel.ProxyServer);
            Assert.IsNull(viewModel.ProxyPort);
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
