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

using Google.Solutions.Common.Text;
using NUnit.Framework;

namespace Google.Solutions.Common.Test.Text
{
    [TestFixture]
    public class TestStringExtensions
    {
        //---------------------------------------------------------------------
        // IndexOf.
        //---------------------------------------------------------------------

        [Test]
        public void IndexOf_WhenTextContainsChar_ThenIndexOfReturnsFirstIndex()
        {
            var text = "a a a";
            Assert.That(text.IndexOf(c => char.IsWhiteSpace(c)), Is.EqualTo(1));
        }

        [Test]
        public void IndexOf_WhenTextDoesNotContainChar_ThenIndexOfReturnsMinusOne()
        {
            var text = "a a a";
            Assert.That(text.IndexOf(c => char.IsDigit(c)), Is.EqualTo(-1));
        }

        //---------------------------------------------------------------------
        // LastIndexOf.
        //---------------------------------------------------------------------

        [Test]
        public void LastIndexOf_WhenTextContainsChar_ThenLastIndexOfReturnsLastIndex()
        {
            var text = "a a a";
            Assert.That(text.LastIndexOf(c => char.IsWhiteSpace(c)), Is.EqualTo(3));
        }

        [Test]
        public void LastIndexOf_WhenTextDoesNotContainChar_ThenLastIndexOfReturnsMinusOne()
        {
            var text = "a a a";
            Assert.That(text.LastIndexOf(c => char.IsDigit(c)), Is.EqualTo(-1));
        }

        //---------------------------------------------------------------------
        // Truncate.
        //---------------------------------------------------------------------

        [Test]
        public void Truncate_WhenStringShortEnough_ThenTruncateReturnsStringVerbatim()
        {
            Assert.That("".Truncate(3), Is.EqualTo(""));
            Assert.That("foo".Truncate(3), Is.EqualTo("foo"));
        }

        [Test]
        public void Truncate_WhenStringTooLong_ThenTruncateReturnsStringWithEllipsis()
        {
            Assert.That("abcd".Truncate(3), Is.EqualTo("abc..."));
        }

        //---------------------------------------------------------------------
        // NullIfEmpty.
        //---------------------------------------------------------------------

        [Test]
        public void NullIfEmpty_WhenStringIsNull_ThenNullIfEmptyReturnsNull()
        {
            string? s = null;
            Assert.That(s.NullIfEmpty(), Is.Null);
        }

        [Test]
        public void NullIfEmpty_WhenStringIsEmpty_ThenNullIfEmptyReturnsNull()
        {
            var s = string.Empty;
            Assert.That(s.NullIfEmpty(), Is.Null);
        }

        [Test]
        public void NullIfEmpty_WhenStringIsWhitespace_ThenNullIfEmptyReturnsString()
        {
            var s = " ";
            Assert.That(s.NullIfEmpty(), Is.EqualTo(" "));
        }

        //---------------------------------------------------------------------
        // NullIfEmptyOrWhitespace.
        //---------------------------------------------------------------------

        [Test]
        public void NullIfEmptyOrWhitespace_WhenStringIsNull_ThenNullIfEmptyOrWhitespaceReturnsNull()
        {
            string? s = null;
            Assert.That(s.NullIfEmptyOrWhitespace(), Is.Null);
        }

        [Test]
        public void NullIfEmptyOrWhitespace_WhenStringIsEmpty_ThenNullIfEmptyOrWhitespaceReturnsNull()
        {
            var s = string.Empty;
            Assert.That(s.NullIfEmptyOrWhitespace(), Is.Null);
        }

        [Test]
        public void NullIfEmptyOrWhitespace_WhenStringIsWhitespace_ThenNullIfEmptyOrWhitespaceReturnsNull()
        {
            var s = " ";
            Assert.That(s.NullIfEmptyOrWhitespace(), Is.Null);
        }

        [Test]
        public void NullIfEmptyOrWhitespace_WhenStringIsNotWhitespace_ThenNullIfEmptyOrWhitespaceReturnsString()
        {
            var s = " a ";
            Assert.That(s.NullIfEmptyOrWhitespace(), Is.EqualTo(" a "));
        }
    }
}
