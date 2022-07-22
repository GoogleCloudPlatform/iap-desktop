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

using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Options;
using Google.Solutions.Testing.Common;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Options
{
    [TestFixture]
    public class TestTerminalOptionsViewModel : ShellFixtureBase
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

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsTrue(viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsCopyPasteUsingCtrlCAndCtrlVEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled);
        }

        [Test]
        public void WhenDisablingIsCopyPasteUsingCtrlCAndCtrlVEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.IsCopyPasteUsingCtrlCAndCtrlVEnabled = false,
                v => v.IsCopyPasteUsingCtrlCAndCtrlVEnabled);
            
            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue);
        }

        [Test]
        public void WhenIsCopyPasteUsingCtrlCAndCtrlVEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

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

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsTrue(viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsCopyPasteUsingShiftInsertAndCtrlInsertEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled);
        }

        [Test]
        public void WhenDisablingIsCopyPasteUsingShiftInsertAndCtrlInsertEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled = false,
                v => v.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled);

            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.BoolValue);
        }

        [Test]
        public void WhenIsCopyPasteUsingShiftInsertAndCtrlInsertEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

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

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsTrue(viewModel.IsSelectAllUsingCtrlAEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsSelectAllUsingCtrlAEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsSelectAllUsingCtrlAEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(viewModel.IsSelectAllUsingCtrlAEnabled);
        }

        [Test]
        public void WhenDisablingIsSelectAllUsingCtrlAEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsSelectAllUsingCtrlAEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.IsSelectAllUsingCtrlAEnabled = false,
                v => v.IsSelectAllUsingCtrlAEnabled);

            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsSelectAllUsingCtrlAEnabled.BoolValue);
        }

        [Test]
        public void WhenIsSelectAllUsingCtrlAEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

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

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsTrue(viewModel.IsSelectUsingShiftArrrowEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsSelectUsingShiftArrrowEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsSelectUsingShiftArrrowEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(viewModel.IsSelectUsingShiftArrrowEnabled);
        }

        [Test]
        public void WhenDisablingIsSelectUsingShiftArrrowEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsSelectUsingShiftArrrowEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.IsSelectUsingShiftArrrowEnabled = false,
                v => v.IsSelectUsingShiftArrrowEnabled);

            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsSelectUsingShiftArrrowEnabled.BoolValue);
        }

        [Test]
        public void WhenIsSelectUsingShiftArrrowEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

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

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsTrue(viewModel.IsQuoteConvertionOnPasteEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsQuoteConvertionOnPasteEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsQuoteConvertionOnPasteEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(viewModel.IsQuoteConvertionOnPasteEnabled);
        }

        [Test]
        public void WhenDisablingIsQuoteConvertionOnPasteEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsQuoteConvertionOnPasteEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.IsQuoteConvertionOnPasteEnabled = false,
                v => v.IsQuoteConvertionOnPasteEnabled);

            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsQuoteConvertionOnPasteEnabled.BoolValue);
        }

        [Test]
        public void WhenIsQuoteConvertionOnPasteEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsQuoteConvertionOnPasteEnabled = !viewModel.IsQuoteConvertionOnPasteEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // IsScrollingUsingCtrlUpDownEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsScrollingUsingCtrlUpDownEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlUpDownEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsTrue(viewModel.IsScrollingUsingCtrlUpDownEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsScrollingUsingCtrlUpDownEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlUpDownEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(viewModel.IsScrollingUsingCtrlUpDownEnabled);
        }

        [Test]
        public void WhenDisablingIsScrollingUsingCtrlUpDownEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlUpDownEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.IsScrollingUsingCtrlUpDownEnabled = false,
                v => v.IsScrollingUsingCtrlUpDownEnabled);

            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsScrollingUsingCtrlUpDownEnabled.BoolValue);
        }

        [Test]
        public void WhenIsScrollingUsingCtrlUpDownEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsScrollingUsingCtrlUpDownEnabled = !viewModel.IsScrollingUsingCtrlUpDownEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // IsScrollingUsingCtrlHomeEndEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingEnabled_ThenIsScrollingUsingCtrlHomeEndEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlHomeEndEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsTrue(viewModel.IsScrollingUsingCtrlHomeEndEnabled);
        }

        [Test]
        public void WhenSettingDisabled_ThenIsScrollingUsingCtrlHomeEndEnabledIsTrue()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlHomeEndEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(viewModel.IsScrollingUsingCtrlHomeEndEnabled);
        }

        [Test]
        public void WhenDisablingIsScrollingUsingCtrlHomeEndEnabled_ThenChangeIsApplied()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IsScrollingUsingCtrlHomeEndEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.IsScrollingUsingCtrlHomeEndEnabled = false,
                v => v.IsScrollingUsingCtrlHomeEndEnabled);

            viewModel.ApplyChanges();

            settings = this.settingsRepository.GetSettings();
            Assert.IsFalse(settings.IsScrollingUsingCtrlHomeEndEnabled.BoolValue);
        }

        [Test]
        public void WhenIsScrollingUsingCtrlHomeEndEnabledChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsScrollingUsingCtrlHomeEndEnabled = !viewModel.IsScrollingUsingCtrlHomeEndEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // TerminalFont.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingPresent_ThenTerminalFontIsSet()
        {
            var font = new Font(FontFamily.GenericMonospace, 24.0f);
            var settings = this.settingsRepository.GetSettings();
            settings.FontFamily.StringValue = font.Name;
            settings.FontSizeAsDword.IntValue = TerminalSettings.DwordFromFontSize(font.Size);
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.AreEqual(font.Name, viewModel.TerminalFont.Name);
            Assert.AreEqual(font.Size, viewModel.TerminalFont.Size);
        }

        [Test]
        public void WhenFontChanged_ThenChangeIsApplied()
        {
            var font = new Font(FontFamily.GenericMonospace, 24.0f);
            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.TerminalFont = font,
                v => v.TerminalFont);

            Assert.IsTrue(viewModel.IsDirty);
            viewModel.ApplyChanges();
            Assert.IsFalse(viewModel.IsDirty);

            var settings = this.settingsRepository.GetSettings();
            Assert.AreEqual(font.Name, settings.FontFamily.StringValue);
            Assert.AreEqual(
                TerminalSettings.DwordFromFontSize(font.Size), 
                settings.FontSizeAsDword.IntValue);
        }

        //---------------------------------------------------------------------
        // ForegroundColor.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingPresent_ThenForegroundColorIsSet()
        {
            var color = Color.Red;

            var settings = this.settingsRepository.GetSettings();
            settings.ForegroundColorArgb.IntValue = color.ToArgb();
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.AreEqual(color.R, viewModel.TerminalForegroundColor.R);
            Assert.AreEqual(color.G, viewModel.TerminalForegroundColor.G);
            Assert.AreEqual(color.B, viewModel.TerminalForegroundColor.B);
        }

        [Test]
        public void WhenForegroundColorChanged_ThenChangeIsApplied()
        {
            var color = Color.Yellow;
            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.TerminalForegroundColor = color,
                v => v.TerminalForegroundColor);

            Assert.IsTrue(viewModel.IsDirty);
            viewModel.ApplyChanges();
            Assert.IsFalse(viewModel.IsDirty);

            var settings = this.settingsRepository.GetSettings();
            Assert.AreEqual(color.ToArgb(), settings.ForegroundColorArgb.IntValue);
        }

        //---------------------------------------------------------------------
        // BackgroundColor.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingPresent_ThenBackgroundColorIsSet()
        {
            var color = Color.Red;

            var settings = this.settingsRepository.GetSettings();
            settings.BackgroundColorArgb.IntValue = color.ToArgb();
            this.settingsRepository.SetSettings(settings);

            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            Assert.AreEqual(color.R, viewModel.TerminalBackgroundColor.R);
            Assert.AreEqual(color.G, viewModel.TerminalBackgroundColor.G);
            Assert.AreEqual(color.B, viewModel.TerminalBackgroundColor.B);
        }

        [Test]
        public void WhenBackgroundColorChanged_ThenChangeIsApplied()
        {
            var color = Color.Yellow;
            var viewModel = new TerminalOptionsViewModel(
                this.settingsRepository,
                new Mock<IExceptionDialog>().Object);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.TerminalBackgroundColor = color,
                v => v.TerminalBackgroundColor);

            Assert.IsTrue(viewModel.IsDirty);
            viewModel.ApplyChanges();
            Assert.IsFalse(viewModel.IsDirty);

            var settings = this.settingsRepository.GetSettings();
            Assert.AreEqual(color.ToArgb(), settings.BackgroundColorArgb.IntValue);
        }
    }
}
