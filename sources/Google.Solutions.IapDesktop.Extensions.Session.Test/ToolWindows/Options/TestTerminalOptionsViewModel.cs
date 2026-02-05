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

            Assert.That(viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value, Is.True);
        }

        [Test]
        public void IsCopyPasteUsingCtrlCAndCtrlVEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value, Is.False);
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
            Assert.That(settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value, Is.False);
        }

        [Test]
        public void IsCopyPasteUsingCtrlCAndCtrlVEnabled_WhenChanged()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value =
                !viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
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

            Assert.That(viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value, Is.True);
        }

        [Test]
        public void IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value, Is.False);
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
            Assert.That(settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value, Is.False);
        }

        [Test]
        public void IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled_WhenChanged()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value =
                !viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
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

            Assert.That(viewModel.IsQuoteConvertionOnPasteEnabled.Value, Is.True);
        }

        [Test]
        public void IsQuoteConvertionOnPasteEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsQuoteConvertionOnPasteEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsQuoteConvertionOnPasteEnabled.Value, Is.False);
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
            Assert.That(settings.IsQuoteConvertionOnPasteEnabled.Value, Is.False);
        }

        [Test]
        public void IsQuoteConvertionOnPasteEnabled_WhenChanged()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.IsQuoteConvertionOnPasteEnabled.Value =
                !viewModel.IsQuoteConvertionOnPasteEnabled.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }

        //---------------------------------------------------------------------
        // IsBracketedPasteEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsBracketedPasteEnabled_WhenSettingEnabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsBracketedPasteEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsBracketedPasteEnabled.Value, Is.True);
        }

        [Test]
        public void IsBracketedPasteEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsBracketedPasteEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsBracketedPasteEnabled.Value, Is.False);
        }

        [Test]
        public async Task IsBracketedPasteEnabled_AppliesChange()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsBracketedPasteEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.IsBracketedPasteEnabled.Value = false;
            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.That(settings.IsBracketedPasteEnabled.Value, Is.False);
        }

        [Test]
        public void IsBracketedPasteEnabled_WhenChanged()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.IsBracketedPasteEnabled.Value =
                !viewModel.IsBracketedPasteEnabled.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
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

            Assert.That(viewModel.IsScrollingUsingCtrlHomeEndEnabled.Value, Is.True);
        }

        [Test]
        public void IsScrollingUsingCtrlHomeEndEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlHomeEndEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsScrollingUsingCtrlHomeEndEnabled.Value, Is.False);
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
            Assert.That(settings.IsScrollingUsingCtrlHomeEndEnabled.Value, Is.False);
        }

        [Test]
        public void IsScrollingUsingCtrlHomeEndEnabled_WhenChanged()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.IsScrollingUsingCtrlHomeEndEnabled.Value =
                !viewModel.IsScrollingUsingCtrlHomeEndEnabled.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }

        //---------------------------------------------------------------------
        // IsScrollingUsingCtrlPageUpDownEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsScrollingUsingCtrlPageUpDownEnabled_WhenSettingEnabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlPageUpDownEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsScrollingUsingCtrlPageUpDownEnabled.Value, Is.True);
        }

        [Test]
        public void IsScrollingUsingCtrlPageUpDownEnabled_WhenSettingDisabled()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlPageUpDownEnabled.Value = false;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsScrollingUsingCtrlPageUpDownEnabled.Value, Is.False);
        }

        [Test]
        public async Task IsScrollingUsingCtrlPageUpDownEnabled_AppliesChange()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlPageUpDownEnabled.Value = true;
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.IsScrollingUsingCtrlPageUpDownEnabled.Value = false;
            await viewModel.ApplyChangesAsync();

            settings = settingsRepository.GetSettings();
            Assert.That(settings.IsScrollingUsingCtrlPageUpDownEnabled.Value, Is.False);
        }

        [Test]
        public void IsScrollingUsingCtrlPageUpDownEnabled_WhenChanged()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.IsScrollingUsingCtrlPageUpDownEnabled.Value =
                !viewModel.IsScrollingUsingCtrlPageUpDownEnabled.Value;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }

        //---------------------------------------------------------------------
        // TerminalFont.
        //---------------------------------------------------------------------

        [Test]
        public void TerminalFont_WhenSettingPresent()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var font = new Font(FontFamily.GenericMonospace, 24.0f);
            var settings = settingsRepository.GetSettings();
            settings.FontFamily.Value = font.Name;
            settings.FontSizeAsDword.Value =
                TerminalSettings.DwordFromFontSize(font.Size);
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.TerminalFont.Value.Name, Is.EqualTo(font.Name));
            Assert.That(viewModel.TerminalFont.Value.Size, Is.EqualTo(font.Size));
        }

        [Test]
        public async Task TerminalFont_WhenFontChanged()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var font = new Font(FontFamily.GenericMonospace, 24.0f);
            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.TerminalFont.Value = font;

            Assert.That(viewModel.IsDirty.Value, Is.True);
            await viewModel.ApplyChangesAsync();
            Assert.That(viewModel.IsDirty.Value, Is.False);

            var settings = settingsRepository.GetSettings();
            Assert.That(settings.FontFamily.Value, Is.EqualTo(font.Name));
            Assert.That(
                settings.FontSizeAsDword.Value, Is.EqualTo(TerminalSettings.DwordFromFontSize(font.Size)));
        }

        //---------------------------------------------------------------------
        // ForegroundColor.
        //---------------------------------------------------------------------

        [Test]
        public void ForegroundColor_WhenSettingPresent()
        {
            var color = Color.Red;

            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.ForegroundColorArgb.Value = color.ToArgb();
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.TerminalForegroundColor.Value.R, Is.EqualTo(color.R));
            Assert.That(viewModel.TerminalForegroundColor.Value.G, Is.EqualTo(color.G));
            Assert.That(viewModel.TerminalForegroundColor.Value.B, Is.EqualTo(color.B));
        }

        [Test]
        public async Task ForegroundColor_WhenForegroundColorChanged()
        {
            var settingsRepository = CreateTerminalSettingsRepository();

            var color = Color.Yellow;
            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.TerminalForegroundColor.Value = color;

            Assert.That(viewModel.IsDirty.Value, Is.True);
            await viewModel.ApplyChangesAsync();
            Assert.That(viewModel.IsDirty.Value, Is.False);

            var settings = settingsRepository.GetSettings();
            Assert.That(settings.ForegroundColorArgb.Value, Is.EqualTo(color.ToArgb()));
        }

        //---------------------------------------------------------------------
        // BackgroundColor.
        //---------------------------------------------------------------------

        [Test]
        public void BackgroundColor_WhenSettingPresent()
        {
            var color = Color.Red;

            var settingsRepository = CreateTerminalSettingsRepository();
            var settings = settingsRepository.GetSettings();
            settings.BackgroundColorArgb.Value = color.ToArgb();
            settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.TerminalBackgroundColor.Value.R, Is.EqualTo(color.R));
            Assert.That(viewModel.TerminalBackgroundColor.Value.G, Is.EqualTo(color.G));
            Assert.That(viewModel.TerminalBackgroundColor.Value.B, Is.EqualTo(color.B));
        }

        [Test]
        public async Task BackgroundColor_WhenBackgroundColorChanged()
        {
            var settingsRepository = CreateTerminalSettingsRepository();

            var color = Color.Yellow;
            var viewModel = new TerminalOptionsViewModel(settingsRepository);
            viewModel.TerminalBackgroundColor.Value = color;

            Assert.That(viewModel.IsDirty.Value, Is.True);
            await viewModel.ApplyChangesAsync();
            Assert.That(viewModel.IsDirty.Value, Is.False);

            var settings = settingsRepository.GetSettings();
            Assert.That(settings.BackgroundColorArgb.Value, Is.EqualTo(color.ToArgb()));
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

            Assert.That(
                viewModel.CaretStyle.Value, Is.EqualTo(VirtualTerminal.CaretStyle.SteadyBlock));
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
            Assert.That(
                viewModel.CaretStyle.Value, Is.EqualTo(VirtualTerminal.CaretStyle.BlinkingBlock));
        }

        [Test]
        public void CaretStyle_WhenChanged()
        {
            var settingsRepository = CreateTerminalSettingsRepository();
            var viewModel = new TerminalOptionsViewModel(settingsRepository);

            Assert.That(viewModel.IsDirty.Value, Is.False);

            viewModel.CaretStyle.Value = VirtualTerminal.CaretStyle.BlinkingBlock;

            Assert.That(viewModel.IsDirty.Value, Is.True);
        }
    }
}