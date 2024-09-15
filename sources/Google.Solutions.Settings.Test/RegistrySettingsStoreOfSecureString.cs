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

using Google.Solutions.Common.Security;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Security;
using System.Text;

namespace Google.Solutions.Settings.Test
{
    [TestFixture]
    public class RegistrySettingsStoreOfSecureString : RegistrySettingsStoreBase
    {
        //---------------------------------------------------------------------
        // IsSpecified.
        //---------------------------------------------------------------------

        [Test]
        public void IsSpecified_WhenValueChanged()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.IsFalse(setting.IsSpecified);
                Assert.IsTrue(setting.IsDefault);

                setting.SetClearTextValue("value");

                Assert.IsTrue(setting.IsSpecified);
                Assert.IsFalse(setting.IsDefault);

                setting.Value = setting.DefaultValue;

                Assert.IsTrue(setting.IsSpecified);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // Read.
        //---------------------------------------------------------------------

        [Test]
        public void Read_WhenRegistryValueDoesNotExist_ThenUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsNull(setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void Read_WhenRegistryKeyIsNull_ThenUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsNull(setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void WhenRegistryValueContainsGibberish_ThenReadUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                key.BackingKey.SetValue("test", Encoding.ASCII.GetBytes("gibberish"), RegistryValueKind.Binary);

                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsNull(setting.Value);
                Assert.IsNull(setting.GetClearTextValue());
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void Read_WhenRegistryValueExists_ThenUsesValue()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("red");
                key.Write(setting);

                Assert.IsNotNull(key.BackingKey.GetValue("test"));

                // Now read again.

                setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("red", setting.GetClearTextValue());
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        //---------------------------------------------------------------------
        // Save.
        //---------------------------------------------------------------------

        [Test]
        public void Save_WhenSettingIsNonNull()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("green");
                key.Write(setting);

                Assert.IsNotNull(key.BackingKey.GetValue("test"));
            }
        }

        [Test]
        public void Save_WhenSettingIsDefaultValue_ThenResetsRegistry()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("red");
                key.Write(setting);

                Assert.IsNotNull(key.BackingKey.GetValue("test"));

                // Now write again.

                setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = setting.DefaultValue;
                key.Write(setting);

                Assert.IsNull(key.BackingKey.GetValue("test"));
            }
        }

        //---------------------------------------------------------------------
        // Value.
        //---------------------------------------------------------------------

        [Test]
        public void SetValue_WhenValuelsDefault_ThenSucceedsAndSettingIsNotDirty()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = null;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void SetValue_WhenValueAndDefaultAreNull_ThenSucceedsAndSettingIsNotDirty()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = null;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void SetValue_WhenValueDiffersFromDefault_ThenSucceedsAndSettingIsDirty()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("yellow");

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        //---------------------------------------------------------------------
        // AnyValue.
        //---------------------------------------------------------------------

        [Test]
        public void SetAnyValue_WhenValueIsOfWrongType_ThenThrowsException()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = (IAnySetting)key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.Throws<InvalidCastException>(() => setting.AnyValue = 1);
            }
        }
    }
}
