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
    public class RegistrySettingsStoreOfLong : RegistrySettingsStoreBase
    {

        //---------------------------------------------------------------------
        // IsSpecified.
        //---------------------------------------------------------------------

        [Test]
        public void IsSpecified_WhenValueChanged()
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
        // Read.
        //---------------------------------------------------------------------

        [Test]
        public void Read_WhenRegistryValueDoesNotExist_ThenUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    Predicate.InRange(0L, 100L));

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(17L, setting.Value);
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
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    Predicate.InRange(0L, 100L));

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(17L, setting.Value);
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
                key.BackingKey.SetValue("test", 420000000000001L, RegistryValueKind.QWord);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    Predicate.InRange(0L, long.MaxValue));

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(420000000000001, setting.Value);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
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
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    Predicate.InRange(0L, 100L));

                setting.Value = 1L;
                key.Write(setting);

                Assert.AreEqual(1, key.BackingKey.GetValue("test"));
            }
        }

        [Test]
        public void Save_WhenSettingIsDefaultValue_ThenResetsRegistry()
        {
            using (var key = CreateSettingsKey())
            {
                key.BackingKey.SetValue("test", 42L, RegistryValueKind.QWord);

                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    Predicate.InRange(0L, 100L));

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
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    Predicate.InRange(0L, 100L));

                setting.Value = setting.DefaultValue;

                Assert.AreEqual(17L, setting.Value);
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
                    Predicate.InRange(0L, 100L));

                setting.Value = 0L;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void SetValue_WhenValueDiffersFromDefault_ThenSucceedsAndSettingIsDirty()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    Predicate.InRange(0L, 100L));

                setting.Value = 0L;

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
                    17L,
                    Predicate.InRange(0L, 100L));

                Assert.Throws<ArgumentOutOfRangeException>(() => setting.Value = -1L);
            }
        }

        //---------------------------------------------------------------------
        // AnyValue.
        //---------------------------------------------------------------------

        [Test]
        public void SetAnyValue_WhenValueIsNull_ThenResetsToDefault()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    Predicate.InRange(0L, 100L));

                setting.Value = 1L;
                ((IAnySetting)setting).AnyValue = null;

                Assert.AreEqual(17L, setting.Value);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        [Test]
        public void SetAnyValue_WhenValueIsOfWrongType_ThenThrowsException()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = (IAnySetting)key.Read(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    Predicate.InRange(0L, 100L));

                Assert.Throws<InvalidCastException>(() => setting.AnyValue = false);
            }
        }

        //---------------------------------------------------------------------
        // Policy.
        //---------------------------------------------------------------------

        [Test]
        public void Policy_WhenPolicyIsEmpty_ThenPolicyIsIgnored()
        {
            using (var key = CreateSettingsKey())
            using (var policyKey = CreatePolicySettingsKey())
            {
                var mergedKey = new MergedSettingsStore(
                    new[] { key, policyKey },
                    MergedSettingsStore.MergeBehavior.Policy);

                key.BackingKey.SetValue("test", 420000000000001L, RegistryValueKind.QWord);

                var setting = mergedKey.Read<long>(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0L, long.MaxValue));

                Assert.AreEqual(420000000000001L, setting.Value);
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

                key.BackingKey.SetValue("test", 420000000000001L, RegistryValueKind.QWord);
                policyKey.BackingKey.SetValue("test", -101, RegistryValueKind.QWord);

                var setting = mergedKey.Read<long>(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0, long.MaxValue));

                Assert.AreEqual(420000000000001L, setting.Value);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void Policy_WhenPolicyNotEmpty_ThenSettingIsMerged()
        {
            using (var key = CreateSettingsKey())
            using (var policyKey = CreatePolicySettingsKey())
            {
                var mergedKey = new MergedSettingsStore(
                    new[] { key, policyKey },
                    MergedSettingsStore.MergeBehavior.Policy);

                key.BackingKey.SetValue("test", 420000000000001L, RegistryValueKind.QWord);
                policyKey.BackingKey.SetValue("test", 880000000000001L, RegistryValueKind.QWord);

                var setting = mergedKey.Read<long>(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    Predicate.InRange(0L, long.MaxValue));

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(880000000000001L, setting.Value);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsTrue(setting.IsReadOnly);
            }
        }
    }
}
