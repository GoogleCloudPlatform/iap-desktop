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

        /// <summary>
        /// Keys, can be a mix of TMetadataAuthorizedPublicKey and
        /// T:UnrecognizedKey.
        /// </summary>
        private readonly ICollection<object> keys;

        /// <summary>
        /// Authorized keys.
        /// </summary>
        public IEnumerable<MetadataAuthorizedPublicKey> Keys
        {
            get => this.keys.OfType<MetadataAuthorizedPublicKey>();
        }

        //---------------------------------------------------------------------
        // Metadata.
        //---------------------------------------------------------------------

        private MetadataAuthorizedPublicKeySet(ICollection<object> keys)
        {
            this.keys = keys;
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
                    .Select(line => MetadataAuthorizedPublicKey.Parse(line.Trim())) // TODO: TryParse
                    .ToList<object>());
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
                    Array.Empty<object>());
            }
        }

        public MetadataAuthorizedPublicKeySet Add(MetadataAuthorizedPublicKey key) // TODO: check keeps unrecognized
        {
            if (Contains(key))
            {
                return this;
            }
            else
            {
                return new MetadataAuthorizedPublicKeySet(
                    this.keys.ConcatItem(key).ToList());
            }
        }

        public MetadataAuthorizedPublicKeySet RemoveExpiredKeys()
        {
            return new MetadataAuthorizedPublicKeySet(this.keys
                .Where(k =>
                {
                    if (k is ManagedMetadataAuthorizedPublicKey managed)
                    {
                        return managed.Metadata.ExpireOn >= DateTime.UtcNow;
                    }
                    else
                    {
                        //
                        // Keep unmanaged and unrecognized keys, these never expire.
                        //
                        return true;
                    }
                })
                .ToList());
        }

        public bool Contains(MetadataAuthorizedPublicKey key)
        {
            return this.keys.Any(k => k.Equals(key));
        }

        public MetadataAuthorizedPublicKeySet Remove(MetadataAuthorizedPublicKey key) // TODO: test keeps unrecognized keys
        {
            return new MetadataAuthorizedPublicKeySet(this.keys
                .Where(k => !k.Equals(key))
                .ToList());
        }

        public override string ToString() // TODO: test keeps unrecognized keys
        {
            return string.Join("\n", this.keys);
        }

        //---------------------------------------------------------------------
        // Private types.
        //---------------------------------------------------------------------

        /// <summary>
        /// A key that is malformed or unrecognized for other reasons.
        /// </summary>
        private class UnrecognizedKey
        {
            /// <summary>
            /// Raw, unparsed value.
            /// </summary>
            private string Value { get; }

            public UnrecognizedKey(string value)
            {
                this.Value = value;
            }

            public override string ToString()
            {
                return this.Value;
            }

            public override int GetHashCode()
            {
                return this.Value.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return 
                    obj is UnrecognizedKey key && 
                    Equals(key.Value, this.Value);
            }
        }
    }
}
