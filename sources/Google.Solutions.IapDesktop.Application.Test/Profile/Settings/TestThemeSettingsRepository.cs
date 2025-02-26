﻿//
// Copyright 2023 Google LLC
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


using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Testing.Apis.Platform;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings
{
    [TestFixture]
    public class TestThemeSettingsRepository
    {
        [Test]
        public void GetSettings_WhenKeyEmpty_ThenDefaultsAreProvided()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new ThemeSettingsRepository(settingsPath.CreateKey());

                var settings = repository.GetSettings();

                Assert.AreEqual(ApplicationTheme._Default, settings.Theme.Value);
                Assert.AreEqual(ScalingMode.SystemDpiAware, settings.ScalingMode.Value);
            }
        }
    }
}
