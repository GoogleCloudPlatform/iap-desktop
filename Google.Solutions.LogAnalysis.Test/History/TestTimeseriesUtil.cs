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

using Google.Solutions.LogAnalysis.History;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.LogAnalysis.Test.History
{
    [TestFixture]
    public class TestTimeseriesUtil
    {
        //---------------------------------------------------------------------
        // High watermark tests.
        //---------------------------------------------------------------------
        
        [Test]
        public void WhenJoinersAndLeaversAlternate_ThenHighWatermarkIsOne()
        {
            var baseDate = new DateTime(2020, 1, 1);

            var joiners = new[]
            {
                baseDate,
                baseDate.AddHours(2)
            };

            var leavers = new[]
            {
                baseDate.AddHours(1),
                baseDate.AddHours(3)
            };

            Assert.AreEqual(1, TimeseriesUtil.HighWatermark(joiners, leavers));
        }

        [Test]
        public void WhenJoinersAndLeaversOverlap_ThenHighWatermarkIsOverOne()
        {
            var baseDate = new DateTime(2020, 1, 1);

            var joiners = new[]
            {
                baseDate,
                baseDate.AddHours(1),
                baseDate.AddHours(2),
                baseDate.AddHours(6)
            };

            var leavers = new[]
            {
                baseDate.AddHours(3),
                baseDate.AddHours(4),
                baseDate.AddHours(5),
                baseDate.AddHours(7)
            };

            Assert.AreEqual(3, TimeseriesUtil.HighWatermark(joiners, leavers));
        }

        //---------------------------------------------------------------------
        // DailyHistogram tests.
        //---------------------------------------------------------------------

        [Test]
        public void WhenJoinersAndLeaversOverlap_ThenDailyHistogramUsesTotal()
        {
            var baseDate = new DateTime(2020, 1, 1);

            var joiners = new[]
            {
                baseDate.AddDays(1),
                baseDate.AddDays(2).AddHours(1),
                baseDate.AddDays(3),
                baseDate.AddDays(7).AddSeconds(1)
            };

            var leavers = new[]
            {
                baseDate.AddDays(4).AddMinutes(30),
                baseDate.AddDays(5),
                baseDate.AddDays(6),
                baseDate.AddDays(10)
            };

            var histogram = TimeseriesUtil.DailyHistogram(joiners, leavers).ToList();

            Assert.AreEqual(1, histogram[0].Value);
            Assert.AreEqual(2, histogram[1].Value);
            Assert.AreEqual(3, histogram[2].Value);
            Assert.AreEqual(3, histogram[3].Value);
            Assert.AreEqual(2, histogram[4].Value);
            Assert.AreEqual(1, histogram[5].Value);
            Assert.AreEqual(1, histogram[6].Value);
            Assert.AreEqual(1, histogram[7].Value);
            Assert.AreEqual(1, histogram[8].Value);
            Assert.AreEqual(1, histogram[9].Value);

            Assert.AreEqual(baseDate.AddDays(1), histogram[0].Timestamp);
            Assert.AreEqual(baseDate.AddDays(10), histogram[9].Timestamp);
        }

        [Test]
        public void WhenJoinerOnFirstDay_ThenDailyHistoramIsÓneAtFirstDay()
        {
            var baseDate = new DateTime(2020, 1, 1);

            var joiners = new[]
            {
                baseDate
            };

            var leavers = new[]
            {
                baseDate.AddDays(4).AddMinutes(30)
            };

            var histogram = TimeseriesUtil.DailyHistogram(joiners, leavers).ToList();

            Assert.AreEqual(1, histogram[0].Value);
            Assert.AreEqual(1, histogram[1].Value);
            Assert.AreEqual(1, histogram[2].Value);
            Assert.AreEqual(1, histogram[3].Value);
            Assert.AreEqual(1, histogram[4].Value);
        }

        [Test]
        public void WhenBalanceChagesThroughoutDay_ThenDailyHistoramReportsMaxButRepeatsLast()
        {
            var baseDate = new DateTime(2020, 1, 1);

            var joiners = new[]
            {
                baseDate.AddMinutes(1),
                baseDate.AddMinutes(2),
                baseDate.AddMinutes(3),
            };

            var leavers = new[]
            {
                baseDate.AddMinutes(4),
                baseDate.AddMinutes(5),
                baseDate.AddDays(2)
            };

            var histogram = TimeseriesUtil.DailyHistogram(joiners, leavers).ToList();

            Assert.AreEqual(3, histogram[0].Value);
            Assert.AreEqual(1, histogram[1].Value);
            Assert.AreEqual(1, histogram[2].Value);
        }
    }
}
