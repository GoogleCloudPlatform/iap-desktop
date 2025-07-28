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
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// A set of authorized keys.
    /// See https://cloud.google.com/compute/docs/instances/adding-removing-ssh-keys.
    /// </summary>
    public class MetadataAuthorizedPublicKeySet
    {
        public const string LegacyMetadataKey = "sshKeys";
        public const string MetadataKey = "ssh-keys";

        public IEnumerable<MetadataAuthorizedPublicKey> Keys { get; }

        //---------------------------------------------------------------------
        // Metadata.
        //---------------------------------------------------------------------
        private MetadataAuthorizedPublicKeySet(IEnumerable<MetadataAuthorizedPublicKey> keys)
        {
            this.Keys = keys;
        }

        public static MetadataAuthorizedPublicKeySet FromMetadata(Metadata.ItemsData data)
        {
            Precondition.ExpectNotNull(data, nameof(data));
            if (data.Key != MetadataKey)
            {
                throw new ArgumentException("Not a valid metadata key");
            }

            return new MetadataAuthorizedPublicKeySet(
                data.Value
                    .Split('\n')
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => MetadataAuthorizedPublicKey.Parse(line.Trim()))
                    .ToList());
        }

        public static MetadataAuthorizedPublicKeySet FromMetadata(Metadata data)
        {
            var item = data?.Items?.FirstOrDefault(i => i.Key == MetadataKey);
            if (item != null && !string.IsNullOrEmpty(item.Value))
            {
                return FromMetadata(item);
            }
            else
            {
                return new MetadataAuthorizedPublicKeySet(
                    Enumerable.Empty<MetadataAuthorizedPublicKey>());
            }
        }

        public MetadataAuthorizedPublicKeySet Add(MetadataAuthorizedPublicKey key)
        {
            if (Contains(key))
            {
                return this;
            }
            else
            {
                return new MetadataAuthorizedPublicKeySet(this.Keys.ConcatItem(key));
            }
        }

        public MetadataAuthorizedPublicKeySet RemoveExpiredKeys()
        {
            return new MetadataAuthorizedPublicKeySet(
                this.Keys.Where(k =>
                {
                    if (k is ManagedMetadataAuthorizedPublicKey managed)
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

        public bool Contains(MetadataAuthorizedPublicKey key)
        {
            return this.Keys.Any(k => k.Equals(key));
        }

        public MetadataAuthorizedPublicKeySet Remove(MetadataAuthorizedPublicKey key)
        {
            return new MetadataAuthorizedPublicKeySet(
                this.Keys.Where(k => !k.Equals(key)));
        }

        public override string ToString()
        {
            return string.Join("\n", this.Keys);
        }
    }
}
