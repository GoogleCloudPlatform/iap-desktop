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
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.That(setting.IsSpecified, Is.False);
                Assert.That(setting.IsDefault, Is.True);

                setting.SetClearTextValue("value");

                Assert.That(setting.IsSpecified, Is.True);
                Assert.That(setting.IsDefault, Is.False);

                setting.Value = setting.DefaultValue;

                Assert.That(setting.IsSpecified, Is.True);
                Assert.That(setting.IsDefault, Is.True);
            }
        }

        //---------------------------------------------------------------------
        // Read.
        //---------------------------------------------------------------------

        [Test]
        public void Read_WhenRegistryValueDoesNotExist_ThenUsesDefaults()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.That(setting.Value, Is.Null);
                Assert.That(setting.IsDefault, Is.True);
                Assert.That(setting.IsDirty, Is.False);
                Assert.That(setting.IsReadOnly, Is.False);
            }
        }

        [Test]
        public void Read_WhenRegistryKeyIsNull_ThenUsesDefaults()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.That(setting.Value, Is.Null);
                Assert.That(setting.IsDefault, Is.True);
                Assert.That(setting.IsDirty, Is.False);
                Assert.That(setting.IsReadOnly, Is.False);
            }
        }

        [Test]
        public void WhenRegistryValueContainsGibberish_ThenReadUsesDefaults()
        {
            using (var key = CreateSettingsStore())
            {
                key.BackingKey.SetValue("test", Encoding.ASCII.GetBytes("gibberish"), RegistryValueKind.Binary);

                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.That(setting.Value, Is.Null);
                Assert.That(setting.GetClearTextValue(), Is.Null);
                Assert.That(setting.IsDefault, Is.True);
                Assert.That(setting.IsDirty, Is.False);
                Assert.That(setting.IsReadOnly, Is.False);
            }
        }

        [Test]
        public void Read_WhenRegistryValueExists_ThenUsesValue()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("red");
                key.Write(setting);

                Assert.That(key.BackingKey.GetValue("test"), Is.Not.Null);

                // Now read again.

                setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.That(setting.GetClearTextValue(), Is.EqualTo("red"));
                Assert.That(setting.IsDefault, Is.False);
                Assert.That(setting.IsDirty, Is.False);
            }
        }

        //---------------------------------------------------------------------
        // Save.
        //---------------------------------------------------------------------

        [Test]
        public void Save_WhenSettingIsNonNull()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("green");
                key.Write(setting);

                Assert.That(key.BackingKey.GetValue("test"), Is.Not.Null);
            }
        }

        [Test]
        public void Save_WhenSettingIsDefaultValue_ThenResetsRegistry()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("red");
                key.Write(setting);

                Assert.That(key.BackingKey.GetValue("test"), Is.Not.Null);

                // Now write again.

                setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = setting.DefaultValue;
                key.Write(setting);

                Assert.That(key.BackingKey.GetValue("test"), Is.Null);
            }
        }

        //---------------------------------------------------------------------
        // Value.
        //---------------------------------------------------------------------

        [Test]
        public void SetValue_WhenValuelsDefault_ThenSucceedsAndSettingIsNotDirty()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = null;

                Assert.That(setting.IsDefault, Is.True);
                Assert.That(setting.IsDirty, Is.False);
            }
        }

        [Test]
        public void SetValue_WhenValueAndDefaultAreNull_ThenSucceedsAndSettingIsNotDirty()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = null;

                Assert.That(setting.IsDefault, Is.True);
                Assert.That(setting.IsDirty, Is.False);
            }
        }

        [Test]
        public void SetValue_WhenValueDiffersFromDefault_ThenSucceedsAndSettingIsDirty()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("yellow");

                Assert.That(setting.IsDefault, Is.False);
                Assert.That(setting.IsDirty, Is.True);
            }
        }

        //---------------------------------------------------------------------
        // AnyValue.
        //---------------------------------------------------------------------

        [Test]
        public void SetAnyValue_WhenValueIsOfWrongType_ThenThrowsException()
        {
            using (var key = CreateSettingsStore())
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
