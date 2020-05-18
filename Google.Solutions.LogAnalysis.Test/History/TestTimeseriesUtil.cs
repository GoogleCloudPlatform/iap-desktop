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

namespace Google.Solutions.LogAnalysis.Test.History
{
    [TestFixture]
    public class TestTimeseriesUtil
    {
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

            Assert.AreEqual(1, TimeseriesUtil.GetHighWatermark(joiners, leavers));
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

            Assert.AreEqual(3, TimeseriesUtil.GetHighWatermark(joiners, leavers));
        }
    }
}
