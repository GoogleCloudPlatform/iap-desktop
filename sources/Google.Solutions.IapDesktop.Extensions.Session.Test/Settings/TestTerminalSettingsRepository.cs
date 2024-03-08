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

using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Microsoft.Win32;
using NUnit.Framework;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
{
    [TestFixture]

    public class TestTerminalSettingsRepository
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
        }

        [Test]
        public void WhenKeyEmpty_ThenDefaultsAreProvided()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new TerminalSettingsRepository(baseKey);

            var settings = repository.GetSettings();

            Assert.IsTrue(settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value);
            Assert.IsFalse(settings.IsSelectAllUsingCtrlAEnabled.Value);
            Assert.IsTrue(settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value);
            Assert.IsTrue(settings.IsSelectUsingShiftArrrowEnabled.Value);
            Assert.IsTrue(settings.IsQuoteConvertionOnPasteEnabled.Value);
            Assert.IsTrue(settings.IsNavigationUsingControlArrrowEnabled.Value);
            Assert.IsTrue(settings.IsScrollingUsingCtrlUpDownEnabled.Value);
            Assert.IsTrue(settings.IsScrollingUsingCtrlHomeEndEnabled.Value);
            Assert.AreEqual(TerminalFont.DefaultFontFamily, settings.FontFamily.Value);
            Assert.AreEqual(
                TerminalFont.DefaultSize,
                TerminalSettingsRepository.FontSizeFromDword(settings.FontSizeAsDword.IntValue));
            Assert.AreEqual(
                TerminalSettingsRepository.DefaultBackgroundColor.ToArgb(),
                settings.BackgroundColorArgb.IntValue);
            Assert.AreEqual(
                Color.White.ToArgb(),
                settings.ForegroundColorArgb.IntValue);
        }

        [Test]
        public void WhenSettingsChanged_ThenEventIsFired()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new TerminalSettingsRepository(baseKey);

            var eventFired = false;
            repository.SettingsChanged += (sender, args) =>
            {
                Assert.AreSame(repository, sender);
                Assert.IsTrue(args.Data.IsSelectAllUsingCtrlAEnabled.Value);
                eventFired = true;
            };

            var settings = repository.GetSettings();
            Assert.IsFalse(settings.IsSelectAllUsingCtrlAEnabled.Value);
            settings.IsSelectAllUsingCtrlAEnabled.Value = true;

            repository.SetSettings(settings);

            Assert.IsTrue(eventFired);
        }
    }
}
