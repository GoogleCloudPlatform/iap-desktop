﻿//
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

using Google.Solutions.Mvvm.Cache;
using NUnit.Framework;
using System;

namespace Google.Solutions.Mvvm.Test.Cache
{
    [TestFixture]
    public class TestLeastRecentlyUsedCache
    {
        [Test]
        public void WhenCapacityTooSmall_ThenArgumentExceptionIsThrown()
        {
            Assert.Throws<ArgumentException>(() => new LeastRecentlyUsedCache<string, string>(0));
        }

        [Test]
        public void WhenLookupNonexistingItem_ThenNullIsReturned()
        {
            var cache = new LeastRecentlyUsedCache<string, string>(2);
            Assert.IsNull(cache.Lookup("key"));
        }

        [Test]
        public void WhenLookupCachedItem_ThenItemIsReturned()
        {
            var cache = new LeastRecentlyUsedCache<string, string>(2);
            cache.Add("one", "ONE");
            cache.Add("two", "TWO");

            Assert.AreEqual("ONE", cache.Lookup("one"));
            Assert.AreEqual("TWO", cache.Lookup("two"));
        }

        [Test]
        public void WhenAddingItemsBeyondCapacity_ThenLeastReentlyUsedItemIsPurged()
        {
            var cache = new LeastRecentlyUsedCache<string, string>(2);
            cache.Add("one", "ONE");
            cache.Add("two", "TWO");
            cache.Add("three", "THREE");

            Assert.IsNull(cache.Lookup("one"));
            Assert.AreEqual("TWO", cache.Lookup("two"));
            Assert.AreEqual("THREE", cache.Lookup("three"));
        }

        [Test]
        public void WhenLookingUpItem_ThenItemIsMarkedAsUsed()
        {
            var cache = new LeastRecentlyUsedCache<string, string>(2);
            cache.Add("one", "ONE");
            cache.Add("two", "TWO");
            cache.Lookup("one");
            cache.Add("three", "THREE");

            Assert.IsNull(cache.Lookup("two"));
            Assert.AreEqual("ONE", cache.Lookup("one"));
            Assert.AreEqual("THREE", cache.Lookup("three"));
        }

        [Test]
        public void WhenItemAddedTwice_ThenSecondCallIsIgnored()
        {
            var cache = new LeastRecentlyUsedCache<string, string>(2);
            cache.Add("one", "ONE");
            cache.Add("one", "ONE");
            cache.Add("two", "TWO");

            Assert.AreEqual("ONE", cache.Lookup("one"));
            Assert.AreEqual("TWO", cache.Lookup("two"));
        }

        [Test]
        public void WhenItemExists_ThenRemoveSucceeds()
        {
            var cache = new LeastRecentlyUsedCache<string, string>(2);
            cache.Add("one", "ONE");
            cache.Remove("one");
            cache.Remove("doesnotexist");

            Assert.IsNull(cache.Lookup("one"));
        }
    }
}
