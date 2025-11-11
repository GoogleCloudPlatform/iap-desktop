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

using Google.Solutions.Common.Text;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// An authorized key as found in metadata.
    /// </summary>
    public abstract class MetadataAuthorizedPublicKey
        : IEquatable<MetadataAuthorizedPublicKey>, IAuthorizedPublicKey
    {
        /// <summary>
        /// Unmanaged keys. These don't have an expiry and use the following format:
        /// 
        /// USERNAME:TYPE KEY_VALUE [EMAIL]
        /// 
        /// EMAIL used to be mandatory, but at some point it became optional.
        /// </summary>
        private static readonly Regex unmanagedKeyPattern = new Regex(
            @"^([^\s]*):([^\s]+)\s+([^\s]+)(\s+[^\s]+)?$");

        /// <summary>
        /// Managed keys. These have an expiry and use the following format:
        /// 
        /// USERNAME:TYPE KEY_VALUE google-ssh {"userName":"EMAIL","expireOn":"EXPIRE_TIME"}
        /// </summary>
        private static readonly Regex managedKeyPattern = new Regex(
            @"^([^\s]*):([^\s]+)\s+([^\s]+)\s+([^\s]+)\s+(\{.*\})$");

        protected const string ManagedKeyToken = "google-ssh";

        /// <summary>
        /// POSIX username, can be custom or derived from the
        /// user's email address.
        /// </summary>
        public string PosixUsername { get; }

        /// <summary>
        /// Type of key (rsa-ssa, ...).
        /// </summary>
        public string KeyType { get; }

        /// <summary>
        /// Public key.
        /// </summary>
        public string PublicKey { get; }

        /// <summary>
        /// Email address of owning user account.
        /// </summary>
        public abstract string? Email { get; }

        public abstract DateTime? ExpireOn { get; }

        protected MetadataAuthorizedPublicKey(
            string posixUsername,
            string keyType,
            string key)
        {
            Precondition.ExpectNotEmpty(keyType, nameof(keyType));
            Precondition.ExpectNotEmpty(key, nameof(key));

            this.PosixUsername = posixUsername;
            this.KeyType = keyType;
            this.PublicKey = key;
        }

        public static bool TryParse(
            string line,
            out MetadataAuthorizedPublicKey? result)
        {
            line = line.Trim();
            Debug.Assert(!line.Contains('\n'));

            if (managedKeyPattern.Match(line) is var managedMatch &&
                managedMatch.Success &&
                managedMatch.Groups[4].Value == ManagedKeyToken)
            {
                //
                // This is a managed key.
                //
                var keyMetadata = JsonConvert.DeserializeObject<
                    ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata>(
                        managedMatch.Groups[5].Value);

                result = new ManagedMetadataAuthorizedPublicKey(
                    managedMatch.Groups[1].Value,
                    managedMatch.Groups[2].Value,
                    managedMatch.Groups[3].Value,
                    keyMetadata!);
                return true;
            }
            else if (unmanagedKeyPattern.Match(line) is var unmanagedMatch &&
                unmanagedMatch.Success)
            {
                //
                // This is an unmanaged key.
                //
                result = new UnmanagedMetadataAuthorizedPublicKey(
                    unmanagedMatch.Groups[1].Value,
                    unmanagedMatch.Groups[2].Value,
                    unmanagedMatch.Groups[3].Value,
                    unmanagedMatch.Groups[4].Value.NullIfEmptyOrWhitespace()?.Trim());
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public static MetadataAuthorizedPublicKey Parse(string line)
        {
            if (TryParse(line, out var result)) 
            {
                return result!;
            }
            else
            {
                throw new ArgumentException(
                    $"Format of metadata key is invalid: {line.Truncate(20)}");
            }
        }

        public override int GetHashCode()
        {
            return
                this.PublicKey.GetHashCode() ^
                this.KeyType.GetHashCode() ^
                this.PosixUsername.GetHashCode();
        }

        public override bool Equals(object? obj)
            => Equals(obj as MetadataAuthorizedPublicKey);

        public bool Equals(MetadataAuthorizedPublicKey? other)
        {
            //
            // NB. These 3 fields are all that count when comparing
            // keys. Any additional metadata is irrelevant.
            //
            // Therefore, subclasses don't need to override
            // this method.
            //

            return
                this.PublicKey == other?.PublicKey &&
                this.KeyType == other?.KeyType &&
                this.PosixUsername == other?.PosixUsername;
        }
    }

    public class UnmanagedMetadataAuthorizedPublicKey 
        : MetadataAuthorizedPublicKey, IAuthorizedPublicKey
    {
        public override string? Email { get; }

        public override DateTime? ExpireOn => null;

        public UnmanagedMetadataAuthorizedPublicKey(
            string posixUsername,
            string keyType,
            string key,
            string? username)
            : base(posixUsername, keyType, key)
        {
            this.Email = username;
        }

        public override string ToString()
        {
            var s = $"{this.PosixUsername}:{this.KeyType} {this.PublicKey}";
            if (this.Email != null)
            {
                s += $" {this.Email}";
            }

            return s;
        }
    }

    public class ManagedMetadataAuthorizedPublicKey 
        : MetadataAuthorizedPublicKey, IAuthorizedPublicKey
    {
        public PublicKeyMetadata Metadata { get; }

        public override DateTime? ExpireOn => this.Metadata.ExpireOn;

        public override string Email => this.Metadata.Email;

        public ManagedMetadataAuthorizedPublicKey(
            string loginUsername,
            string keyType,
            string key,
            PublicKeyMetadata metadata)
            : base(loginUsername, keyType, key)
        {
            Precondition.ExpectNotNull(metadata, nameof(metadata));
            Debug.Assert(metadata.ExpireOn.Kind == DateTimeKind.Utc);

            this.Metadata = metadata;
        }

        public override string ToString()
        {
            var metadata = JsonConvert.SerializeObject(
                this.Metadata,
                new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,

                    //
                    // The GCE agent does not support a "Z" suffix and also 
                    // cannot parse a +00:00 suffix (with colons). So the standrd
                    // .NET format strings do not work.
                    //
                    // We know that the date is in UTC, so fake the offset.
                    //
                    DateFormatString = "yyyy-MM-dd'T'HH:mm:ss+0000"
                });
            return $"{this.PosixUsername}:{this.KeyType} {this.PublicKey} {ManagedKeyToken} {metadata}";
        }

        public class PublicKeyMetadata
        {
            /// <summary>
            /// Email address of owning user account.
            /// </summary>
            [JsonProperty("userName")]
            public string Email { get; set; }

            [JsonProperty("expireOn")]
            public DateTime ExpireOn { get; set; }

            [JsonConstructor]
            public PublicKeyMetadata(
                [JsonProperty("userName")] string username,
                [JsonProperty("expireOn")] DateTime expireOn)
            {
                this.Email = username;
                this.ExpireOn = expireOn.ToUniversalTime();
            }
        }
    }
}
