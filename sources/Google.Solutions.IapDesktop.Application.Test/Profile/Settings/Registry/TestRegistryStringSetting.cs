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
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings.Registry
{
    [TestFixture]
    public class TestRegistryStringSetting
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
            var setting = RegistryStringSetting.FromKey(
                "test",
                "title",
                "description",
                "category",
                "blue",
                null,
                _ => true);

            Assert.IsFalse(setting.IsSpecified);
            Assert.IsTrue(setting.IsDefault);

            setting.StringValue = "red";

            Assert.IsTrue(setting.IsSpecified);
            Assert.IsFalse(setting.IsDefault);

            setting.StringValue = setting.DefaultValue;

            Assert.IsTrue(setting.IsSpecified);
            Assert.IsTrue(setting.IsDefault);
        }

        //---------------------------------------------------------------------
        // Load.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRegistryValueDoesNotExist_ThenFromKeyUsesDefaults()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("blue", setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void WhenRegistryKeyIsNull_ThenFromKeyUsesDefaults()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    null,
                    _ => true);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("blue", setting.Value);
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
                key.SetValue("test", "red");

                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
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
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                setting.Value = "green";
                setting.Save(key);

                Assert.AreEqual("green", key.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsNull_ThenSaveResetsRegistry()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", "red");

                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

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
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                setting.Value = "blue";
                setting.Value = null;

                Assert.AreEqual("blue", setting.Value);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        [Test]
        public void WhenValueEqualsDefault_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                setting.Value = "blue";

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueAndDefaultAreNull_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    null,
                    key,
                    _ => true);

                setting.Value = null;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueDiffersFromDefault_ThenSetValueSucceedsAndSettingIsDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                setting.Value = "yellow";

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueIsOfWrongType_ThenSetValueRaisesInvalidCastException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                Assert.Throws<InvalidCastException>(() => setting.Value = 1);
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
                var parent = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsTrue(parent.IsDefault);

                var child = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual("black", effective.DefaultValue);
                Assert.AreEqual("black", effective.Value);
                Assert.IsTrue(effective.IsDefault);
            }

        }

        [Test]
        public void WhenParentIsNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                parent.Value = "red";
                Assert.IsFalse(parent.IsDefault);

                var child = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsTrue(child.IsDefault);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);


                Assert.AreEqual("red", effective.Value);
                Assert.AreEqual("red", effective.DefaultValue);
                Assert.IsTrue(effective.IsDefault);
            }
        }

        [Test]
        public void WhenChildIsNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsTrue(parent.IsDefault);
                Assert.IsFalse(parent.IsSpecified);

                key.SetValue("test", "yellow");
                var child = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsFalse(child.IsDefault);
                Assert.IsTrue(child.IsSpecified);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual("yellow", effective.Value);
                Assert.AreEqual("black", effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentAndChildNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", "red");
                var parent = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsFalse(parent.IsDefault);
                Assert.IsTrue(parent.IsSpecified);

                key.SetValue("test", "green");
                var child = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsFalse(child.IsDefault);
                Assert.IsTrue(child.IsSpecified);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual("green", effective.Value);
                Assert.AreEqual("red", effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentIsNonDefaultAndChildSetToOriginalDefault_ThenIsDefaultReturnsFalse()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                parent.Value = "red";
                Assert.IsFalse(parent.IsDefault);

                var intermediate = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsTrue(intermediate.IsDefault);

                var child = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);

                var effective = parent
                    .OverlayBy(intermediate)
                    .OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, intermediate);
                Assert.AreNotSame(effective, child);

                effective.Value = "black";

                Assert.AreEqual("black", effective.Value);
                Assert.AreEqual("red", effective.DefaultValue);
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
                key.SetValue("test", "red");

                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

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
                key.SetValue("test", "red");

                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

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
                key.SetValue("test", "red");
                policyKey.SetValue("test", "BLUE");

                var setting = RegistryStringSetting.FromKey(
                        "test",
                        "title",
                        "description",
                        "category",
                        "black",
                        key,
                        v => v.ToLower() == v)
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
                key.SetValue("test", "red");
                policyKey.SetValue("test", "BLUE");

                var setting = RegistryStringSetting.FromKey(
                        "test",
                        "title",
                        "description",
                        "category",
                        "black",
                        key,
                        _ => true)
                    .ApplyPolicy(policyKey);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("BLUE", setting.StringValue);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsTrue(setting.IsReadOnly);
            }
        }
    }
}
