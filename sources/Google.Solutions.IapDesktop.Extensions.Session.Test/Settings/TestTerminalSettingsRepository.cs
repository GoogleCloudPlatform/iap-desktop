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
using Google.Solutions.Testing.Apis.Platform;
using NUnit.Framework;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
{
    [TestFixture]

    public class TestTerminalSettingsRepository
    {
        [Test]
        public void GetSettings_WhenKeyEmpty_ThenDefaultsAreProvided()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new TerminalSettingsRepository(keyPath.CreateKey());

                var settings = repository.GetSettings();

                Assert.IsTrue(settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value);
                Assert.IsFalse(settings.IsSelectAllUsingCtrlAEnabled.Value);
                Assert.IsTrue(settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value);
                Assert.IsTrue(settings.IsSelectUsingShiftArrrowEnabled.Value);
                Assert.IsTrue(settings.IsQuoteConvertionOnPasteEnabled.Value);
                Assert.IsTrue(settings.IsNavigationUsingControlArrrowEnabled.Value);
                Assert.IsTrue(settings.IsScrollingUsingCtrlUpDownEnabled.Value);
                Assert.IsTrue(settings.IsScrollingUsingCtrlHomeEndEnabled.Value);
                Assert.AreEqual(TerminalSettings.DefaultFontFamily, settings.FontFamily.Value);
                Assert.AreEqual(
                    TerminalSettings.DefaultFontSize,
                    TerminalSettings.FontSizeFromDword(settings.FontSizeAsDword.Value));
                Assert.AreEqual(
                    TerminalSettings.DefaultBackgroundColor.ToArgb(),
                    settings.BackgroundColorArgb.Value);
                Assert.AreEqual(
                    Color.White.ToArgb(),
                    settings.ForegroundColorArgb.Value);
            }
        }

        [Test]
        public void SetSettings_WhenSettingsChanged_ThenEventIsFired()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new TerminalSettingsRepository(keyPath.CreateKey());

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
}
