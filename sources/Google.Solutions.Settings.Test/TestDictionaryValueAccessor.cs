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
using System.Security;

namespace Google.Solutions.Settings.Test
{
    public static class TestDictionaryValueAccessor
    {
        [TestFixture]
        public class TestBoolValueAccessor : TestDictionaryValueAccessorBase<bool>
        {
            protected override bool SampleData => true;
        }

        [TestFixture]
        public class TestIntValueAccessor : TestDictionaryValueAccessorBase<int>
        {
            protected override int SampleData => -1;
        }

        [TestFixture]
        public class TestLongValueAccessor : TestDictionaryValueAccessorBase<long>
        {
            protected override long SampleData => -1;
        }

        [TestFixture]
        public class TestStringValueAccessor : TestDictionaryValueAccessorBase<string>
        {
            protected override string SampleData => "some string";
        }

        [TestFixture]
        public class TestSecureStringValueAccessor : TestDictionaryValueAccessorBase<SecureString>
        {
            protected override SecureString SampleData => null;

            [Test]
            public override void TryRead_WhenValueSet_ThenTryReadReturnsTrue()
            {
                var dictionary = new Dictionary<string, string>();
                var accessor = CreateAccessor("test");
                accessor.Write(dictionary, this.SampleData);

                Assert.That(accessor.TryRead(dictionary, out var _), Is.False);
            }
        }

        [TestFixture]
        public class TestEnumValueAccessor : TestDictionaryValueAccessorBase<ConsoleColor>
        {
            protected override ConsoleColor SampleData => ConsoleColor.Magenta;

            [Test]
            public void IsValid()
            {
                Assert.IsTrue(CreateAccessor("test").IsValid(ConsoleColor.Blue));
                Assert.That(CreateAccessor("test").IsValid((ConsoleColor)(-1)), Is.False);
            }
        }
    }
}
