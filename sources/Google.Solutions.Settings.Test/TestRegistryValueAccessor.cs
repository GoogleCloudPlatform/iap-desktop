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

using Google.Solutions.Common.Security;
using Microsoft.Win32;
using NUnit.Framework;
using System.Security;

namespace Google.Solutions.Settings.Test
{
    public static class TestRegistryValueAccessor
    {
        [TestFixture]
        public class TestBoolRegistryValueAccessor : TestRegistryValueAccessorBase<bool>
        {
            protected override bool SampleData => true;

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, "some data", RegistryValueKind.String);
            }
        }

        [TestFixture]
        public class TestDwordRegistryValueAccessor : TestRegistryValueAccessorBase<int>
        {
            protected override int SampleData => int.MinValue;

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, "some data", RegistryValueKind.String);
            }
        }

        [TestFixture]
        public class TestQwordRegistryValueAccessor : TestRegistryValueAccessorBase<long>
        {
            protected override long SampleData => long.MinValue;

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, "some data", RegistryValueKind.String);
            }
        }

        [TestFixture]
        public class TestStringRegistryValueAccessor : TestRegistryValueAccessorBase<string>
        {
            protected override string SampleData => "some text";

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, 1, RegistryValueKind.DWord);
            }
        }

        [TestFixture]
        public class TestSecureStringRegistryValueAccessor : TestRegistryValueAccessorBase<SecureString>
        {
            protected override SecureString SampleData
                => SecureStringExtensions.FromClearText("some text");

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, 1, RegistryValueKind.DWord);
            }

            [Test]
            public override void WhenValueSet_ThenTryReadReturnsTrue()
            {
                using (var key = CreateKey())
                {
                    var accessor = CreateAccessor("test");
                    accessor.Write(key, this.SampleData);

                    Assert.IsTrue(accessor.TryRead(key, out var read));
                    Assert.AreEqual(this.SampleData.AsClearText(), read.AsClearText());
                }
            }
        }

        [TestFixture]
        public class TestEnumRegistryValueAccessor : TestRegistryValueAccessorBase<TestEnumRegistryValueAccessor.Drink>
        {
            public enum Drink
            {
                Coffee,
                Tea,
                Water
            }

            protected override Drink SampleData => Drink.Water;

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, "some data", RegistryValueKind.String);
            }
        }
    }
}
