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

using Google.Solutions.Settings.Registry;
using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.Settings.Test.Registry
{
    [TestFixture]
    public class TestMergedSettingsKey
    {
        private const string KeyPath = @"Software\Google\__Test";
        private const string OverlayKeyPath = @"Software\Google\__TestOverlay";

        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(KeyPath, false);
            this.hkcu.DeleteSubKeyTree(OverlayKeyPath, false);
        }

        protected SettingsKey CreateLesserSettingsKey()
        {
            return new SettingsKey(this.hkcu.CreateSubKey(KeyPath));
        }

        protected SettingsKey CreateMergedSettingsKey(
            SettingsKey lesserKey,
            MergedSettingsKey.MergeBehavior mergeBehavior)
        {
            return new MergedSettingsKey(
                this.hkcu.CreateSubKey(OverlayKeyPath),
                lesserKey,
                mergeBehavior);
        }

        //---------------------------------------------------------------------
        // Policy.
        //---------------------------------------------------------------------

        [Test]
        public void PolicyKeyValueEmpty_LesserKeyValueEmpty()
        {
            using (var lesserKey = CreateLesserSettingsKey())
            using (var mergedKey = CreateMergedSettingsKey(
                lesserKey, 
                MergedSettingsKey.MergeBehavior.Policy))
            {
                var defaultValue = 1;
                var setting = mergedKey.Read<int>("test", "test", null, null, defaultValue);

                Assert.AreEqual(defaultValue, setting.Value);
                Assert.AreEqual(defaultValue, setting.DefaultValue);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsSpecified);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void PolicyKeyValueEmpty_LesserKeyValueSet()
        {
            using (var lesserKey = CreateLesserSettingsKey())
            using (var mergedKey = CreateMergedSettingsKey(
                lesserKey, 
                MergedSettingsKey.MergeBehavior.Policy))
            {
                lesserKey.BackingKey.SetValue("test", 2);

                var defaultValue = 1;
                var setting = mergedKey.Read<int>("test", "test", null, null, defaultValue);

                Assert.AreEqual(2, setting.Value);
                Assert.AreEqual(defaultValue, setting.DefaultValue);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsSpecified);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void PolicyKeyValueSet_LesserKeyValueEmpty()
        {
            using (var lesserKey = CreateLesserSettingsKey())
            using (var mergedKey = CreateMergedSettingsKey(
                lesserKey, 
                MergedSettingsKey.MergeBehavior.Policy))
            {
                mergedKey.BackingKey.SetValue("test", 3);

                var defaultValue = 1;
                var setting = mergedKey.Read<int>("test", "test", null, null, defaultValue);

                Assert.AreEqual(3, setting.Value);
                Assert.AreEqual(defaultValue, setting.DefaultValue);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsSpecified);
                Assert.IsTrue(setting.IsReadOnly);
            }
        }

        [Test]
        public void PolicyKeyValueSet_LesserKeyValueSet()
        {
            using (var lesserKey = CreateLesserSettingsKey())
            using (var mergedKey = CreateMergedSettingsKey(
                lesserKey, 
                MergedSettingsKey.MergeBehavior.Policy))
            {
                lesserKey.BackingKey.SetValue("test", 2);
                mergedKey.BackingKey.SetValue("test", 3);

                var defaultValue = 1;
                var setting = mergedKey.Read<int>("test", "test", null, null, defaultValue);

                Assert.AreEqual(3, setting.Value);
                Assert.AreEqual(defaultValue, setting.DefaultValue);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsSpecified);
                Assert.IsTrue(setting.IsReadOnly);
            }
        }

        //---------------------------------------------------------------------
        // Override.
        //---------------------------------------------------------------------

        [Test]
        public void OverrideKeyValueEmpty_LesserKeyValueEmpty()
        {
            using (var lesserKey = CreateLesserSettingsKey())
            using (var mergedKey = CreateMergedSettingsKey(
                lesserKey, 
                MergedSettingsKey.MergeBehavior.Override))
            {
                var defaultValue = 1;
                var setting = mergedKey.Read<int>("test", "test", null, null, defaultValue);

                Assert.AreEqual(defaultValue, setting.Value);
                Assert.AreEqual(defaultValue, setting.DefaultValue);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsSpecified);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void OverrideKeyValueEmpty_LesserKeyValueSet()
        {
            using (var lesserKey = CreateLesserSettingsKey())
            using (var mergedKey = CreateMergedSettingsKey(
                lesserKey, MergedSettingsKey.
                MergeBehavior.Override))
            {
                lesserKey.BackingKey.SetValue("test", 2);

                var defaultValue = 1;
                var setting = mergedKey.Read<int>("test", "test", null, null, defaultValue);

                Assert.AreEqual(2, setting.Value);
                Assert.AreEqual(2, setting.DefaultValue);   // New default
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsSpecified);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void OverrideKeyValueSet_LesserKeyValueEmpty()
        {
            using (var lesserKey = CreateLesserSettingsKey())
            using (var mergedKey = CreateMergedSettingsKey(
                lesserKey, 
                MergedSettingsKey.MergeBehavior.Override))
            {
                mergedKey.BackingKey.SetValue("test", 3);

                var defaultValue = 1;
                var setting = mergedKey.Read<int>("test", "test", null, null, defaultValue);

                Assert.AreEqual(3, setting.Value);
                Assert.AreEqual(defaultValue, setting.DefaultValue);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsSpecified);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void OverrideKeyValueSet_LesserKeyValueSet()
        {
            using (var lesserKey = CreateLesserSettingsKey())
            using (var mergedKey = CreateMergedSettingsKey(
                lesserKey,
                MergedSettingsKey.MergeBehavior.Override))
            {
                lesserKey.BackingKey.SetValue("test", 2);
                mergedKey.BackingKey.SetValue("test", 3);

                var defaultValue = 1;
                var setting = mergedKey.Read<int>("test", "test", null, null, defaultValue);

                Assert.AreEqual(3, setting.Value);
                Assert.AreEqual(2, setting.DefaultValue);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsSpecified);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }
    }
}
