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

using NUnit.Framework;
using System;

namespace Google.Solutions.Settings.Test
{
    [TestFixture]
    public class RegistrySettingsStoreOfString : RegistrySettingsStoreBase
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
                    "blue",
                    _ => true);

                Assert.IsFalse(setting.IsSpecified);
                Assert.IsTrue(setting.IsDefault);

                setting.Value = "red";

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
        public void WhenRegistryValueDoesNotExist_ThenReadUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    _ => true);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("blue", setting.Value);
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
                key.BackingKey.SetValue("test", "red");

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    _ => true);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("red", setting.Value);
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
                    "blue",
                    _ => true);

                setting.Value = "green";
                key.Write(setting);

                Assert.AreEqual("green", key.BackingKey.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsDefaultValue_ThenSaveResetsRegistry()
        {
            using (var key = CreateSettingsKey())
            {
                key.BackingKey.SetValue("test", "red");

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    _ => true);

                setting.Value = setting.DefaultValue;
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
                    "blue",
                    _ => true);

                setting.Value = setting.DefaultValue;

                Assert.AreEqual("blue", setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueAndDefaultAreNull_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<string>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null,
                    _ => true);

                setting.Value = null;

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
                    "blue",
                    _ => true);

                setting.Value = "yellow";

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
                    "blue",
                    _ => true);

                setting.Value = "red";
                setting.AnyValue = null;

                Assert.AreEqual("blue", setting.Value);
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
                    "blue",
                    _ => true);

                Assert.Throws<InvalidCastException>(() => setting.AnyValue = 1);
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

                key.BackingKey.SetValue("test", "red");

                var setting = mergedKey.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    _ => true);

                Assert.AreEqual("red", setting.Value);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void WhenPolicyInvalid_ThenPolicyIsIgnored()
        {
            using (var key = CreateSettingsKey())
            using (var policyKey = CreatePolicySettingsKey())
            {
                var mergedKey = new MergedSettingsStore(
                    new[] { key, policyKey },
                    MergedSettingsStore.MergeBehavior.Policy);

                key.BackingKey.SetValue("test", "red");
                policyKey.BackingKey.SetValue("test", "BLUE");

                var setting = mergedKey.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    v => v.ToLower() == v);

                Assert.AreEqual("red", setting.Value);
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

                key.BackingKey.SetValue("test", "red");
                policyKey.BackingKey.SetValue("test", "BLUE");

                var setting = mergedKey.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    _ => true);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("BLUE", setting.Value);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsTrue(setting.IsReadOnly);
            }
        }
    }
}
