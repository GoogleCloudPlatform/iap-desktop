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

using Google.Solutions.IapDesktop.Application.Profile.Settings.Registry;
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings.Registry
{
    [TestFixture]
    public class TestRegistryDwordSetting
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private const string TestPolicyKeyPath = @"Software\Google\__TestPolicy";

        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.hkcu.DeleteSubKeyTree(TestPolicyKeyPath, false);
        }

        //---------------------------------------------------------------------
        // IsSpecified.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueChanged_ThenIsSpecifiedIsTrue()
        {
            var setting = RegistryDwordSetting.FromKey(
                "test",
                "title",
                "description",
                "category",
                17,
                null,
                0, 100);

            Assert.IsFalse(setting.IsSpecified);
            Assert.IsTrue(setting.IsDefault);

            setting.IntValue = 1;

            Assert.IsTrue(setting.IsSpecified);
            Assert.IsFalse(setting.IsDefault);

            setting.IntValue = setting.DefaultValue;

            Assert.IsTrue(setting.IsSpecified);
            Assert.IsTrue(setting.IsDefault);
        }

        //---------------------------------------------------------------------
        // Load.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRegistryKeyIsNull_ThenFromKeyUsesDefaults()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    null,
                    0, 100);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
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
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
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
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", 42);

                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(42, setting.Value);
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
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                setting.Value = 1;
                setting.Save(key);

                Assert.AreEqual(1, key.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsNull_ThenSaveResetsRegistry()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", 42);

                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                setting.Value = null;
                setting.Save(key);

                Assert.IsNull(key.GetValue("test"));
            }
        }

        //---------------------------------------------------------------------
        // Get/set value.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsNull_ThenSetValueResetsToDefault()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                setting.Value = 1;
                setting.Value = null;

                Assert.AreEqual(17, setting.Value);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        [Test]
        public void WhenValueEqualsDefault_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                setting.Value = 17;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueAndDefaultAreNull_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    0,
                    key,
                    0, 100);

                setting.Value = 0;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueDiffersFromDefault_ThenSetValueSucceedsAndSettingIsDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                setting.Value = 0;

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueIsString_ThenSetValueParsesValue()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                setting.Value = "12";

                Assert.AreEqual(12, setting.Value);
            }
        }

        [Test]
        public void WhenValueIsOfWrongType_ThenSetValueRaisesInvalidCastException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                Assert.Throws<InvalidCastException>(() => setting.Value = false);
            }
        }

        [Test]
        public void WhenValueIsInvalid_ThenSetValueRaisesArgumentOutOfRangeException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                Assert.Throws<ArgumentOutOfRangeException>(() => setting.Value = -1);
            }
        }

        [Test]
        public void WhenValueIsUnparsable_ThenSetValueRaisesFormatException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                Assert.Throws<FormatException>(() => setting.Value = "test");
            }
        }

        //---------------------------------------------------------------------
        // Overlay.
        //---------------------------------------------------------------------

        [Test]
        public void WhenParentAndChildDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);
                Assert.IsTrue(parent.IsDefault);

                var child = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual(10, effective.DefaultValue);
                Assert.AreEqual(10, effective.Value);
                Assert.IsTrue(effective.IsDefault);
            }

        }

        [Test]
        public void WhenParentIsNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);
                parent.Value = 42;
                Assert.IsFalse(parent.IsDefault);

                var child = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);
                Assert.IsTrue(child.IsDefault);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);


                Assert.AreEqual(42, effective.Value);
                Assert.AreEqual(42, effective.DefaultValue);
                Assert.IsTrue(effective.IsDefault);
            }
        }

        [Test]
        public void WhenChildIsNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);
                Assert.IsTrue(parent.IsDefault);
                Assert.IsFalse(parent.IsSpecified);

                key.SetValue("test", 1);
                var child = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);
                Assert.IsFalse(child.IsDefault);
                Assert.IsTrue(child.IsSpecified);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual(1, effective.Value);
                Assert.AreEqual(10, effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentAndChildNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", 42);
                var parent = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);
                Assert.IsFalse(parent.IsDefault);
                Assert.IsTrue(parent.IsSpecified);

                key.SetValue("test", 1);
                var child = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);
                Assert.IsFalse(child.IsDefault);
                Assert.IsTrue(child.IsSpecified);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual(1, effective.Value);
                Assert.AreEqual(42, effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentIsNonDefaultAndChildSetToOriginalDefault_ThenIsDefaultReturnsFalse()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);
                parent.Value = 42;
                Assert.IsFalse(parent.IsDefault);

                var intermediate = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);
                Assert.IsTrue(intermediate.IsDefault);

                var child = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10,
                    key,
                    0, 100);

                var effective = parent
                    .OverlayBy(intermediate)
                    .OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, intermediate);
                Assert.AreNotSame(effective, child);

                effective.Value = 10;

                Assert.AreEqual(10, effective.Value);
                Assert.AreEqual(42, effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // Policy.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPolicyKeyIsNull_ThenApplyPolicyReturnsThis()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", 42);

                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                var settingWithPolicy = setting.ApplyPolicy(null);

                Assert.AreSame(setting, settingWithPolicy);
            }
        }

        [Test]
        public void WhenPolicyValueIsMissing_ThenApplyPolicyReturnsThis()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestPolicyKeyPath))
            {
                key.SetValue("test", 42);

                var setting = RegistryDwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17,
                    key,
                    0, 100);

                var settingWithPolicy = setting.ApplyPolicy(policyKey);

                Assert.AreSame(setting, settingWithPolicy);
            }
        }

        [Test]
        public void WhenPolicyInvalid_ThenApplyPolicyReturnsThis()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestPolicyKeyPath))
            {
                key.SetValue("test", 42);
                policyKey.SetValue("test", 101, RegistryValueKind.DWord);

                var setting = RegistryDwordSetting.FromKey(
                        "test",
                        "title",
                        "description",
                        "category",
                        17,
                        key,
                        0, 100)
                    .ApplyPolicy(policyKey);

                var settingWithPolicy = setting.ApplyPolicy(policyKey);

                Assert.AreSame(setting, settingWithPolicy);
            }
        }

        [Test]
        public void WhenPolicySet_ThenApplyPolicyReturnsReadOnlySettingWithPolicyApplied()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            using (var policyKey = this.hkcu.CreateSubKey(TestPolicyKeyPath))
            {
                key.SetValue("test", 42);
                policyKey.SetValue("test", 88, RegistryValueKind.DWord);

                var setting = RegistryDwordSetting.FromKey(
                        "test",
                        "title",
                        "description",
                        "category",
                        17,
                        key,
                        0, 100)
                    .ApplyPolicy(policyKey);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(88, setting.IntValue);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsTrue(setting.IsReadOnly);
            }
        }
    }
}
