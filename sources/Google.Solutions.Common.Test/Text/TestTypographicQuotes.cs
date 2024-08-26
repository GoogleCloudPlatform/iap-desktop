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
    public class TestTypographicQuotes
    {
        [Test]
        public void ToAsciiQuotes_WhenStringContainsNoTypograhicQuotes_ThenToAsciiQuotesReturnsSameString()
        {
            var s = "This isn't \"typographic\" but `plain´ ASCII";
            Assert.AreEqual(s, TypographicQuotes.ToAsciiQuotes(s));
        }

        [Test]
        public void ToAsciiQuotes_WhenStringContainsTypograhicDoubleQuotes_ThenToAsciiQuotesReturnsSanitizedString()
        {
            var s = "These are \u201CEnglish\u201d, \u201eGerman\u201c, and \u00bbFrench\u00ab double quotes";
            Assert.AreEqual(
                "These are \"English\", \"German\", and \"French\" double quotes",
            TypographicQuotes.ToAsciiQuotes(s));
        }

        [Test]
        public void ToAsciiQuotes_WhenStringContainsTypograhicSingleQuotes_ThenToAsciiQuotesReturnsSanitizedString()
        {
            var s = "These are \u2018English\u2019, \u201aGerman\u2018, and \u203aFrench\u2039 single quotes";
            Assert.AreEqual(
                "These are \'English\', \'German\', and \'French\' single quotes",
                TypographicQuotes.ToAsciiQuotes(s));
        }
    }
}
