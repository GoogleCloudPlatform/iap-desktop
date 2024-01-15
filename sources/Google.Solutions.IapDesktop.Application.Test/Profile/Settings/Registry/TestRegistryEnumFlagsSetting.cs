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
    public class TestRegistryEnumFlagsSetting
    {
        [Flags]
        public enum Toppings
        {
            None = 0,
            Cheese = 1,
            Chocolate = 2,
            Cream = 4
        }

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
            var setting = RegistryEnumSetting<Toppings>.FromKey(
                "test",
                "title",
                "description",
                "category",
                Toppings.None,
                null);

            Assert.IsFalse(setting.IsSpecified);
            Assert.IsTrue(setting.IsDefault);

            setting.EnumValue = Toppings.Cheese;

            Assert.IsTrue(setting.IsSpecified);
            Assert.IsFalse(setting.IsDefault);

            setting.EnumValue = setting.DefaultValue;

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
                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    null);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(Toppings.None, setting.Value);
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
                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(Toppings.None, setting.Value);
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
                key.SetValue(
                    "test",
                    (int)(Toppings.Cheese | Toppings.Chocolate),
                    RegistryValueKind.DWord);

                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(Toppings.Cheese | Toppings.Chocolate, setting.Value);
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
                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

                setting.Value = Toppings.Cream | Toppings.Chocolate;
                setting.Save(key);

                Assert.AreEqual((int)(Toppings.Cream | Toppings.Chocolate), key.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsNull_ThenSaveResetsRegistry()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", (int)Toppings.Cream, RegistryValueKind.DWord);

                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

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
                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

                setting.Value = Toppings.None;
                setting.Value = null;

                Assert.AreEqual(Toppings.None, setting.Value);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        [Test]
        public void WhenValueEqualsDefault_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

                setting.Value = Toppings.None;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueDiffersFromDefault_ThenSetValueSucceedsAndSettingIsDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

                setting.Value = Toppings.Cream;

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueIsNumericString_ThenSetValueSucceeds()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

                setting.Value = ((int)Toppings.Cream).ToString();

                Assert.AreEqual(Toppings.Cream, setting.Value);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueIsOfWrongType_ThenSetValueRaisesInvalidCastException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

                Assert.Throws<InvalidCastException>(() => setting.Value = false);
            }
        }

        [Test]
        public void WhenValueIsInvalid_ThenSetValueRaisesArgumentOutOfRangeException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

                Assert.Throws<ArgumentOutOfRangeException>(() => setting.Value = (Toppings)100);
            }
        }

        [Test]
        public void WhenValueIsUnparsable_ThenSetValueRaisesFormatException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    key);

                Assert.Throws<FormatException>(() => setting.Value = "");
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
                key.SetValue(
                    "test",
                    (int)(Toppings.Cheese | Toppings.Chocolate),
                    RegistryValueKind.DWord);

                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    null);

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
                key.SetValue(
                    "test",
                    (int)(Toppings.Cheese | Toppings.Chocolate),
                    RegistryValueKind.DWord);

                var setting = RegistryEnumSetting<Toppings>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    Toppings.None,
                    null);

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
                key.SetValue(
                    "test",
                    (int)(Toppings.Cheese | Toppings.Chocolate),
                    RegistryValueKind.DWord);

                policyKey.SetValue(
                    "test",
                    -123,
                    RegistryValueKind.DWord);

                var setting = RegistryEnumSetting<Toppings>.FromKey(
                        "test",
                        "title",
                        "description",
                        "category",
                        Toppings.None,
                        null)
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
                key.SetValue(
                    "test",
                    (int)(Toppings.Cheese | Toppings.Chocolate),
                    RegistryValueKind.DWord);

                policyKey.SetValue(
                    "test",
                    (int)Toppings.Cream,
                    RegistryValueKind.DWord);

                var setting = RegistryEnumSetting<Toppings>.FromKey(
                        "test",
                        "title",
                        "description",
                        "category",
                        Toppings.None,
                        null)
                    .ApplyPolicy(policyKey);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(Toppings.Cream, setting.EnumValue);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsTrue(setting.IsReadOnly);
            }
        }
    }
}
