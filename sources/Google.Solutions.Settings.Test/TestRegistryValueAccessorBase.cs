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
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.Settings.Test
{
    public abstract class TestRegistryValueAccessorBase<T>
    {
        private protected static RegistryValueAccessor<T> CreateAccessor(string valueName)
        {
            return RegistryValueAccessor.Create<T>(valueName);
        }

        protected abstract T SampleData { get; }

        protected abstract void WriteIncompatibleValue(RegistryKey key, string name);

        //---------------------------------------------------------------------
        // TryRead.
        //---------------------------------------------------------------------

        [Test]
        public void TryRead_WhenValueNotSet_ThenTryReadReturnsFalse()
        {
            using (var key = RegistryKeyPath.ForCurrentTest().CreateKey())
            {
                var accessor = CreateAccessor("test");

                Assert.IsFalse(accessor.TryRead(key, out var _));
            }
        }

        [Test]
        public virtual void TryRead_WhenValueSet_ThenTryReadReturnsTrue()
        {
            using (var key = RegistryKeyPath.ForCurrentTest().CreateKey())
            {
                var accessor = CreateAccessor("test");
                accessor.Write(key, this.SampleData);

                Assert.IsTrue(accessor.TryRead(key, out var read));
                Assert.AreEqual(this.SampleData, read);
            }
        }

        [Test]
        public void TryRead_WhenValueHasWrongKind_ThenTryReadThrowsException()
        {
            using (var key = RegistryKeyPath.ForCurrentTest().CreateKey())
            {
                var accessor = CreateAccessor("test");
                WriteIncompatibleValue(key, accessor.Name);

                Assert.Throws<InvalidCastException>(
                    () => accessor.TryRead(key, out var read));
            }
        }

        //---------------------------------------------------------------------
        // Delete.
        //---------------------------------------------------------------------

        [Test]
        public void Delete_WhenValueNotSet_ThenDeleteReturns()
        {
            using (var key = RegistryKeyPath.ForCurrentTest().CreateKey())
            {
                var accessor = CreateAccessor("test");
                accessor.Delete(key);
                accessor.Delete(key);
            }
        }

        [Test]
        public void Delete_WhenValueSet_ThenDeleteDeletesValue()
        {
            using (var key = RegistryKeyPath.ForCurrentTest().CreateKey())
            {
                var accessor = CreateAccessor("test");
                accessor.Write(key, this.SampleData);
                accessor.Delete(key);

                Assert.IsFalse(accessor.TryRead(key, out var _));
            }
        }
    }
}
