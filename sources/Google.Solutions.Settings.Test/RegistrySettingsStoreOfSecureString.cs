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

namespace Google.Solutions.Settings.Test
{
    [TestFixture]
    public class RegistrySettingsStoreOfSecureString : RegistrySettingsStoreBase
    {
        //---------------------------------------------------------------------
        // IsSpecified.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueChanged_ThenIsSpecifiedIsTrue()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.IsFalse(setting.IsSpecified);
                Assert.IsTrue(setting.IsDefault);

                setting.SetClearTextValue("value");

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
        public void WhenRegistryValueDoesNotExist_ThenFromKeyUsesDefaults()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
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
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
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
            using (var key = CreateSettingsKey())
            {
                key.BackingKey.SetValue("test", Encoding.ASCII.GetBytes("gibberish"), RegistryValueKind.Binary);

                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.IsNull(setting.Value);
                Assert.IsNull(setting.GetClearTextValue());
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
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("red");
                key.Write(setting);

                Assert.IsNotNull(key.BackingKey.GetValue("test"));

                // Now read again.

                setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.DisplayName);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("red", setting.GetClearTextValue());
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
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("green");
                key.Write(setting);

                Assert.IsNotNull(key.BackingKey.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsDefaultValue_ThenSaveResetsRegistry()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("red");
                key.Write(setting);

                Assert.IsNotNull(key.BackingKey.GetValue("test"));

                // Now write again.

                setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

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
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = null;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueAndDefaultAreNull_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

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
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                setting.Value = SecureStringExtensions.FromClearText("yellow");

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        //---------------------------------------------------------------------
        // AnyValue.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsOfWrongType_ThenSetAnyValueRaisesInvalidCastException()
        {
            using (var key = CreateSettingsKey())
            {
                var setting = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                Assert.Throws<InvalidCastException>(() => setting.AnyValue = 1);
            }
        }

        //---------------------------------------------------------------------
        // Overlay.
        //---------------------------------------------------------------------

        [Test]
        public void WhenParentAndChildDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = CreateSettingsKey())
            {
                var parent = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);
                Assert.IsTrue(parent.IsDefault);

                var child = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

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
            using (var key = CreateSettingsKey())
            {
                var parent = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);
                parent.SetClearTextValue("red");
                Assert.IsFalse(parent.IsDefault);

                var child = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);
                Assert.IsTrue(child.IsDefault);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual("red", effective.Value.AsClearText());
                Assert.AreEqual("red", effective.DefaultValue.AsClearText());
                Assert.IsTrue(effective.IsDefault);
            }
        }

        [Test]
        public void WhenChildIsNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = CreateSettingsKey())
            {
                var parent = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);
                Assert.IsTrue(parent.IsDefault);
                Assert.IsFalse(parent.IsSpecified);

                RegistryValueAccessor.Create<SecureString>("test")
                    .Write(
                        key.BackingKey,
                        SecureStringExtensions.FromClearText("yellow"));
                var child = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);
                Assert.IsFalse(child.IsDefault);
                Assert.IsTrue(child.IsSpecified);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual("yellow", effective.Value.AsClearText());
                Assert.IsNull(effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentAndChildNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = CreateSettingsKey())
            {
                RegistryValueAccessor.Create<SecureString>("test")
                    .Write(
                        key.BackingKey,
                        SecureStringExtensions.FromClearText("red"));
                var parent = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);
                Assert.IsFalse(parent.IsDefault);
                Assert.IsTrue(parent.IsSpecified);

                RegistryValueAccessor.Create<SecureString>("test")
                    .Write(
                        key.BackingKey,
                        SecureStringExtensions.FromClearText("green"));
                var child = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);
                Assert.IsFalse(child.IsDefault);
                Assert.IsTrue(child.IsSpecified);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual("green", effective.Value.AsClearText());
                Assert.AreEqual("red", effective.DefaultValue.AsClearText());
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentIsNonDefaultAndChildSetToOriginalDefault_ThenIsDefaultReturnsFalse()
        {
            using (var key = CreateSettingsKey())
            {
                var parent = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);
                parent.SetClearTextValue("red");
                Assert.IsFalse(parent.IsDefault);

                var intermediate = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);
                Assert.IsTrue(intermediate.IsDefault);

                var child = key.Read<SecureString>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null);

                var effective = parent
                    .OverlayBy(intermediate)
                    .OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, intermediate);
                Assert.AreNotSame(effective, child);

                effective.SetClearTextValue("black");

                Assert.AreEqual("black", effective.Value.AsClearText());
                Assert.AreEqual("red", effective.DefaultValue.AsClearText());
                Assert.IsFalse(effective.IsDefault);
            }
        }
    }
}
