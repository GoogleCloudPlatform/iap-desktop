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

using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.Adapters
{
    [TestFixture]
    public class TestDateRange : FixtureBase
    {
        [Test]
        public void WhenStartEqualsEnd_ThenRangeContainsSingleRange()
        {
            var range = DateRange.DayRange(
                new DateTime(2020, 1, 1, 0, 0, 0),
                new DateTime(2020, 1, 1, 2, 3, 4),
                1);

            Assert.AreEqual(1, range.Count());
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 0, 0, 0),
                range.First());
        }

        [Test]
        public void WhenEndBeforeStart_ThenRangeRhrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => DateRange.DayRange(
                new DateTime(2020, 1, 2, 0, 0, 0),
                new DateTime(2020, 1, 1, 0, 0, 0),
                1).ToList());
        }

        [Test]
        public void WhenStartLessThanEnd_ThenRangeContainsAllDaysIncludingBounds()
        {
            var range = DateRange.DayRange(
                new DateTime(2020, 1, 1, 0, 0, 0),
                new DateTime(2020, 1, 3, 0, 0, 0),
                1).ToList();

            Assert.AreEqual(3, range.Count());
            Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0), range[0]);
            Assert.AreEqual(new DateTime(2020, 1, 2, 0, 0, 0), range[1]);
            Assert.AreEqual(new DateTime(2020, 1, 3, 0, 0, 0), range[2]);
        }

        [Test]
        public void WhenStepPassesEndAndStepIsPositive_ThenRangeExcludesEnd()
        {
            var range = DateRange.DayRange(
                new DateTime(2020, 1, 1, 0, 0, 0),
                new DateTime(2020, 1, 4, 0, 0, 0),
                2).ToList();

            Assert.AreEqual(2, range.Count());
            Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0), range[0]);
            Assert.AreEqual(new DateTime(2020, 1, 3, 0, 0, 0), range[1]);
        }

        [Test]
        public void WhenStepPassesEndAndStepIsNegative_ThenRangeExcludesEnd()
        {
            var range = DateRange.DayRange(
                new DateTime(2020, 1, 10, 0, 0, 0),
                new DateTime(2020, 1, 5, 0, 0, 0),
                -2).ToList();

            Assert.AreEqual(3, range.Count());
            Assert.AreEqual(new DateTime(2020, 1, 10, 0, 0, 0), range[0]);
            Assert.AreEqual(new DateTime(2020, 1, 8, 0, 0, 0), range[1]);
            Assert.AreEqual(new DateTime(2020, 1, 6, 0, 0, 0), range[2]);
        }
    }
}
