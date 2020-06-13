//
// Copyright 2020 Google LLC
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

namespace Google.Solutions.IapDesktop.Application.Util
{
    /// <summary>
    /// Non-threadsafe, simple implementation of a LRU cache.
    /// </summary>
    public class LeastRecentlyUsedCache<K, V>
    {
        private readonly int capacity;
        private readonly LinkedList<KeyValuePair<K, V>> lruList = new LinkedList<KeyValuePair<K, V>>();
        private readonly Dictionary<K, LinkedListNode<KeyValuePair<K, V>>> cacheMap 
            = new Dictionary<K, LinkedListNode<KeyValuePair<K, V>>>();

        public LeastRecentlyUsedCache(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentException(nameof(capacity));
            }

            this.capacity = capacity;
        }

        public V Lookup(K key)
        {
            if (cacheMap.TryGetValue(key, out LinkedListNode<KeyValuePair<K, V>> node))
            {
                V value = node.Value.Value;

                // Track this as most recently accessed.
                lruList.Remove(node);
                lruList.AddLast(node);

                return value;
            }
            return default(V);
        }

        public void Add(K key, V val)
        {
            if (cacheMap.Count >= capacity)
            {
                PurgeLeastRecentlyUsed();
            }

            var node = new LinkedListNode<KeyValuePair<K, V>>(
                new KeyValuePair<K, V>(key, val));

            // Track this as most recently accessed.
            lruList.AddLast(node);
            cacheMap[key] = node;
        }

        private void PurgeLeastRecentlyUsed()
        {
            var node = lruList.First;
            lruList.RemoveFirst();
            cacheMap.Remove(node.Value.Key);
        }
    }
}
