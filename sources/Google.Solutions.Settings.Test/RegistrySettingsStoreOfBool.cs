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
    public class RegistrySettingsStoreOfBool : RegistrySettingsStoreBase
    {
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
                    false);
                Assert.That(setting.IsSpecified, Is.False);
                Assert.IsTrue(setting.IsDefault);

                setting.Value = true;

                Assert.IsTrue(setting.IsSpecified);
                Assert.That(setting.IsDefault, Is.False);

                setting.Value = setting.DefaultValue;

                Assert.IsTrue(setting.IsSpecified);
                Assert.IsTrue(setting.IsDefault);
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
                    true);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.IsTrue(setting.Value);
                Assert.IsTrue(setting.IsDefault);
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
                    true);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.IsTrue(setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.That(setting.IsDirty, Is.False);
                Assert.That(setting.IsReadOnly, Is.False);
            }
        }

        [Test]
        public void Read_WhenRegistryValueExists_ThenUsesValue()
        {
            using (var key = CreateSettingsStore())
            {
                key.BackingKey.SetValue("test", 1, RegistryValueKind.DWord);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.IsTrue(setting.Value);
                Assert.That(setting.IsDefault, Is.False);
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
                    false);

                setting.Value = true;
                key.Write(setting);

                Assert.That(key.BackingKey.GetValue("test"), Is.EqualTo(1));
            }
        }

        [Test]
        public void Save_WhenSettingIsDefaultValue_ThenResetsRegistry()
        {
            using (var key = CreateSettingsStore())
            {
                key.BackingKey.SetValue("test", 1, RegistryValueKind.DWord);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                setting.Value = setting.DefaultValue;
                key.Write(setting);

                Assert.IsNull(key.BackingKey.GetValue("test"));
            }
        }

        [Test]
        public void Save_WhenSettingIsNullAndValueDeleted_ThenDoesNothing()
        {
            using (var key = CreateSettingsStore())
            {
                key.BackingKey.SetValue("test", 1, RegistryValueKind.DWord);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                key.BackingKey.DeleteValue("test");

                ((IAnySetting)setting).AnyValue = null;
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
                    false);

                setting.Value = setting.DefaultValue;

                Assert.That(setting.Value, Is.EqualTo(false));
                Assert.IsTrue(setting.IsDefault);
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
                    false);

                setting.Value = true;

                Assert.That(setting.IsDefault, Is.False);
                Assert.IsTrue(setting.IsDirty);
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
                    false);

                setting.Value = true;
                ((IAnySetting)setting).AnyValue = null;

                Assert.That(setting.Value, Is.EqualTo(false));
                Assert.IsTrue(setting.IsDefault);
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
                    false);

                Assert.Throws<InvalidCastException>(() => setting.AnyValue = -1);
            }
        }

        //---------------------------------------------------------------------
        // Policy.
        //---------------------------------------------------------------------

        [Test]
        public void Policy_WhenPolicyIsEmpty_ThenPolicyIsIgnored()
        {
            using (var key = CreateSettingsStore())
            using (var policyKey = CreatePolicyStore())
            {
                var mergedKey = new MergedSettingsStore(
                    new[] { key, policyKey },
                    MergedSettingsStore.MergeBehavior.Policy);

                key.BackingKey.SetValue("test", 1, RegistryValueKind.DWord);

                var setting = mergedKey.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                Assert.That(setting.Value, Is.EqualTo(true));
                Assert.That(setting.IsReadOnly, Is.False);
            }
        }

        [Test]
        public void Policy_WhenPolicyNotEmpty_ThenSettingIsMerged()
        {
            using (var key = CreateSettingsStore())
            using (var policyKey = CreatePolicyStore())
            {
                var mergedKey = new MergedSettingsStore(
                    new[] { key, policyKey },
                    MergedSettingsStore.MergeBehavior.Policy);

                key.BackingKey.SetValue("test", 1, RegistryValueKind.DWord);
                policyKey.BackingKey.SetValue("test", 0, RegistryValueKind.DWord);

                var setting = mergedKey.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                Assert.That(setting.Key, Is.EqualTo("test"));
                Assert.That(setting.DisplayName, Is.EqualTo("title"));
                Assert.That(setting.Description, Is.EqualTo("description"));
                Assert.That(setting.Category, Is.EqualTo("category"));
                Assert.That(setting.Value, Is.False);
                Assert.IsTrue(setting.IsDefault);
                Assert.That(setting.IsDirty, Is.False);
                Assert.IsTrue(setting.IsReadOnly);
            }
        }
    }
}
