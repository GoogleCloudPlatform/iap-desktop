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
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Settings
{
    [TestFixture]
    public class TestRegistryQwordSetting
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
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, 100L);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(17L, setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenRegistryKeyIsNull_ThenFromKeyUsesDefaults()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    null,
                    0L, 100L);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(17L, setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenRegistryValueExists_ThenFromKeyUsesValue()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", 420000000000001L, RegistryValueKind.QWord);

                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, long.MaxValue);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(420000000000001, setting.Value);
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
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, 100L);

                setting.Value = 1L;
                setting.Save(key);

                Assert.AreEqual(1, key.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsNull_ThenSaveResetsRegistry()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", 42L, RegistryValueKind.QWord);

                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, 100L);

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
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, 100L);

                setting.Value = 1L;
                setting.Value = null;

                Assert.AreEqual(17L, setting.Value);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        [Test]
        public void WhenValueEqualsDefault_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, 100L);

                setting.Value = 17L;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueAndDefaultAreNull_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    0,
                    key,
                    0L, 100L);

                setting.Value = 0L;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueDiffersFromDefault_ThenSetValueSucceedsAndSettingIsDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, 100L);

                setting.Value = 0L;

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueIsString_ThenSetValueParsesValue()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, long.MaxValue);

                setting.Value = "120000000000000001";

                Assert.AreEqual(120000000000000001L, setting.Value);
            }
        }

        [Test]
        public void WhenValueIsOfWrongType_ThenSetValueRaisesInvalidCastException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, 100L);

                Assert.Throws<InvalidCastException>(() => setting.Value = false);
            }
        }

        [Test]
        public void WhenValueIsInvalid_ThenSetValueRaisesArgumentOutOfRangeException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, 100L);

                Assert.Throws<ArgumentOutOfRangeException>(() => setting.Value = -1L);
            }
        }

        [Test]
        public void WhenValueIsUnparsable_ThenSetValueRaisesFormatException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    17L,
                    key,
                    0L, 100L);

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
                var parent = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);
                Assert.IsTrue(parent.IsDefault);

                var child = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);

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
                var parent = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);
                parent.Value = 42L;
                Assert.IsFalse(parent.IsDefault);

                var child = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);
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
                var parent = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);
                Assert.IsTrue(parent.IsDefault);

                var child = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);
                child.Value = 1L;
                Assert.IsFalse(child.IsDefault);

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
                var parent = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);
                parent.Value = 42L;
                Assert.IsFalse(parent.IsDefault);

                var child = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);
                child.Value = 1L;
                Assert.IsFalse(child.IsDefault);

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
                var parent = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);
                parent.Value = 42L;
                Assert.IsFalse(parent.IsDefault);

                var intermediate = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);
                Assert.IsTrue(intermediate.IsDefault);

                var child = RegistryQwordSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    10L,
                    key,
                    0L, 100L);

                var effective = parent
                    .OverlayBy(intermediate)
                    .OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, intermediate);
                Assert.AreNotSame(effective, child);

                effective.Value = 10L;

                Assert.AreEqual(10, effective.Value);
                Assert.AreEqual(42, effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }
    }
}
