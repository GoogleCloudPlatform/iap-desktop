﻿//
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

using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.IapDesktop.Application.Test.Settings
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
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
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
        public void WhenSettingIsNull_ThenSaveResetsRegistry()
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
                var setting = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);

                setting.Value = SecureStringExtensions.FromClearText("blue");
                setting.Value = null;

                Assert.IsNull(setting.Value);
                Assert.IsTrue(setting.IsDefault);
            }
        }

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

        [Test]
        public void WhenValueIsString_ThenSetValueParsesValue()
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

                setting.Value = "secret";

                Assert.AreEqual("secret", setting.ClearTextValue);
            }
        }

        [Test]
        public void WhenValueIsOfWrongType_ThenSetValueRaisesInvalidCastException()
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
                parent.Value = "red";
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
                Assert.AreEqual("red", ((SecureString)effective.DefaultValue).AsClearText());
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

                var child = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                child.Value = "yellow";
                Assert.IsFalse(child.IsDefault);

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
                var parent = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                parent.Value = "red";
                Assert.IsFalse(parent.IsDefault);

                var child = RegistrySecureStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    key,
                    DataProtectionScope.CurrentUser);
                child.Value = "green";
                Assert.IsFalse(child.IsDefault);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual("green", ((SecureString)effective.Value).AsClearText());
                Assert.AreEqual("red", ((SecureString)effective.DefaultValue).AsClearText());
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
                parent.Value = "red";
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

                var effective = parent
                    .OverlayBy(intermediate)
                    .OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, intermediate);
                Assert.AreNotSame(effective, child);

                effective.Value = "black";

                Assert.AreEqual("black", ((SecureString)effective.Value).AsClearText());
                Assert.AreEqual("red", ((SecureString)effective.DefaultValue).AsClearText());
                Assert.IsFalse(effective.IsDefault);
            }
        }
    }
}
