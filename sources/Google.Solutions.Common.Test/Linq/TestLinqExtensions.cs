//
// Copyright 2019 Google LLC
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

using Google.Solutions.Common.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Common.Test.Linq
{
    [TestFixture]
    public class TestLinqExtensions : CommonFixtureBase
    {
        //---------------------------------------------------------------------
        // EnsureNotNull.
        //---------------------------------------------------------------------

        [Test]
        public void EnsureNotNull_WhenEnumIsNull_EnsureNotNullReturnsEmpty()
        {
            IEnumerable<string>? e = null;
            Assert.IsNotNull(e.EnsureNotNull());
            Assert.AreEqual(0, e.EnsureNotNull().Count());
        }

        //---------------------------------------------------------------------
        // ContainsAll.
        //---------------------------------------------------------------------

        [Test]
        public void ContainsAll_WhenListsDontIntersect_ContainsAllIsFalse()
        {
            var list = new[] { "a", "b" };
            var lookup = new[] { "c", "d" };

            Assert.IsFalse(list.ContainsAll(lookup));
        }

        [Test]
        public void ContainsAll_WhenListsPartiallyIntersect_ContainsAllIsFalse()
        {
            var list = new[] { "a", "b" };
            var lookup = new[] { "b", "c" };

            Assert.IsFalse(list.ContainsAll(lookup));
        }

        [Test]
        public void ContainsAll_WhenListsOverlap_ContainsAllIsTrue()
        {
            var list = new[] { "a", "b", "c", "d" };
            var lookup = new[] { "c", "d" };

            Assert.IsTrue(list.ContainsAll(lookup));
        }

        //---------------------------------------------------------------------
        // Chunk.
        //---------------------------------------------------------------------

        [Test]
        public void Chunk_WhenListSmallerThanChunk_ThenChunkReturnsSingleList()
        {
            var list = new[] { "a", "b", "c" };
            var chunks = list.Chunk(4);

            Assert.AreEqual(1, chunks.Count());
            Assert.AreEqual(3, chunks.First().Count());
        }

        [Test]
        public void Chunk_WhenListFillsTwoChunks_ThenChunkReturnsTwoLists()
        {
            var list = new[] { "a", "b", "c", "d" };
            var chunks = list.Chunk(2);

            Assert.AreEqual(2, chunks.Count());
            CollectionAssert.AreEqual(new[] { "a", "b" }, chunks.First());
            CollectionAssert.AreEqual(new[] { "c", "d" }, chunks.Skip(1).First());
        }

        [Test]
        public void Chunk_WhenListLargerThanSingleChunk_ThenChunkReturnsTwoLists()
        {
            var list = new[] { "a", "b", "c" };
            var chunks = list.Chunk(2);

            Assert.AreEqual(2, chunks.Count());
            CollectionAssert.AreEqual(new[] { "a", "b" }, chunks.First());
            CollectionAssert.AreEqual(new[] { "c" }, chunks.Skip(1).First());
        }

        //---------------------------------------------------------------------
        // ConcatItem.
        //---------------------------------------------------------------------

        [Test]
        public void ConcatItem_WhenEnumEmpty_ThenConcatItemReturnsSingleItem()
        {
            var e = Enumerable.Empty<string>()
                .ConcatItem("test");

            Assert.AreEqual(1, e.Count());
            Assert.AreEqual("test", e.First());
        }

        [Test]
        public void ConcatItem_WhenEnumNotEmpty_ThenConcatItemAppendsItem()
        {
            var e = new[] { "foo", "bar" }
                .ConcatItem("test");

            Assert.AreEqual(3, e.Count());
            Assert.AreEqual("foo", e.First());
            Assert.AreEqual("test", e.Last());
        }
    }
}
