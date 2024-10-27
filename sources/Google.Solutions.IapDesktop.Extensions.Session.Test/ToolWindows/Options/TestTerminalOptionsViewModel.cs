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

using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Options;
using Google.Solutions.Terminal.Controls;
using Google.Solutions.Testing.Apis.Platform;
using Microsoft.Win32;
using NUnit.Framework;
using System.Drawing;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Options
{
    [TestFixture]
    public class TestTerminalOptionsViewModel
    {
        private static TerminalSettingsRepository CreateTerminalSettingsRepository()
        {
            return new TerminalSettingsRepository(
                RegistryKeyPath.ForCurrentTest().CreateKey());
        }

        //---------------------------------------------------------------------
        // IsCopyPasteUsingCtrlCAndCtrlVEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsCopyPasteUsingCtrlCAndCtrlVEnabled_WhenSettingEnabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsTrue(viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value);
        }

        [Test]
        public void IsCopyPasteUsingCtrlCAndCtrlVEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value);
        }

        [Test]
        public async Task IsCopyPasteUsingCtrlCAndCtrlVEnabled_AppliesChange()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value = false;
            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value);
        }

        [Test]
        public void IsCopyPasteUsingCtrlCAndCtrlVEnabled_WhenChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value =
                !viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled_WhenSettingEnabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsTrue(viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value);
        }

        [Test]
        public void IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value);
        }

        [Test]
        public async Task IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled_AppliesChange()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value = false;
            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value);
        }

        [Test]
        public void IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled_WhenChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value =
                !viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // IsQuoteConvertionOnPasteEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsQuoteConvertionOnPasteEnabled_WhenSettingEnabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsQuoteConvertionOnPasteEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsTrue(viewModel.IsQuoteConvertionOnPasteEnabled.Value);
        }

        [Test]
        public void IsQuoteConvertionOnPasteEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsQuoteConvertionOnPasteEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsQuoteConvertionOnPasteEnabled.Value);
        }

        [Test]
        public async Task IsQuoteConvertionOnPasteEnabled_AppliesChange()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsQuoteConvertionOnPasteEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.IsQuoteConvertionOnPasteEnabled.Value = false;
            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsQuoteConvertionOnPasteEnabled.Value);
        }

        [Test]
        public void IsQuoteConvertionOnPasteEnabled_WhenChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsQuoteConvertionOnPasteEnabled.Value =
                !viewModel.IsQuoteConvertionOnPasteEnabled.Value;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // IsScrollingUsingCtrlHomeEndEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsScrollingUsingCtrlHomeEndEnabled_WhenSettingEnabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlHomeEndEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsTrue(viewModel.IsScrollingUsingCtrlHomeEndEnabled.Value);
        }

        [Test]
        public void IsScrollingUsingCtrlHomeEndEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlHomeEndEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsScrollingUsingCtrlHomeEndEnabled.Value);
        }

        [Test]
        public async Task IsScrollingUsingCtrlHomeEndEnabled_AppliesChange()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlHomeEndEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.IsScrollingUsingCtrlHomeEndEnabled.Value = false;
            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsScrollingUsingCtrlHomeEndEnabled.Value);
        }

        [Test]
        public void IsScrollingUsingCtrlHomeEndEnabled_WhenChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.IsScrollingUsingCtrlHomeEndEnabled.Value =
                !viewModel.IsScrollingUsingCtrlHomeEndEnabled.Value;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // TerminalFont.
        //---------------------------------------------------------------------

        [Test]
        public void TerminalFont_WhenSettingPresent_ThenTerminalFontIsSet()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var font = new Font(FontFamily.GenericMonospace, 24.0f);
            var settings = settingsRepository.GetSettings();
            settings.FontFamily.Value = font.Name;
            settings.FontSizeAsDword.Value =
                TerminalSettings.DwordFromFontSize(font.Size);
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.AreEqual(font.Name, viewModel.TerminalFont.Value.Name);
            Assert.AreEqual(font.Size, viewModel.TerminalFont.Value.Size);
        }

        [Test]
        public async Task TerminalFont_WhenFontChanged_ThenChangeIsApplied()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var font = new Font(FontFamily.GenericMonospace, 24.0f);
            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.TerminalFont.Value = font;

            Assert.IsTrue(viewModel.IsDirty.Value);
            await viewModel.ApplyChangesAsync();
            Assert.IsFalse(viewModel.IsDirty.Value);

            var settings = settingsRepository.GetSettings();
            Assert.AreEqual(font.Name, settings.FontFamily.Value);
            Assert.AreEqual(
                TerminalSettings.DwordFromFontSize(font.Size),
                settings.FontSizeAsDword.Value);
        }

        //---------------------------------------------------------------------
        // ForegroundColor.
        //---------------------------------------------------------------------

        [Test]
        public void ForegroundColor_WhenSettingPresent_ThenForegroundColorIsSet()
        {
            var color = Color.Red;

            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.ForegroundColorArgb.Value = color.ToArgb();
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.AreEqual(color.R, viewModel.TerminalForegroundColor.Value.R);
            Assert.AreEqual(color.G, viewModel.TerminalForegroundColor.Value.G);
            Assert.AreEqual(color.B, viewModel.TerminalForegroundColor.Value.B);
        }

        [Test]
        public async Task ForegroundColor_WhenForegroundColorChanged_ThenChangeIsApplied()
        {
            var settingsRepository = CreateTerminalSettingsRepository();

            var color = Color.Yellow;
            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.TerminalForegroundColor.Value = color;

            Assert.IsTrue(viewModel.IsDirty.Value);
            await viewModel.ApplyChangesAsync();
            Assert.IsFalse(viewModel.IsDirty.Value);

            var settings = settingsRepository.GetSettings();
            Assert.AreEqual(color.ToArgb(), settings.ForegroundColorArgb.Value);
        }

        //---------------------------------------------------------------------
        // BackgroundColor.
        //---------------------------------------------------------------------

        [Test]
        public void BackgroundColor_WhenSettingPresent_ThenBackgroundColorIsSet()
        {
            var color = Color.Red;

            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.BackgroundColorArgb.Value = color.ToArgb();
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.AreEqual(color.R, viewModel.TerminalBackgroundColor.Value.R);
            Assert.AreEqual(color.G, viewModel.TerminalBackgroundColor.Value.G);
            Assert.AreEqual(color.B, viewModel.TerminalBackgroundColor.Value.B);
        }

        [Test]
        public async Task BackgroundColor_WhenBackgroundColorChanged_ThenChangeIsApplied()
        {
            var settingsRepository = CreateTerminalSettingsRepository();

            var color = Color.Yellow;
            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.TerminalBackgroundColor.Value = color;

            Assert.IsTrue(viewModel.IsDirty.Value);
            await viewModel.ApplyChangesAsync();
            Assert.IsFalse(viewModel.IsDirty.Value);

            var settings = settingsRepository.GetSettings();
            Assert.AreEqual(color.ToArgb(), settings.BackgroundColorArgb.Value);
        }

        //---------------------------------------------------------------------
        // CaretStyle.
        //---------------------------------------------------------------------

        [Test]
        public void CaretStyle_WhenSettingPresent()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.CaretStyle.Value = VirtualTerminal.CaretStyle.SteadyBlock;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.AreEqual(
                VirtualTerminal.CaretStyle.SteadyBlock,
                viewModel.CaretStyle.Value);
        }

        [Test]
        public async Task CaretStyle_AppliesChange()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.CaretStyle.Value = VirtualTerminal.CaretStyle.SteadyBlock;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.CaretStyle.Value = VirtualTerminal.CaretStyle.BlinkingBlock;
            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.AreEqual(
                VirtualTerminal.CaretStyle.BlinkingBlock,
                viewModel.CaretStyle.Value);
        }

        [Test]
        public void CaretStyle_WhenChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.IsFalse(viewModel.IsDirty.Value);

            viewModel.CaretStyle.Value = VirtualTerminal.CaretStyle.BlinkingBlock;

            Assert.IsTrue(viewModel.IsDirty.Value);
        }
    }
}