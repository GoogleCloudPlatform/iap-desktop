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

using Google.Solutions.Common.Security;
using Google.Solutions.Settings.Registry;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.Settings.Test.Registry
{
    [TestFixture]
    public class TestRegistrySecureStringSetting
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
        }

        //---------------------------------------------------------------------
        // IsSpecified.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueChanged_ThenIsSpecifiedIsTrue()
        {
            var setting = RegistrySecureStringSetting.FromKey(
                "test",
                "title",
                "description",
                "category",
                null,
                DataProtectionScope.CurrentUser);

            Assert.IsFalse(setting.IsSpecified);
            Assert.IsTrue(setting.IsDefault);

            setting.ClearTextValue = "value";

            Assert.IsTrue(setting.IsSpecified);
            Assert.IsFalse(setting.IsDefault);

            setting.Value = setting.DefaultValue;

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
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsNull(setting.Value);
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
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    null,
                    DataProtectionScope.CurrentUser);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsNull(setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
                Assert.IsFalse(setting.IsReadOnly);
            }
        }

        [Test]
        public void WhenRegistryValueContainsGibberish_ThenFromKeyUsesDefaults()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", Encoding.ASCII.GetBytes("gibberish"), RegistryValueKind.Binary);

                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsNull(setting.Value);
                Assert.IsNull(setting.ClearTextValue);
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
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                setting.Value = SecureStringExtensions.FromClearText("red");
                setting.Save(key);

                Assert.IsNotNull(key.GetValue("test"));

                // Now read again.

                setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("red", setting.ClearTextValue);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
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
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                setting.Value = SecureStringExtensions.FromClearText("green");
                setting.Save(key);

                Assert.IsNotNull(key.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsDefaultValue_ThenSaveResetsRegistry()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                setting.Value = SecureStringExtensions.FromClearText("red");
                setting.Save(key);

                Assert.IsNotNull(key.GetValue("test"));

                // Now write again.

                setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                setting.Value = setting.DefaultValue;
                setting.Save(key);

                Assert.IsNull(key.GetValue("test"));
            }
        }

        //---------------------------------------------------------------------
        // Value.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueEqualsDefault_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                setting.Value = null;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueAndDefaultAreNull_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

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
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                setting.Value = SecureStringExtensions.FromClearText("yellow");

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        //---------------------------------------------------------------------
        // AnyValue.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsString_ThenSetAnyValueParsesValue()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                setting.AnyValue = "secret";

                Assert.AreEqual("secret", setting.ClearTextValue);
            }
        }

        [Test]
        public void WhenValueIsOfWrongType_ThenSetAnyValueRaisesInvalidCastException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                Assert.Throws<InvalidCastException>(() => setting.AnyValue = 1);
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
                var parent = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                Assert.IsTrue(parent.IsDefault);

                var child = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.IsNull(effective.DefaultValue);
                Assert.IsNull(effective.Value);
                Assert.IsTrue(effective.IsDefault);
            }

        }

        [Test]
        public void WhenParentIsNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                parent.ClearTextValue = "red";
                Assert.IsFalse(parent.IsDefault);

                var child = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                Assert.IsTrue(child.IsDefault);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);


                Assert.AreEqual("red", ((SecureString)effective.Value).AsClearText());
                Assert.AreEqual("red", effective.DefaultValue.AsClearText());
                Assert.IsTrue(effective.IsDefault);
            }
        }

        [Test]
        public void WhenChildIsNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                Assert.IsTrue(parent.IsDefault);
                Assert.IsFalse(parent.IsSpecified);

                key.SetValue(
                    "test",
                    RegistrySecureStringSetting.Encrypt(
                        "test",
                        DataProtectionScope.CurrentUser,
                        SecureStringExtensions.FromClearText("yellow")));
                var child = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                Assert.IsFalse(child.IsDefault);
                Assert.IsTrue(child.IsSpecified);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual("yellow", ((SecureString)effective.Value).AsClearText());
                Assert.IsNull(effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentAndChildNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue(
                    "test",
                    RegistrySecureStringSetting.Encrypt(
                        "test",
                        DataProtectionScope.CurrentUser,
                        SecureStringExtensions.FromClearText("red")));
                var parent = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                Assert.IsFalse(parent.IsDefault);
                Assert.IsTrue(parent.IsSpecified);

                key.SetValue(
                    "test",
                    RegistrySecureStringSetting.Encrypt(
                        "test",
                        DataProtectionScope.CurrentUser,
                        SecureStringExtensions.FromClearText("green")));
                var child = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                Assert.IsFalse(child.IsDefault);
                Assert.IsTrue(child.IsSpecified);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual("green", ((SecureString)effective.Value).AsClearText());
                Assert.AreEqual("red", effective.DefaultValue.AsClearText());
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentIsNonDefaultAndChildSetToOriginalDefault_ThenIsDefaultReturnsFalse()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                parent.ClearTextValue = "red";
                Assert.IsFalse(parent.IsDefault);

                var intermediate = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                Assert.IsTrue(intermediate.IsDefault);

                var child = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                var effective = (RegistrySecureStringSetting)parent
                    .OverlayBy(intermediate)
                    .OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, intermediate);
                Assert.AreNotSame(effective, child);

                effective.ClearTextValue = "black";

                Assert.AreEqual("black", ((SecureString)effective.Value).AsClearText());
                Assert.AreEqual("red", effective.DefaultValue.AsClearText());
                Assert.IsFalse(effective.IsDefault);
            }
        }
    }
}
