//
// Copyright 2024 Google LLC
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

using Google.Solutions.Settings.ComponentModel;
using NUnit.Framework;
using System.Security;

namespace Google.Solutions.Settings.Test.ComponentModel
{
    [TestFixture]
    public class TestMaskedSettingDescriptor
    {
        [Test]
        public void GetValueReturnsMaskedString()
        {
            var setting = DictionarySettingsStore
                .Empty()
                .Read<SecureString>("key", "display name", "description", "category", null);
            setting.SetClearTextValue("secret");

            var descriptor = new MaskedSettingDescriptor(setting);

            Assert.AreEqual("********", descriptor.GetValue(setting));
        }

        [Test]
        public void SetValue()
        {
            var setting = DictionarySettingsStore
                .Empty()
                .Read<SecureString>("key", "display name", "description", "category", null);

            var descriptor = new MaskedSettingDescriptor(setting);
            descriptor.SetValue(setting, "secret");

            Assert.AreEqual("secret", setting.GetClearTextValue());
        }

        [Test]
        public void ResetValue()
        {
            var setting = DictionarySettingsStore
                .Empty()
                .Read<SecureString>("key", "display name", "description", "category", null);
            setting.SetClearTextValue("secret");

            Assert.IsFalse(setting.IsDefault);

            var descriptor = new MaskedSettingDescriptor(setting);
            descriptor.SetValue(setting, null);

            Assert.IsTrue(setting.IsDefault);
        }
    }
}
