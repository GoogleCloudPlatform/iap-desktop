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
using System;

namespace Google.Solutions.Settings.Test.Registry
{
    [TestFixture]
    public class TestRegistrySettingsStore
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

        protected RegistryKey CreateKey()
        {
            return this.hkcu.CreateSubKey(TestKeyPath);
        }

        //---------------------------------------------------------------------
        // Read.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTypeUnsupported_ThenReadThrowsException()
        {
            using (var key = new RegistrySettingsStore(CreateKey()))
            {
                Assert.Throws<ArgumentException>(
                    () => key.Read<uint>("test", "test", null, null, 0));
            }
        }

        [Test]
        public void WhenValueExists_ThenIsSpecifiedIsTrue()
        {
            using (var key = new RegistrySettingsStore(CreateKey()))
            {
                //
                // Read non-existing value.
                //
                var setting = key.Read<int>("test", "test", null, null, 0);
                Assert.IsFalse(setting.IsSpecified);

                //
                // Write and read again.
                //
                setting.Value = 1;
                key.Write(setting);

                setting = key.Read<int>("test", "test", null, null, 0);
                Assert.IsTrue(setting.IsSpecified);
            }
        }

        [Test]
        public void WhenValueExists_ThenValueIsNotDefault()
        {
            using (var key = new RegistrySettingsStore(CreateKey()))
            {
                //
                // Read non-existing value.
                //
                var defaultValue = -1;
                var setting = key.Read<int>("test", "test", null, null, defaultValue);
                Assert.IsTrue(setting.IsDefault);
                Assert.AreEqual(defaultValue, setting.Value);

                //
                // Write and read again.
                //
                setting.Value = 1;
                key.Write(setting);

                setting = key.Read<int>("test", "test", null, null, 0);
                Assert.IsFalse(setting.IsDefault);
                Assert.AreEqual(1, setting.Value);
            }
        }

        [Test]
        public void WhenCustomValidationFails_ThenSetValueThrowsException()
        {
            using (var key = new RegistrySettingsStore(CreateKey()))
            {
                //
                // Read non-existing value.
                //
                var setting = key.Read<int>(
                    "test",
                    "test", 
                    null, 
                    null, 
                    0,
                    v => v > 0);

                //
                // Assign valid value.
                //
                setting.Value = 1;

                //
                // Assign invalid value.
                //
                Assert.Throws<ArgumentOutOfRangeException>(() => setting.Value = 0);
            }
        }

        //---------------------------------------------------------------------
        // Write.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsDefault_ThenWriteDeletesValue()
        {
            using (var key = new RegistrySettingsStore(CreateKey()))
            {
                var setting = key.Read<int>("test", "test", null, null, 0);

                //
                // Write non-default value.
                //
                setting.Value = 1;
                key.Write(setting);

                //
                // Write default value.
                //
                setting = key.Read<int>("test", "test", null, null, 0);
                Assert.IsFalse(setting.IsDefault);
                setting.Reset();
                key.Write(setting);

                //
                // Status back to "not specified".
                //
                setting = key.Read<int>("test", "test", null, null, 0);
                Assert.IsFalse(setting.IsSpecified);
                Assert.IsTrue(setting.IsDefault);
            }
        }
    }
}
