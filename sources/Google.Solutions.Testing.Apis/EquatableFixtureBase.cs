//
// Copyright 2023 Google LLC
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
using System.Reflection;

namespace Google.Solutions.Testing.Apis
{
    public abstract class EquatableFixtureBase<T, TEquatable>
        where T : class, IEquatable<TEquatable>
        where TEquatable : class
    {
        protected abstract T CreateInstance();

        private static bool EqualityOperator(T? lhs, T? rhs)
        {
            return (bool)typeof(T)
                .GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new[] { lhs, rhs });
        }

        private static bool InequalityOperator(T? lhs, T? rhs)
        {
            return (bool)typeof(T)
                .GetMethod("op_Inequality", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new[] { lhs, rhs });
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOtherIsNull_ThenEqualsReturnsFalse()
        {
            var obj = CreateInstance();

            Assert.IsFalse(obj.Equals((object?)null));
            Assert.IsFalse(((IEquatable<TEquatable>)obj!).Equals((TEquatable)null!));

            Assert.IsFalse(EqualityOperator(obj, null));
            Assert.IsTrue(InequalityOperator(obj, null));
        }

        [Test]
        public void WhenOtherIsDifferent_ThenEqualsReturnsFalse()
        {
            var obj = CreateInstance();

            Assert.IsFalse(obj.Equals("test"));
        }

        [Test]
        public void WhenOtherIsOfDifferentType_ThenEqualsReturnsFalse()
        {
            var obj = CreateInstance();

            Assert.IsFalse(obj.Equals("test"));
        }

        [Test]
        public void WhenObjectsAreSame_ThenEqualsReturnsTrue()
        {
            var obj = CreateInstance();
            var other = obj;
            Assert.IsTrue(obj.Equals(other));
            Assert.IsTrue(EqualityOperator(obj, other));
            Assert.IsFalse(InequalityOperator(obj, other));
        }

        [Test]
        public void WhenObjectsAreEquivalent_ThenEqualsReturnsTrue()
        {
            var obj1 = CreateInstance();
            var obj2 = CreateInstance();
            Assert.IsTrue(obj1.Equals(obj2));
            Assert.IsTrue(EqualityOperator(obj1, obj2));
            Assert.IsFalse(InequalityOperator(obj1, obj2));
        }

        //---------------------------------------------------------------------
        // GetHashCode.
        //---------------------------------------------------------------------

        [Test]
        public void WhenObjectsAreEquivalent_ThenHashCodeEquals()
        {
            var obj1 = CreateInstance();
            var obj2 = CreateInstance();
            Assert.AreEqual(obj1.GetHashCode(), obj2.GetHashCode());
        }
    }
}
