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

using Google.Solutions.Testing.Apis.Platform;
using NUnit.Framework;
using System;

namespace Google.Solutions.Settings.Test
{
    [TestFixture]
    public class TestRegistrySettingsStore
    {
        //---------------------------------------------------------------------
        // Read.
        //---------------------------------------------------------------------

        [Test]
        public void Read_WhenTypeUnsupported_ThenReadThrowsException()
        {
            using (var key = new RegistrySettingsStore(
                RegistryKeyPath.ForCurrentTest().CreateKey()))
            {
                Assert.Throws<ArgumentException>(
                    () => key.Read<uint>("test", "test", null, null, 0));
            }
        }

        //---------------------------------------------------------------------
        // IsSpecified.
        //---------------------------------------------------------------------

        [Test]
        public void IsSpecified_WhenValueExists_ThenIsSpecifiedIsTrue()
        {
            using (var key = new RegistrySettingsStore(
                RegistryKeyPath.ForCurrentTest().CreateKey()))
            {
                //
                // Read non-existing value.
                //
                var setting = key.Read("test", "test", null, null, 0);
                Assert.IsFalse(setting.IsSpecified);

                //
                // Write and read again.
                //
                setting.Value = 1;
                key.Write(setting);

                setting = key.Read("test", "test", null, null, 0);
                Assert.IsTrue(setting.IsSpecified);
            }
        }

        //---------------------------------------------------------------------
        // IsDefault.
        //---------------------------------------------------------------------

        [Test]
        public void IsDefault_WhenValueExists_ThenValueIsNotDefault()
        {
            using (var key = new RegistrySettingsStore(
                RegistryKeyPath.ForCurrentTest().CreateKey()))
            {
                //
                // Read non-existing value.
                //
                var defaultValue = -1;
                var setting = key.Read("test", "test", null, null, defaultValue);
                Assert.IsTrue(setting.IsDefault);
                Assert.That(setting.Value, Is.EqualTo(defaultValue));

                //
                // Write and read again.
                //
                setting.Value = 1;
                key.Write(setting);

                setting = key.Read("test", "test", null, null, 0);
                Assert.IsFalse(setting.IsDefault);
                Assert.That(setting.Value, Is.EqualTo(1));
            }
        }

        //---------------------------------------------------------------------
        // SetValue.
        //---------------------------------------------------------------------

        [Test]
        public void SetValue_WhenCustomValidationFails_ThenSetValueThrowsException()
        {
            using (var key = new RegistrySettingsStore(
                RegistryKeyPath.ForCurrentTest().CreateKey()))
            {
                //
                // Read non-existing value.
                //
                var setting = key.Read(
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
        public void Write_WhenValueIsDefault_ThenWriteDeletesValue()
        {
            using (var key = new RegistrySettingsStore(
                RegistryKeyPath.ForCurrentTest().CreateKey()))
            {
                var setting = key.Read("test", "test", null, null, 0);

                //
                // Write non-default value.
                //
                setting.Value = 1;
                key.Write(setting);

                //
                // Write default value.
                //
                setting = key.Read("test", "test", null, null, 0);
                Assert.IsFalse(setting.IsDefault);
                setting.Reset();
                key.Write(setting);

                //
                // Status back to "not specified".
                //
                setting = key.Read("test", "test", null, null, 0);
                Assert.IsFalse(setting.IsSpecified);
                Assert.IsTrue(setting.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // Clear.
        //---------------------------------------------------------------------

        [Test]
        public void Clear_RemovesAllRegistryValues()
        {
            using (var key = RegistryKeyPath.ForCurrentTest().CreateKey())
            {
                key.SetValue("foo", 1);
                key.SetValue("bar", 1);

                new RegistrySettingsStore(key).Clear();

                Assert.IsNull(key.GetValue("foo"));
                Assert.IsNull(key.GetValue("bar"));
            }
        }
    }
}
