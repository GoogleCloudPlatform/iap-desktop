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

using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Google.Solutions.Settings.Test
{
    [TestFixture]
    public class TestMergedSettingsStore
    {

        //---------------------------------------------------------------------
        // Read - Policy.
        //---------------------------------------------------------------------

        [Test]
        public void Read_PolicyKeyValueEmpty_ThenLesserKeyValueEmpty()
        {
            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    DictionarySettingsStore.Empty(),
                    DictionarySettingsStore.Empty()
                },
                MergedSettingsStore.MergeBehavior.Policy);

            var defaultValue = 1;
            var setting = mergedKey.Read("test", "test", null, null, defaultValue);

            Assert.That(setting.Value, Is.EqualTo(defaultValue));
            Assert.That(setting.DefaultValue, Is.EqualTo(defaultValue));
            Assert.IsTrue(setting.IsDefault);
            Assert.IsFalse(setting.IsSpecified);
            Assert.IsFalse(setting.IsReadOnly);
        }

        [Test]
        public void PRead_olicyKeyValueEmpty_ThenLesserKeyValueSet()
        {
            var lesserStore = new DictionarySettingsStore(new Dictionary<string, string>
            {
                { "test", "2" }
            });
            var policyStore = DictionarySettingsStore.Empty();

            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    lesserStore,
                    policyStore
                },
                MergedSettingsStore.MergeBehavior.Policy);

            var defaultValue = 1;
            var setting = mergedKey.Read("test", "test", null, null, defaultValue);

            Assert.That(setting.Value, Is.EqualTo(2));
            Assert.That(setting.DefaultValue, Is.EqualTo(defaultValue));
            Assert.IsFalse(setting.IsDefault);
            Assert.IsTrue(setting.IsSpecified);
            Assert.IsFalse(setting.IsReadOnly);
        }

        [Test]
        public void Read_PolicyKeyValueSet_ThenLesserKeyValueEmpty()
        {
            var lesserStore = DictionarySettingsStore.Empty();
            var policyStore = new DictionarySettingsStore(new Dictionary<string, string>
            {
                { "test", "3" }
            });

            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    lesserStore,
                    policyStore
                },
                MergedSettingsStore.MergeBehavior.Policy);

            var defaultValue = 1;
            var setting = mergedKey.Read("test", "test", null, null, defaultValue);

            Assert.That(setting.Value, Is.EqualTo(3));
            Assert.That(setting.DefaultValue, Is.EqualTo(defaultValue));
            Assert.IsFalse(setting.IsDefault);
            Assert.IsTrue(setting.IsSpecified);
            Assert.IsTrue(setting.IsReadOnly);
        }

        [Test]
        public void Read_PolicyKeyValueSet_ThenLesserKeyValueSet()
        {
            var lesserStore = new DictionarySettingsStore(new Dictionary<string, string>
            {
                { "test", "2" }
            }); ;
            var policyStore1 = new DictionarySettingsStore(new Dictionary<string, string>
            {
                { "test", "3" }
            });
            var policyStore2 = new DictionarySettingsStore(new Dictionary<string, string>
            {
                { "test", "4" }
            });

            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    lesserStore,
                    policyStore1,
                    policyStore2,
                },
                MergedSettingsStore.MergeBehavior.Policy);

            var defaultValue = 1;
            var setting = mergedKey.Read("test", "test", null, null, defaultValue);

            Assert.That(setting.Value, Is.EqualTo(4));
            Assert.That(setting.DefaultValue, Is.EqualTo(defaultValue));
            Assert.IsFalse(setting.IsDefault);
            Assert.IsTrue(setting.IsSpecified);
            Assert.IsTrue(setting.IsReadOnly);
        }

        //---------------------------------------------------------------------
        // Read - Overlay.
        //---------------------------------------------------------------------

        [Test]
        public void Read_OverlayKeyValueEmpty_ThenLesserKeyValueEmpty()
        {
            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    DictionarySettingsStore.Empty(),
                    DictionarySettingsStore.Empty()
                },
                MergedSettingsStore.MergeBehavior.Overlay);

            var defaultValue = 1;
            var setting = mergedKey.Read("test", "test", null, null, defaultValue);

            Assert.That(setting.Value, Is.EqualTo(defaultValue));
            Assert.That(setting.DefaultValue, Is.EqualTo(defaultValue));
            Assert.IsTrue(setting.IsDefault);
            Assert.IsFalse(setting.IsSpecified);
            Assert.IsFalse(setting.IsReadOnly);
        }

        [Test]
        public void Read_OverlayKeyValueEmpty_ThenLesserKeyValueSet()
        {
            var lesserStore = new DictionarySettingsStore(new Dictionary<string, string>
            {
                { "test", "2" }
            });
            var overlayStore = DictionarySettingsStore.Empty();

            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    lesserStore,
                    overlayStore
                },
                MergedSettingsStore.MergeBehavior.Overlay);

            var defaultValue = 1;
            var setting = mergedKey.Read("test", "test", null, null, defaultValue);

            Assert.That(setting.Value, Is.EqualTo(2));
            Assert.That(setting.DefaultValue, Is.EqualTo(2));   // New default
            Assert.IsTrue(setting.IsDefault);
            Assert.IsFalse(setting.IsSpecified);
            Assert.IsFalse(setting.IsReadOnly);
        }

        [Test]
        public void Read_OverlayKeyValueSet_ThenLesserKeyValueEmpty()
        {
            var lesserStore = DictionarySettingsStore.Empty();
            var overlayStore = new DictionarySettingsStore(new Dictionary<string, string>
            {
                { "test", "3" }
            });

            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    lesserStore,
                    overlayStore
                },
                MergedSettingsStore.MergeBehavior.Overlay);

            var defaultValue = 1;
            var setting = mergedKey.Read("test", "test", null, null, defaultValue);

            Assert.That(setting.Value, Is.EqualTo(3));
            Assert.That(setting.DefaultValue, Is.EqualTo(defaultValue));
            Assert.IsFalse(setting.IsDefault);
            Assert.IsTrue(setting.IsSpecified);
            Assert.IsFalse(setting.IsReadOnly);
        }

        [Test]
        public void Read_OverlayKeyValueSet_ThenLesserKeyValueSet()
        {
            var lesserStore = new DictionarySettingsStore(new Dictionary<string, string>
            {
                { "test", "2" }
            });
            var overlayStore1 = new DictionarySettingsStore(new Dictionary<string, string>
            {
                { "test", "3" }
            });
            var overlayStore2 = new DictionarySettingsStore(new Dictionary<string, string>
            {
                { "test", "4" }
            });

            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    lesserStore,
                    overlayStore1,
                    overlayStore2
                },
                MergedSettingsStore.MergeBehavior.Overlay);

            var defaultValue = 1;
            var setting = mergedKey.Read("test", "test", null, null, defaultValue);

            Assert.That(setting.Value, Is.EqualTo(4));
            Assert.That(setting.DefaultValue, Is.EqualTo(3));
            Assert.IsFalse(setting.IsDefault);
            Assert.IsTrue(setting.IsSpecified);
            Assert.IsFalse(setting.IsReadOnly);
        }

        //---------------------------------------------------------------------
        // Write.
        //---------------------------------------------------------------------

        [Test]
        public void Write_WhenBehaviorIsPolicy_ThenWriteGoesToLesserStore()
        {
            var policyStore = DictionarySettingsStore.Empty();
            var lesserStore = DictionarySettingsStore.Empty();

            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    lesserStore,
                    DictionarySettingsStore.Empty(),
                    DictionarySettingsStore.Empty(),
                    policyStore,
                },
                MergedSettingsStore.MergeBehavior.Policy);

            var setting = mergedKey.Read("test", "test", null, null, 0);
            setting.Value = 1;
            mergedKey.Write(setting);

            Assert.IsFalse(policyStore.Values.ContainsKey("test"));
            Assert.IsTrue(lesserStore.Values.ContainsKey("test"));
        }

        [Test]
        public void Write_WhenBehaviorIsOverlay_ThenWriteGoesToOverlayStore()
        {
            var overlayStore = DictionarySettingsStore.Empty();
            var lesserStore = DictionarySettingsStore.Empty();

            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    lesserStore,
                    overlayStore,
                },
                MergedSettingsStore.MergeBehavior.Overlay);

            var setting = mergedKey.Read("test", "test", null, null, 0);
            setting.Value = 1;
            mergedKey.Write(setting);

            Assert.IsTrue(overlayStore.Values.ContainsKey("test"));
            Assert.IsFalse(lesserStore.Values.ContainsKey("test"));
        }

        //---------------------------------------------------------------------
        // Clear.
        //---------------------------------------------------------------------

        [Test]
        public void Clear_ThrowsException()
        {
            var mergedKey = new MergedSettingsStore(
                new[]
                {
                    DictionarySettingsStore.Empty(),
                    DictionarySettingsStore.Empty()
                },
                MergedSettingsStore.MergeBehavior.Policy);

            Assert.Throws<InvalidOperationException>(
                () => mergedKey.Clear());
        }
    }
}
