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

using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.Settings.Test
{
    [TestFixture]
    public class RegistrySettingsStoreOfEnum : RegistrySettingsStoreBase
    {
        [Flags]
        public enum Toppings
        {
            None = 0,
            Cheese = 1,
            Chocolate = 2,
            Cream = 4
        }

        //---------------------------------------------------------------------
        // IsSpecified.
        //---------------------------------------------------------------------

        [Test]
        public void IsSpecified_WhenValueChanged()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None);

                Assert.That(setting.IsSpecified, Is.False);
                Assert.That(setting.IsDefault, Is.True);

                setting.Value = Toppings.Cheese;

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
        public void Read_WhenRegistryKeyIsNull_ThenUsesDefaults()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.That(setting.Value, Is.EqualTo(ConsoleColor.Blue));
                Assert.That(setting.IsDefault, Is.True);
                Assert.That(setting.IsDirty, Is.False);
                Assert.That(setting.IsReadOnly, Is.False);
            }
        }

        [Test]
        public void Read_WhenRegistryValueDoesNotExist_ThenUsesDefaults()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.That(setting.Value, Is.EqualTo(ConsoleColor.Blue));
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
                key.BackingKey.SetValue("test", (int)ConsoleColor.Red, RegistryValueKind.DWord);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.That(setting.Value, Is.EqualTo(ConsoleColor.Red));
                Assert.That(setting.IsDefault, Is.False);
                Assert.That(setting.IsDirty, Is.False);
                Assert.That(setting.IsReadOnly, Is.False);
            }
        }

        [Test]
        public void WhenRegistryValueInvalid_ThenReadUsesDefaults()
        {
            using (var key = CreateSettingsStore())
            {
                key.BackingKey.SetValue("test", -1);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.That(setting.Value, Is.EqualTo(ConsoleColor.Blue));
                Assert.That(setting.IsDefault, Is.True);
                Assert.That(setting.IsDirty, Is.False);
                Assert.That(setting.IsReadOnly, Is.False);
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
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

                setting.Value = ConsoleColor.Green;
                key.Write(setting);

                Assert.That(key.BackingKey.GetValue("test"), Is.EqualTo((int)ConsoleColor.Green));
            }
        }

        [Test]
        public void Save_WhenSettingIsDefaultValue_ThenResetsRegistry()
        {
            using (var key = CreateSettingsStore())
            {
                key.BackingKey.SetValue("test", (int)ConsoleColor.Red, RegistryValueKind.DWord);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

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
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

                setting.Value = setting.DefaultValue;

                Assert.That(setting.Value, Is.EqualTo(ConsoleColor.Blue));
                Assert.That(setting.IsDefault, Is.True);
                Assert.That(setting.IsDirty, Is.False);
            }
        }

        [Test]
        public void SetValue_WhenValueDiffersFromDefault_ThenSucceedsAndSettingIsDirty()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

                setting.Value = ConsoleColor.Yellow;

                Assert.That(setting.IsDefault, Is.False);
                Assert.That(setting.IsDirty, Is.True);
            }
        }

        [Test]
        public void WhenValueIsInvalid_ThenSetValueRaisesArgumentOutOfRangeException()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

                Assert.Throws<ArgumentOutOfRangeException>(() => setting.Value = (ConsoleColor)100);
            }
        }

        //---------------------------------------------------------------------
        // AnyValue.
        //---------------------------------------------------------------------

        [Test]
        public void SetAnyValue_WhenValueIsNull_ThenResetsToDefault()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

                setting.Value = ConsoleColor.Blue;
                ((IAnySetting)setting).AnyValue = null;

                Assert.That(setting.Value, Is.EqualTo(ConsoleColor.Blue));
                Assert.That(setting.IsDefault, Is.True);
            }
        }

        [Test]
        public void SetAnyValue_WhenValueIsOfWrongType_ThenThrowsException()
        {
            using (var key = CreateSettingsStore())
            {
                var setting = (IAnySetting)key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue);

                Assert.Throws<InvalidCastException>(() => setting.AnyValue = false);
            }
        }
    }
}
