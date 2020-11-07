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
    }
}
