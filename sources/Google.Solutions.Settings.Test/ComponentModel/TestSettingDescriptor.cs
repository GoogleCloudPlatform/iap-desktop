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
using System;

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

            Assert.That(descriptor.Name, Is.EqualTo("key"));
            Assert.That(descriptor.DisplayName, Is.EqualTo("display name"));
            Assert.That(descriptor.Description, Is.EqualTo("description"));
            Assert.That(descriptor.Category, Is.EqualTo("category"));

            Assert.That(descriptor.IsBrowsable, Is.True);
            Assert.That(descriptor.IsReadOnly, Is.False);

            Assert.That(descriptor.ComponentType, Is.EqualTo(typeof(ISetting)));
            Assert.That(descriptor.PropertyType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void NonBrowsableSetting()
        {
            var setting = DictionarySettingsStore
                .Empty()
                .Read<string>("key", null, null, null, "default");

            var descriptor = new SettingDescriptor(setting);

            Assert.That(descriptor.Name, Is.EqualTo("key"));
            Assert.That(descriptor.DisplayName, Is.Null);
            Assert.That(descriptor.Description, Is.Null);
            Assert.That(descriptor.Category, Is.Null);

            Assert.That(descriptor.IsBrowsable, Is.False);
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
            Assert.That(setting.Value, Is.EqualTo("value-1"));
            Assert.That(descriptor.ShouldSerializeValue(setting), Is.True);

            //
            // Get value.
            //
            Assert.That(descriptor.GetValue(setting), Is.EqualTo("value-1"));

            //
            // Reset.
            //
            Assert.That(descriptor.CanResetValue(setting), Is.True);
            descriptor.ResetValue(setting);
            Assert.That(setting.IsDefault, Is.True);
            Assert.That(descriptor.ShouldSerializeValue(setting), Is.False);
        }

        //--------------------------------------------------------------------
        // Converter.
        //--------------------------------------------------------------------

        [Test]
        public void Converter_WhenEnum()
        {
            var setting = DictionarySettingsStore
                .Empty()
                .Read<StringComparison>("key", null, null, null, StringComparison.Ordinal);

            var descriptor = new SettingDescriptor(setting);
            Assert.That(descriptor.Converter, Is.InstanceOf<EnumDisplayNameConverter>());
        }

        [Test]
        public void Converter_WhenString()
        {
            var setting = DictionarySettingsStore
                .Empty()
                .Read<string>("key", "display name", "description", "category", "default");

            var descriptor = new SettingDescriptor(setting);
            Assert.That(descriptor.Converter, Is.Not.InstanceOf<EnumDisplayNameConverter>());
        }
    }
}
