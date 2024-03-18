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

using Google.Solutions.Common.Util;
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.Settings.Test
{
    [TestFixture]
    public class RegistrySettingsStoreOfInt : RegistrySettingsStoreBase
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
                    17,
                    Predicate.InRange(0, 100));

                Assert.IsFalse(setting.IsSpecified);
                Assert.IsTrue(setting.IsDefault);

                setting.Value = 1;

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
        public void WhenRegistryKeyIsNull_ThenFromKeyUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0, 100));

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(17, setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void WhenRegistryValueDoesNotExist_ThenFromKeyUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0, 100));

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(17, setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void WhenRegistryValueExists_ThenFromKeyUsesValue()
        {
            using (var key = CreateSettingsKey())
            {
                key.BackingKey.SetValue("test", 42);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0, 100));

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(42, setting.Value);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void WhenRegistryValueInvalid_ThenReadUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                key.BackingKey.SetValue("test", 101);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    1,
                    Predicate.InRange(0, 100));

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(1, setting.Value);
                Assert.IsTrue(setting.IsDefault);
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
                    17,
                    Predicate.InRange(0, 100));

                setting.Value = 1;
                key.Write(setting);

                Assert.AreEqual(1, key.BackingKey.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsDefaultValue_ThenSaveResetsRegistry()
        {
            using (var key = CreateSettingsKey())
            {
                key.BackingKey.SetValue("test", 42);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0, 100));

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
                    17,
                    Predicate.InRange(0, 100));

                setting.Value = setting.DefaultValue;

                Assert.AreEqual(17, setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueAndDefaultAreNull_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    0,
                    Predicate.InRange(0, 100));

                setting.Value = 0;

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
                    17,
                    Predicate.InRange(0, 100));

                setting.Value = 0;

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueIsInvalid_ThenSetValueRaisesArgumentOutOfRangeException()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0, 100));

                Assert.Throws<ArgumentOutOfRangeException>(() => setting.Value = -1);
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
                    17,
                    Predicate.InRange(0, 100));

                setting.Value = 1;
                ((IAnySetting)setting).AnyValue = null;

                Assert.AreEqual(17, setting.Value);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        [Test]
        public void WhenValueIsOfWrongType_ThenSetAnyValueRaisesInvalidCastException()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = (IAnySetting)key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0, 100));

                Assert.Throws<InvalidCastException>(() => setting.AnyValue = false);
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

                key.BackingKey.SetValue("test", 42);

                var setting = mergedKey.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0, 100));

                Assert.AreEqual(42, setting.Value);
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

                key.BackingKey.SetValue("test", 42);
                policyKey.BackingKey.SetValue("test", 101, RegistryValueKind.DWord);

                var setting = mergedKey.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0, 100));

                Assert.AreEqual(42, setting.Value);
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

                key.BackingKey.SetValue("test", 42);
                policyKey.BackingKey.SetValue("test", 88, RegistryValueKind.DWord);

                var setting = mergedKey.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0, 100));

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(88, setting.Value);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsTrue(setting.IsReadOnly);
            }
        }
    }
}
