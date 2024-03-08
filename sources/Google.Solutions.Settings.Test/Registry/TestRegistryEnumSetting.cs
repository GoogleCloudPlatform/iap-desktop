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

using Google.Solutions.Settings.Registry;
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.Settings.Test.Registry
{
    [TestFixture]
    public class TestRegistryEnumSetting
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
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    null);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(ConsoleColor.Blue, setting.Value);
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
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(ConsoleColor.Blue, setting.Value);
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
                key.SetValue("test", (int)ConsoleColor.Red, RegistryValueKind.DWord);

                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual(ConsoleColor.Red, setting.Value);
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
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                setting.Value = ConsoleColor.Green;
                setting.Save(key);

                Assert.AreEqual((int)ConsoleColor.Green, key.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsDefaultValue_ThenSaveResetsRegistry()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", (int)ConsoleColor.Red, RegistryValueKind.DWord);

                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                setting.Value = setting.DefaultValue;
                setting.Save(key);

                Assert.IsNull(key.GetValue("test"));
            }
        }

        //---------------------------------------------------------------------
        // Value.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsDefaultValue_ThenSetValueResetsToDefault()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                setting.Value = ConsoleColor.Blue;
                setting.Value = setting.DefaultValue;

                Assert.AreEqual(ConsoleColor.Blue, setting.Value);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        [Test]
        public void WhenValueEqualsDefault_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                setting.Value = ConsoleColor.Blue;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueDiffersFromDefault_ThenSetValueSucceedsAndSettingIsDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                setting.Value = ConsoleColor.Yellow;

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueIsInvalid_ThenSetValueRaisesArgumentOutOfRangeException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                Assert.Throws<ArgumentOutOfRangeException>(() => setting.Value = (ConsoleColor)100);
            }
        }

        //---------------------------------------------------------------------
        // AnyValue.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsNull_ThenSetAnyValueResetsToDefault()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                setting.Value = ConsoleColor.Blue;
                setting.AnyValue = null;

                Assert.AreEqual(ConsoleColor.Blue, setting.Value);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        [Test]
        public void WhenValueIsNumericString_ThenSetAnyValueSucceeds()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                setting.AnyValue = ((int)ConsoleColor.Yellow).ToString();

                Assert.AreEqual(ConsoleColor.Yellow, setting.Value);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueIsOfWrongType_ThenSetAnyValueRaisesInvalidCastException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                Assert.Throws<InvalidCastException>(() => setting.AnyValue = false);
            }
        }

        [Test]
        public void WhenValueIsUnparsable_ThenSetAnyValueRaisesFormatException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Blue,
                    key);

                Assert.Throws<FormatException>(() => setting.AnyValue = "");
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
                var parent = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);
                Assert.IsTrue(parent.IsDefault);

                var child = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual(ConsoleColor.Black, effective.DefaultValue);
                Assert.AreEqual(ConsoleColor.Black, effective.Value);
                Assert.IsTrue(effective.IsDefault);
            }

        }

        [Test]
        public void WhenParentIsNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);
                parent.Value = ConsoleColor.Red;
                Assert.IsFalse(parent.IsDefault);

                var child = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);
                Assert.IsTrue(child.IsDefault);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);


                Assert.AreEqual(ConsoleColor.Red, effective.Value);
                Assert.AreEqual(ConsoleColor.Red, effective.DefaultValue);
                Assert.IsTrue(effective.IsDefault);
            }
        }

        [Test]
        public void WhenChildIsNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);
                Assert.IsTrue(parent.IsDefault);
                Assert.IsFalse(parent.IsSpecified);

                key.SetValue("test", (int)ConsoleColor.Yellow);
                var child = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);
                Assert.IsFalse(child.IsDefault);
                Assert.IsTrue(child.IsSpecified);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual(ConsoleColor.Yellow, effective.Value);
                Assert.AreEqual(ConsoleColor.Black, effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentAndChildNonDefault_ThenOverlayByReturnsCorrectValues()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", (int)ConsoleColor.Red);
                var parent = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);
                Assert.IsFalse(parent.IsDefault);
                Assert.IsTrue(parent.IsSpecified);

                key.SetValue("test", (int)ConsoleColor.Green);
                var child = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);
                Assert.IsFalse(child.IsDefault);
                Assert.IsTrue(child.IsSpecified);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual(ConsoleColor.Green, effective.Value);
                Assert.AreEqual(ConsoleColor.Red, effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentIsNonDefaultAndChildSetToOriginalDefault_ThenIsDefaultReturnsFalse()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);
                parent.Value = ConsoleColor.Red;
                Assert.IsFalse(parent.IsDefault);

                var intermediate = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);
                Assert.IsTrue(intermediate.IsDefault);

                var child = RegistryEnumSetting<ConsoleColor>.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    ConsoleColor.Black,
                    key);

                var effective = parent
                    .OverlayBy(intermediate)
                    .OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, intermediate);
                Assert.AreNotSame(effective, child);

                effective.Value = ConsoleColor.Black;

                Assert.AreEqual(ConsoleColor.Black, effective.Value);
                Assert.AreEqual(ConsoleColor.Red, effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }
    }
}
