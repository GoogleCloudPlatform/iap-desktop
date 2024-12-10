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

using Google.Solutions.Common.Util;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Google.Solutions.Common.Linq
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Try to get a value from a dict. 
        /// </summary>
        /// <returns>Value or null if not found</returns>
        public static V? TryGet<K, V>(
            this IDictionary<K, V> dict,
            K key)
            where V : class
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Convert a NameValueCollection to key/value pairs.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs(
            this NameValueCollection collection)
        {
            return collection
                .ExpectNotNull(nameof(collection))
                .Cast<string>()
                .Select(key => new KeyValuePair<string, string>(key, collection[key]));
        }

        /// <summary>
        /// Create a dictionary from a list of key value pairs, ignoring
        /// possible key duplicates.
        /// </summary>
        public static IDictionary<K, V> ToDictionary<K, V>(
            this IEnumerable<KeyValuePair<K, V>> entries)
        {
            return entries
                .ExpectNotNull(nameof(entries))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
