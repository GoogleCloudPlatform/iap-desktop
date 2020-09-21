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

using Google.Solutions.IapDesktop.Application.Settings;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.Settings
{
    [TestFixture]
    public class TestSettingsCollectionExtensions
    {
        private class TestCollection : ISettingsCollection
        {
            public IEnumerable<ISetting> Settings { get; }

            public TestCollection(IEnumerable<ISetting> settings)
            {
                this.Settings = settings;
            }
        }

        private class TestSetting : SettingBase<string>
        {
            public TestSetting(
                string key,
                string initialValue,
                string defaultValue)
                : base(key, null, null, null, initialValue, defaultValue)
            {
            }

            protected override SettingBase<string> CreateNew(string value, string defaultValue)
            {
                return new TestSetting(
                    this.Key,
                    value,
                    defaultValue);
            }

            protected override bool IsValid(string value) => true;
            protected override string Parse(string value) => value;
        }

        [Test]
        public void WhenOverlayHasFewerSettings_ThenResultContainsUnion()
        {
            var parent = new TestCollection(new[]
            {
                new TestSetting("setting-1", "red", null)
            });

            var child = new TestCollection(new[]
            {
                new TestSetting("setting-1", "blue", null),
                new TestSetting("setting-2", "green", null)
            });

            var result = parent.OverlayBy(child);

            Assert.AreEqual(2, result.Settings.Count());
            Assert.IsNotNull(result.Settings.First(s => s.Key == "setting-1"));
            Assert.IsNotNull(result.Settings.First(s => s.Key == "setting-2"));
        }

        [Test]
        public void WhenOverlayHasSameSettings_ThenResultContainsOverlaidSettings()
        {
            var parent = new TestCollection(new[]
            {
                new TestSetting("setting-1", "red", null)
            });

            var child = new TestCollection(new[]
            {
                new TestSetting("setting-1", "green", null)
            });

            var result = parent.OverlayBy(child);

            Assert.AreEqual(1, result.Settings.Count());
            var setting = result.Settings.First(s => s.Key == "setting-1");
            Assert.AreEqual("green", setting.Value);
        }

        [Test]
        public void WhenOverlayHasMoreSettings_ThenResultContainsUnion()
        {
            var parent = new TestCollection(new[]
            {
                new TestSetting("setting-1", "red", null),
                new TestSetting("setting-2", "blue", null)
            });

            var child = new TestCollection(new[]
            {
                new TestSetting("setting-2", "green", null)
            });

            var result = parent.OverlayBy(child);

            Assert.AreEqual(2, result.Settings.Count());
            Assert.IsNotNull(result.Settings.First(s => s.Key == "setting-1"));
            Assert.IsNotNull(result.Settings.First(s => s.Key == "setting-2"));
        }
    }
}
