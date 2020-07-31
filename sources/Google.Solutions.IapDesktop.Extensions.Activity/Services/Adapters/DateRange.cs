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

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters
{
    internal class DateRange
    {
        public static IEnumerable<DateTime> DayRange(
            DateTime startInclusive,
            DateTime endInclusive,
            int daysStep)
        {
            if (daysStep < 0 && startInclusive < endInclusive ||
                daysStep > 0 && startInclusive > endInclusive)
            {
                throw new ArgumentException(nameof(startInclusive));
            }
            else if (daysStep == 0)
            {
                throw new ArgumentException(nameof(daysStep));
            }

            for (var day = startInclusive.Date; ; day = day.AddDays(daysStep))
            {
                if (daysStep < 0 && day < endInclusive ||
                    daysStep > 0 && day > endInclusive)
                {
                    // Overstepped end date.
                    yield break;
                }

                yield return day;

                if (day == endInclusive.Date)
                {
                    yield break;
                }
            }
        }
    }
}
