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

using Google.Apis.Compute.v1.Data;
using Google.Apis.Util;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter
{
    /// <summary>
    /// A set of authorized keys.
    /// See https://cloud.google.com/compute/docs/instances/adding-removing-ssh-keys.
    /// </summary>
    public class MetadataAuthorizedKeySet
    {
        public const string LegacyMetadataKey = "sshKeys";
        public const string MetadataKey = "ssh-keys";

        public IEnumerable<MetadataAuthorizedKey> Keys { get; }

        //---------------------------------------------------------------------
        // Metadata.
        //---------------------------------------------------------------------
        private MetadataAuthorizedKeySet(IEnumerable<MetadataAuthorizedKey> keys)
        {
            this.Keys = keys;
        }

        public static MetadataAuthorizedKeySet FromMetadata(Metadata.ItemsData data)
        {
            Utilities.ThrowIfNull(data, nameof(data));
            if (data.Key != MetadataKey)
            {
                throw new ArgumentException("Not a valid metadata key");
            }

            return new MetadataAuthorizedKeySet(
                data.Value
                    .Split('\n')
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => MetadataAuthorizedKey.Parse(line.Trim()))
                    .ToList());
        }

        public static MetadataAuthorizedKeySet FromMetadata(Metadata data)
        {
            var item = data?.Items?.FirstOrDefault(i => i.Key == MetadataKey);
            if (item != null)
            {
                return FromMetadata(item);
            }
            else
            {
                return new MetadataAuthorizedKeySet(
                    Enumerable.Empty<MetadataAuthorizedKey>());
            }
        }

        public MetadataAuthorizedKeySet Add(MetadataAuthorizedKey key)
        {
            if (Contains(key))
            {
                return this;
            }
            else
            {
                return new MetadataAuthorizedKeySet(this.Keys.ConcatItem(key));
            }
        }

        public MetadataAuthorizedKeySet RemoveExpiredKeys()
        {
            return new MetadataAuthorizedKeySet(
                this.Keys.Where(k =>
                {
                    if (k is ManagedMetadataAuthorizedKey managed)
                    {
                        return managed.Metadata.ExpireOn >= DateTime.UtcNow;
                    }
                    else
                    {
                        // Unmanaged keys never expire.
                        return true;
                    }
                }));
        }

        public bool Contains(MetadataAuthorizedKey key)
        {
            return this.Keys
                .Any(k => k.Key == key.Key &&
                          k.KeyType == key.KeyType &&
                          k.LoginUsername == key.LoginUsername);
        }

        public override string ToString()
        {
            return string.Join("\n", this.Keys);
        }
    }
}
