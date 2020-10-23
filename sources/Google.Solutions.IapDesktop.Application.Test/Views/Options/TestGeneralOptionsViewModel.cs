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

using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Options
{
    [TestFixture]
    public class TestGeneralOptionsViewModel : FixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private GeneralOptionsViewModel viewModel;
        private Mock<IAppProtocolRegistry> protocolRegistryMock;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new ApplicationSettingsRepository(baseKey);

            this.protocolRegistryMock = new Mock<IAppProtocolRegistry>();
            this.viewModel = new GeneralOptionsViewModel(
                repository,
                this.protocolRegistryMock.Object);
        }

        //---------------------------------------------------------------------
        // Update check.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUpdateCheckChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsUpdateCheckEnabled = !viewModel.IsUpdateCheckEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // Update check.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBrowserIntegrationChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsBrowserIntegrationEnabled = !viewModel.IsBrowserIntegrationEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenBrowserIntegrationEnabled_ThenApplyChangesRegistersProtocol()
        {
            viewModel.IsBrowserIntegrationEnabled = true;
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
            viewModel.IsBrowserIntegrationEnabled = false;
            viewModel.ApplyChanges();

            this.protocolRegistryMock.Verify(r => r.Unregister(
                    It.Is<string>(s => s == IapRdpUrl.Scheme)), 
                Times.Once);
        }
    }
}
