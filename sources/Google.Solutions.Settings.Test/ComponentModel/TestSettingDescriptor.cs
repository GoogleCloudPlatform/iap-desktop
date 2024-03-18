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

using Google.Solutions.Settings.ComponentModel;
using NUnit.Framework;

namespace Google.Solutions.Settings.Test.ComponentModel
{
    [TestFixture]
    public class TestSettingDescriptor
    {
        [Test]
        public void BrowsableSetting()
        {
            var setting = DictionarySettingsStore
                .Empty()
                .Read<string>("key", "display name", "description", "category", "default");

            var descriptor = new SettingDescriptor(setting);

            Assert.AreEqual("key", descriptor.Name);
            Assert.AreEqual("display name", descriptor.DisplayName);
            Assert.AreEqual("description", descriptor.Description);
            Assert.AreEqual("category", descriptor.Category);

            Assert.IsTrue(descriptor.IsBrowsable);
            Assert.IsFalse(descriptor.IsReadOnly);

            Assert.AreEqual(typeof(ISetting), descriptor.ComponentType);
            Assert.AreEqual(typeof(string), descriptor.PropertyType);
        }

        [Test]
        public void NonBrowsableSetting()
        {
            var setting = DictionarySettingsStore
                .Empty()
                .Read<string>("key", null, null, null, "default");

            var descriptor = new SettingDescriptor(setting);

            Assert.AreEqual("key", descriptor.Name);
            Assert.IsNull(descriptor.DisplayName);
            Assert.IsNull(descriptor.Description);
            Assert.IsNull(descriptor.Category);

            Assert.IsFalse(descriptor.IsBrowsable);
        }

        [Test]
        public void ModifyValue()
        {
            var setting = DictionarySettingsStore
                .Empty()
                .Read<string>("key", "display name", "description", "category", "default");


            var descriptor = new SettingDescriptor(setting);
            
            //
            // Set value.
            //
            descriptor.SetValue(setting, "value-1");
            Assert.AreEqual("value-1", setting.Value);
            Assert.IsTrue(descriptor.ShouldSerializeValue(setting));

            //
            // Get value.
            //
            Assert.AreEqual("value-1", descriptor.GetValue(setting));

            //
            // Reset.
            //
            Assert.IsTrue(descriptor.CanResetValue(setting));
            descriptor.ResetValue(setting);
            Assert.IsTrue(setting.IsDefault);
            Assert.IsFalse(descriptor.ShouldSerializeValue(setting));
        }
    }
}
