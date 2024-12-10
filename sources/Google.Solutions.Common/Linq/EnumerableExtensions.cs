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

using Google.Solutions.Common.Util;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Common.Linq
{
    /// <summary>
    /// Utility methods for working with Enumerables.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Create a hashset from an enumerable.
        /// </summary>
        public static HashSet<T> ToHashSet<T>(
            this IEnumerable<T> source,
            IEqualityComparer<T>? comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }

        /// <summary>
        /// Ensure that an enumerable is not null.
        /// </summary>
        /// <returns>
        /// The enumerable or an empty enumerable if it's null
        /// </returns>
        public static IEnumerable<T> EnsureNotNull<T>(
            this IEnumerable<T>? e)
        {
            return e ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Check if the enumerable is a subset of another
        /// enumerable.
        /// </summary>
        public static bool ContainsAll<T>(
            this IEnumerable<T> sequence,
            IEnumerable<T> lookup)
        {
            return !lookup.Except(sequence).Any();
        }

        /// <summary>
        /// Split an enumerable into multiple enumerables that
        /// each have a maximum size.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(
            this IEnumerable<T> source, 
            ushort chunkSize)
        {
            source.ExpectNotNull(nameof(source));

            var chunk = new List<T>(chunkSize);
            foreach (var x in source)
            {
                chunk.Add(x);
                if (chunk.Count == chunkSize)
                {
                    yield return chunk;
                    chunk = new List<T>(chunkSize);
                }
            }

            if (chunk.Count != 0)
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Concatenate an enumerable with a single item.
        /// </summary>
        public static IEnumerable<T> ConcatItem<T>(this IEnumerable<T> target, T item)
        {
            target.ExpectNotNull(nameof(target));

            foreach (var t in target)
            {
                yield return t;
            }

            yield return item;
        }
    }
}
