//
// Copyright 2024 Google LLC
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

namespace Google.Solutions.Common.Util
{
    /// <summary>
    /// Common predicates.
    /// </summary>
    public static class Predicate
    {
        /// <summary>
        /// Create a predicate that checks if an integer is in a range.
        /// </summary>
        public static Predicate<int> InRange(int minInclusive, int maxInclusive)
        {
            return v => v >= minInclusive && v <= maxInclusive;
        }

        /// <summary>
        /// Create a predicate that checks if an integer is in a range.
        /// </summary>
        public static Predicate<long> InRange(long minInclusive, long maxInclusive)
        {
            return v => v >= minInclusive && v <= maxInclusive;
        }
    }
}
