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
    public class TestEnumerableExtensions : CommonFixtureBase
    {
        //---------------------------------------------------------------------
        // EnsureNotNull.
        //---------------------------------------------------------------------

        [Test]
        public void EnsureNotNull_WhenEnumIsNull_EnsureNotNullReturnsEmpty()
        {
            IEnumerable<string>? e = null;
            Assert.That(e.EnsureNotNull(), Is.Not.Null);
            Assert.That(e.EnsureNotNull().Count(), Is.EqualTo(0));
        }

        //---------------------------------------------------------------------
        // ContainsAll.
        //---------------------------------------------------------------------

        [Test]
        public void ContainsAll_WhenListsDontIntersect_ContainsAllIsFalse()
        {
            var list = new[] { "a", "b" };
            var lookup = new[] { "c", "d" };

            Assert.That(list.ContainsAll(lookup), Is.False);
        }

        [Test]
        public void ContainsAll_WhenListsPartiallyIntersect_ContainsAllIsFalse()
        {
            var list = new[] { "a", "b" };
            var lookup = new[] { "b", "c" };

            Assert.That(list.ContainsAll(lookup), Is.False);
        }

        [Test]
        public void ContainsAll_WhenListsOverlap_ContainsAllIsTrue()
        {
            var list = new[] { "a", "b", "c", "d" };
            var lookup = new[] { "c", "d" };

            Assert.That(list.ContainsAll(lookup), Is.True);
        }

        //---------------------------------------------------------------------
        // Chunk.
        //---------------------------------------------------------------------

        [Test]
        public void Chunk_WhenListSmallerThanChunk_ThenChunkReturnsSingleList()
        {
            var list = new[] { "a", "b", "c" };
            var chunks = list.Chunk(4);

            Assert.That(chunks.Count(), Is.EqualTo(1));
            Assert.That(chunks.First().Count(), Is.EqualTo(3));
        }

        [Test]
        public void Chunk_WhenListFillsTwoChunks_ThenChunkReturnsTwoLists()
        {
            var list = new[] { "a", "b", "c", "d" };
            var chunks = list.Chunk(2);

            Assert.That(chunks.Count(), Is.EqualTo(2));
            Assert.That(chunks.First(), Is.EqualTo(new[] { "a", "b" }).AsCollection);
            Assert.That(chunks.Skip(1).First(), Is.EqualTo(new[] { "c", "d" }).AsCollection);
        }

        [Test]
        public void Chunk_WhenListLargerThanSingleChunk_ThenChunkReturnsTwoLists()
        {
            var list = new[] { "a", "b", "c" };
            var chunks = list.Chunk(2);

            Assert.That(chunks.Count(), Is.EqualTo(2));
            Assert.That(chunks.First(), Is.EqualTo(new[] { "a", "b" }).AsCollection);
            Assert.That(chunks.Skip(1).First(), Is.EqualTo(new[] { "c" }).AsCollection);
        }

        //---------------------------------------------------------------------
        // ConcatItem.
        //---------------------------------------------------------------------

        [Test]
        public void ConcatItem_WhenEnumEmpty_ThenConcatItemReturnsSingleItem()
        {
            var e = Enumerable.Empty<string>()
                .ConcatItem("test");

            Assert.That(e.Count(), Is.EqualTo(1));
            Assert.That(e.First(), Is.EqualTo("test"));
        }

        [Test]
        public void ConcatItem_WhenEnumNotEmpty_ThenConcatItemAppendsItem()
        {
            var e = new[] { "foo", "bar" }
                .ConcatItem("test");

            Assert.That(e.Count(), Is.EqualTo(3));
            Assert.That(e.First(), Is.EqualTo("foo"));
            Assert.That(e.Last(), Is.EqualTo("test"));
        }
    }
}
