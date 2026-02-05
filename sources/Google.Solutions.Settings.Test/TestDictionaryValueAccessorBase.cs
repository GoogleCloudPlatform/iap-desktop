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
using System.Collections.Generic;

namespace Google.Solutions.Settings.Test
{
    [TestFixture]
    public abstract class TestDictionaryValueAccessorBase<T>
    {
        private protected static DictionaryValueAccessor<T> CreateAccessor(string valueName)
        {
            return DictionaryValueAccessor.Create<T>(valueName);
        }

        protected abstract T SampleData { get; }

        //---------------------------------------------------------------------
        // TryRead.
        //---------------------------------------------------------------------

        [Test]
        public void TryRead_WhenValueNotSet_ThenTryReadReturnsFalse()
        {
            var dictionary = new Dictionary<string, string>();
            var accessor = CreateAccessor("test");

            Assert.That(accessor.TryRead(dictionary, out var _), Is.False);
        }

        [Test]
        public virtual void TryRead_WhenValueSet_ThenTryReadReturnsTrue()
        {
            var dictionary = new Dictionary<string, string>();
            var accessor = CreateAccessor("test");
            accessor.Write(dictionary, this.SampleData);

            Assert.That(accessor.TryRead(dictionary, out var read), Is.True);
            Assert.That(read, Is.EqualTo(this.SampleData));
        }

        //---------------------------------------------------------------------
        // Delete.
        //---------------------------------------------------------------------

        [Test]
        public void Delete_WhenValueNotSet_ThenDeleteReturns()
        {
            var dictionary = new Dictionary<string, string>();
            var accessor = CreateAccessor("test");

            accessor.Delete(dictionary);
            accessor.Delete(dictionary);
        }

        [Test]
        public void Delete_WhenValueSet_ThenDeleteDeletesValue()
        {
            var dictionary = new Dictionary<string, string>();
            var accessor = CreateAccessor("test");

            accessor.Write(dictionary, this.SampleData);
            accessor.Delete(dictionary);

            Assert.That(accessor.TryRead(dictionary, out var _), Is.False);
        }
    }
}
