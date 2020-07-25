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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Util;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Common.ApiExtensions.Instance
{
    public static class MetadataExtensions
    {
        public static void Add(
            this Metadata metadata,
            string key,
            string value)
        {
            if (metadata.Items == null)
            {
                metadata.Items = new List<Metadata.ItemsData>();
            }

            var existingEntry = metadata.Items
                .Where(i => i.Key == key)
                .FirstOrDefault();
            if (existingEntry != null)
            {
                existingEntry.Value = value;
            }
            else
            {
                metadata.Items.Add(new Metadata.ItemsData()
                {
                    Key = key,
                    Value = value
                });
            }
        }
        public static void Add(
            this Metadata metadata,
            Metadata additionalMetadata)
        {
            foreach (var item in additionalMetadata.Items.EnsureNotNull())
            {
                metadata.Add(item.Key, item.Value);
            }
        }

        public static string AsString(
            this Metadata metadata)
        {
            return "[" +
                string.Join(
                    ", ",
                    metadata
                        .Items
                        .EnsureNotNull()
                        .Select(i => $"{i.Key}={i.Value}")) + "]";
        }
    }
}
