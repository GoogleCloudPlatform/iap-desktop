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
        public void WhenValueChanged_ThenIsSpecifiedIsTrue()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);
                Assert.IsFalse(setting.IsSpecified);
                Assert.IsTrue(setting.IsDefault);

                setting.Value = true;

                Assert.IsTrue(setting.IsSpecified);
                Assert.IsFalse(setting.IsDefault);

                setting.Value = setting.DefaultValue;

                Assert.IsTrue(setting.IsSpecified);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // Load.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRegistryKeyIsNull_ThenReadUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    true);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsTrue(setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void WhenRegistryValueDoesNotExist_ThenReadUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    true);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsTrue(setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void WhenRegistryValueExists_ThenReadUsesValue()
        {
            using (var key = CreateSettingsKey())
            {
                key.BackingKey.SetValue("test", 1, RegistryValueKind.DWord);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsTrue(setting.Value);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        //---------------------------------------------------------------------
        // Save.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingIsNonNull_ThenSaveUpdatesRegistry()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                setting.Value = true;
                key.Write(setting);

                Assert.AreEqual(1, key.BackingKey.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsDefaultValue_ThenSaveResetsRegistry()
        {
            using (var key = CreateSettingsKey())
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
        public void WhenSettingIsNullAndValueDeleted_ThenSaveDoesNothing()
        {
            using (var key = CreateSettingsKey())
            {
                key.BackingKey.SetValue("test", 1, RegistryValueKind.DWord);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                key.BackingKey.DeleteValue("test");

                setting.AnyValue = null;
                key.Write(setting);

                Assert.IsNull(key.BackingKey.GetValue("test"));
            }
        }

        //---------------------------------------------------------------------
        // Value.
        //---------------------------------------------------------------------


        [Test]
        public void WhenValueEqualsDefault_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                setting.Value = setting.DefaultValue;

                Assert.AreEqual(false, setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueDiffersFromDefault_ThenSetValueSucceedsAndSettingIsDirty()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                setting.Value = true;

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        //---------------------------------------------------------------------
        // AnyValue.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsNull_ThenSetAnyValueResetsToDefault()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    false);

                setting.Value = true;
                setting.AnyValue = null;

                Assert.AreEqual(false, setting.Value);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        [Test]
        public void WhenValueIsOfWrongType_ThenSetAnyValueRaisesInvalidCastException()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
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
        public void WhenPolicyIsEmpty_ThenPolicyIsIgnored()
        {
            using (var key = CreateSettingsKey())
            using (var policyKey = CreatePolicySettingsKey())
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

                Assert.AreEqual(true, setting.Value);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void WhenPolicySet_ThenSettingHasPolicyApplied()
        {
            using (var key = CreateSettingsKey())
            using (var policyKey = CreatePolicySettingsKey())
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

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsFalse(setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsTrue(setting.IsReadOnly);
            }
        }
    }
}
