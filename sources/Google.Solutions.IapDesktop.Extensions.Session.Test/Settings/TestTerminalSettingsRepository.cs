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
        public void GetSettings_WhenKeyEmpty()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new TerminalSettingsRepository(keyPath.CreateKey());

                var settings = repository.GetSettings();

                Assert.That(settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.Value, Is.True);
                Assert.That(settings.IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled.Value, Is.True);
                Assert.That(settings.IsQuoteConvertionOnPasteEnabled.Value, Is.True);
                Assert.That(settings.IsScrollingUsingCtrlHomeEndEnabled.Value, Is.True);
                Assert.That(settings.IsScrollingUsingCtrlPageUpDownEnabled.Value, Is.True);
                Assert.That(settings.FontFamily.Value, Is.EqualTo(TerminalSettings.DefaultFontFamily));
                Assert.That(
                    TerminalSettings.FontSizeFromDword(settings.FontSizeAsDword.Value), Is.EqualTo(TerminalSettings.DefaultFontSize));
                Assert.That(
                    settings.BackgroundColorArgb.Value, Is.EqualTo(TerminalSettings.DefaultBackgroundColor.ToArgb()));
                Assert.That(
                    settings.ForegroundColorArgb.Value, Is.EqualTo(Color.White.ToArgb()));
            }
        }

        [Test]
        public void SetSettings_WhenSettingsChanged()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new TerminalSettingsRepository(keyPath.CreateKey());

                var eventFired = false;
                repository.SettingsChanged += (sender, args) =>
                {
                    Assert.That(sender, Is.SameAs(repository));
                    Assert.That(args.Data.IsScrollingUsingCtrlHomeEndEnabled.Value, Is.False);
                    eventFired = true;
                };

                var settings = repository.GetSettings();
                Assert.That(settings.IsScrollingUsingCtrlHomeEndEnabled.Value, Is.True);
                settings.IsScrollingUsingCtrlHomeEndEnabled.Value = false;

                repository.SetSettings(settings);

                Assert.That(eventFired, Is.True);
            }
        }
    }
}
