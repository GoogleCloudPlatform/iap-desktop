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

using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Compute
{
    public static class LinqExtensions
    {
        public static HashSet<T> ToHashSet<T>(
            this IEnumerable<T> source,
            IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }

        public static HashSet<T> Subtract<T>(this HashSet<T> set, HashSet<T> toSubtract)
        {
            var copy = new HashSet<T>(set);
            copy.ExceptWith(toSubtract);
            return copy;
        }

        public static IEnumerable<T> EnsureNotNull<T>(this IEnumerable<T> e)
        {
            return e == null ? Enumerable.Empty<T>() : e;
        }

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            using (var e = source.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    for (var value = e.Current; e.MoveNext(); value = e.Current)
                    {
                        yield return value;
                    }
                }
            }
        }

        public static bool ContainsAll<T>(this IEnumerable<T> sequence, IEnumerable<T> lookup)
        {
            return !lookup.Except(sequence).Any();
        }
    }
}
