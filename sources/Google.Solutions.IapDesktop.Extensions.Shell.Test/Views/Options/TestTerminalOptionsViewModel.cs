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

using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Options;
using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Options
{
    [TestFixture]
    public class TestTerminalOptionsViewModel : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey
            .OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private TerminalSettingsRepository settingsRepository;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var baseKey = hkcu.CreateSubKey(TestKeyPath);

            this.settingsRepository = new TerminalSettingsRepository(baseKey);
        }

        //---------------------------------------------------------------------
        // IsCopyPasteUsingCtrlCAndCtrlVEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsCopyPasteUsingCtrlCAndCtrlVEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsTrue(viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsCopyPasteUsingCtrlCAndCtrlVEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled);
        }

        [Test]
        public void WhenDisablingIsCopyPasteUsingCtrlCAndCtrlVEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository)
            {
                IsCopyPasteUsingCtrlCAndCtrlVEnabled = false
            };
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue);
        }

        [Test]
        public void WhenIsCopyPasteUsingCtrlCAndCtrlVEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled = !viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }


        //---------------------------------------------------------------------
        // IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsCopyPasteUsingShiftInsertAndCtrlInsertEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsTrue(viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsCopyPasteUsingShiftInsertAndCtrlInsertEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled);
        }

        [Test]
        public void WhenDisablingIsCopyPasteUsingShiftInsertAndCtrlInsertEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository)
            {
                IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled = false
            };
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue);
        }

        [Test]
        public void WhenIsCopyPasteUsingShiftInsertAndCtrlInsertEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled = !viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // IsSelectAllUsingCtrlAEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsSelectAllUsingCtrlAEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsSelectAllUsingCtrlAEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsTrue(viewModel.IsSelectAllUsingCtrlAEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsSelectAllUsingCtrlAEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsSelectAllUsingCtrlAEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsSelectAllUsingCtrlAEnabled);
        }

        [Test]
        public void WhenDisablingIsSelectAllUsingCtrlAEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsSelectAllUsingCtrlAEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository)
            {
                IsSelectAllUsingCtrlAEnabled = false
            };
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsSelectAllUsingCtrlAEnabled.BoolValue);
        }

        [Test]
        public void WhenIsSelectAllUsingCtrlAEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsSelectAllUsingCtrlAEnabled = !viewModel.IsSelectAllUsingCtrlAEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // IsSelectUsingShiftArrrowEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsSelectUsingShiftArrrowEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsSelectUsingShiftArrrowEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsTrue(viewModel.IsSelectUsingShiftArrrowEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsSelectUsingShiftArrrowEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsSelectUsingShiftArrrowEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsSelectUsingShiftArrrowEnabled);
        }

        [Test]
        public void WhenDisablingIsSelectUsingShiftArrrowEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsSelectUsingShiftArrrowEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository)
            {
                IsSelectUsingShiftArrrowEnabled = false
            };
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsSelectUsingShiftArrrowEnabled.BoolValue);
        }

        [Test]
        public void WhenIsSelectUsingShiftArrrowEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsSelectUsingShiftArrrowEnabled = !viewModel.IsSelectUsingShiftArrrowEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // IsQuoteConvertionOnPasteEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsQuoteConvertionOnPasteEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsQuoteConvertionOnPasteEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsTrue(viewModel.IsQuoteConvertionOnPasteEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsQuoteConvertionOnPasteEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsQuoteConvertionOnPasteEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsQuoteConvertionOnPasteEnabled);
        }

        [Test]
        public void WhenDisablingIsQuoteConvertionOnPasteEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsQuoteConvertionOnPasteEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(this.settingsRepository)
            {
                IsQuoteConvertionOnPasteEnabled = false
            };
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsQuoteConvertionOnPasteEnabled.BoolValue);
        }

        [Test]
        public void WhenIsQuoteConvertionOnPasteEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(this.settingsRepository);

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsQuoteConvertionOnPasteEnabled = !viewModel.IsQuoteConvertionOnPasteEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }
    }
}
