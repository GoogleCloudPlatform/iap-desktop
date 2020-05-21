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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.LogAnalysis.History
{
    internal static class TimeseriesUtil
    {
        /// <summary>
        /// Returns the balance numbers given a stream of joiners
        /// and leavers. The timestamps in the result are not guaranteed
        /// to be unique.
        /// </summary>
        /// <param name="joinersUnsorted"></param>
        /// <param name="leaversUnsorted"></param>
        /// <returns></returns>
        public static IEnumerable<DataPoint> Balances(
            IEnumerable<DateTime> joinersUnsorted,
            IEnumerable<DateTime> leaversUnsorted)
        {
            Debug.Assert(joinersUnsorted.Count() == leaversUnsorted.Count());

            var joinersWithDelta = joinersUnsorted
                .Select(t => new DataPoint(t, 1));
            var leaversWithDelta = leaversUnsorted
                .Select(t => new DataPoint(t, -1));

            // Combine them into a sequence: +1 +1 -1 +1 +1 -1 -1 ...
            var sequence =
                joinersWithDelta.Concat(leaversWithDelta)
                .OrderBy(t => t.Timestamp);  // Order by timestamp

            int balance = 0;
            foreach (var delta in sequence)
            {
                balance += delta.Value;

                Debug.Assert(balance >= 0);
                yield return new DataPoint(delta.Timestamp, balance);
            }
        }

        public static int HighWatermark(
            IEnumerable<DateTime> joinersUnsorted,
            IEnumerable<DateTime> leaversUnsorted)
        {
            return Balances(joinersUnsorted, leaversUnsorted)
                .Select(d => d.Value)
                .Max();
        }

        private static IEnumerable<DateTime> DateSequence(
            DateTime from,
            DateTime to)
        {
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                yield return d;
            }
        }

        public static IEnumerable<DataPoint> DailyHistogram(
            IEnumerable<DateTime> joinersUnsorted,
            IEnumerable<DateTime> leaversUnsorted)
        {
            if (!joinersUnsorted.Any())
            {
                yield break;
            }

            var balancesNonDiscrete = Balances(joinersUnsorted, leaversUnsorted).ToList();

            var lastValue = 0;
            for (var date = balancesNonDiscrete.First().Timestamp.Date;
                 date <= balancesNonDiscrete.Last().Timestamp.Date;
                 date = date.AddDays(1))
            {
                // Get all data points on that day... not the most efficient way, but the 
                // nuber of balances is likely to be pretty small.
                var valuesOnDay = balancesNonDiscrete
                    .Where(d => d.Timestamp >= date && d.Timestamp < date.AddDays(1))
                    .Select(d => d.Value);

                if (valuesOnDay.Any())
                {
                    yield return new DataPoint(date, Math.Max(lastValue, valuesOnDay.Max()));
                    lastValue = valuesOnDay.Last();
                }
                else
                {
                    yield return new DataPoint(date, lastValue);
                }

            }
        }
    }
}
