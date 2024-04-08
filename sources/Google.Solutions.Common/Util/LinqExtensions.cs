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
using System.Collections.Specialized;
using System.Linq;

namespace Google.Solutions.Common.Util
{
    public static class LinqExtensions
    {
        public static HashSet<T> ToHashSet<T>(
            this IEnumerable<T> source,
            IEqualityComparer<T>? comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }

        public static IEnumerable<T> EnsureNotNull<T>(
            this IEnumerable<T>? e)
        {
            return e ?? Enumerable.Empty<T>();
        }

        public static bool ContainsAll<T>(
            this IEnumerable<T> sequence,
            IEnumerable<T> lookup)
        {
            return !lookup.Except(sequence).Any();
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, ushort chunkSize)
        {
            Precondition.ExpectNotNull(source, nameof(source));

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

        public static IEnumerable<T> ConcatItem<T>(this IEnumerable<T> target, T item)
        {
            Precondition.ExpectNotNull(target, nameof(target));

            foreach (var t in target)
            {
                yield return t;
            }

            yield return item;
        }

        public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs(
            this NameValueCollection collection)
        {
            return collection
                .ExpectNotNull(nameof(collection))
                .Cast<string>()
                .Select(key => new KeyValuePair<string, string>(key, collection[key]));
        }

        public static IDictionary<K, V> ToDictionary<K, V>(
            this IEnumerable<KeyValuePair<K, V>> entries)
        {
            return entries
                .ExpectNotNull(nameof(entries))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static NameValueCollection ToNameValueCollection(
            this IDictionary<string, string> dictionary)
        {
            dictionary.ExpectNotNull(nameof(dictionary));

            var collection = new NameValueCollection();

            foreach (var pair in dictionary)
            {
                collection.Add(pair.Key, pair.Value);
            }

            return collection;
        }
    }
}
