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

using Google.Solutions.IapDesktop.Application.Util;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Util
{
    [TestFixture]
    public class TestStringExtensions
    {
        [Test]
        public void WhenTextContainsChar_ThenIndexOfReturnsFirstIndex()
        {
            var text = "a a a";
            Assert.AreEqual(1, text.IndexOf(c => char.IsWhiteSpace(c)));
        }

        [Test]
        public void WhenTextDoesNotContainChar_ThenIndexOfReturnsMinusOne()
        {
            var text = "a a a";
            Assert.AreEqual(-1, text.IndexOf(c => char.IsDigit(c)));
        }

        [Test]
        public void WhenTextContainsChar_ThenLastIndexOfReturnsLastIndex()
        {
            var text = "a a a";
            Assert.AreEqual(3, text.LastIndexOf(c => char.IsWhiteSpace(c)));
        }

        [Test]
        public void WhenTextDoesNotContainChar_ThenLastIndexOfReturnsMinusOne()
        {
            var text = "a a a";
            Assert.AreEqual(-1, text.LastIndexOf(c => char.IsDigit(c)));
        }

        [Test]
        public void WhenStringShortEnough_ThenTruncateReturnsStringVerbatim()
        {
            Assert.AreEqual("", "".Truncate(3));
            Assert.AreEqual("foo", "foo".Truncate(3));
        }

        [Test]
        public void WhenStringTooLong_ThenTruncateReturnsStringWithEllipsis()
        {
            Assert.AreEqual("abc...", "abcd".Truncate(3));
        }
    }
}
