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
    /// Single authorized key.
    /// See https://cloud.google.com/compute/docs/instances/adding-removing-ssh-keys.
    /// </summary>
    public abstract class MetadataAuthorizedPublicKey
        : IEquatable<MetadataAuthorizedPublicKey>, IAuthorizedPublicKey
    {
        //
        // NB Managed and unmanaged keys use a different format,
        // this pattern matches both.
        //
        private static readonly Regex keyPattern = new Regex(
            @"^([^\s]*):([^\s]+) ([^\s]+) ([^\s]+)( \{.*\})?$");

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
        public abstract string Email { get; }

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

        public static MetadataAuthorizedPublicKey Parse(string line)
        {
            Debug.Assert(!line.Contains('\n'));

            var match = keyPattern.Match(line);
            if (!match.Success || match.Groups.Count != 6)
            {
                throw new ArgumentException(
                    $"Format of metadata key is invalid: {line.Truncate(20)}");
            }

            var username = match.Groups[1].Value;
            var keyType = match.Groups[2].Value;
            var key = match.Groups[3].Value;

            // NB. "google-ssh" is also a valid username.
            if (match.Groups[4].Value == ManagedKeyToken &&
                !string.IsNullOrWhiteSpace(match.Groups[5].Value))
            {
                // This is a managed key.
                var keyMetadata = JsonConvert.DeserializeObject<
                    ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata>(
                        match.Groups[5].Value);

                return new ManagedMetadataAuthorizedPublicKey(
                    username,
                    keyType,
                    key,
                    keyMetadata!);
            }
            else
            {
                // This is an unmanaged key.
                return new UnmanagedMetadataAuthorizedPublicKey(
                    username,
                    keyType,
                    key,
                    match.Groups[4].Value);
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
            => Equals(obj as ManagedMetadataAuthorizedPublicKey);

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

    public class UnmanagedMetadataAuthorizedPublicKey : MetadataAuthorizedPublicKey, IAuthorizedPublicKey
    {
        public override string Email { get; }

        public override DateTime? ExpireOn => null;

        public UnmanagedMetadataAuthorizedPublicKey(
            string posixUsername,
            string keyType,
            string key,
            string username)
            : base(posixUsername, keyType, key)
        {
            Precondition.ExpectNotEmpty(username, nameof(username));

            this.Email = username;
        }

        public override string ToString()
        {
            return $"{this.PosixUsername}:{this.KeyType} {this.PublicKey} {this.Email}";
        }
    }

    public class ManagedMetadataAuthorizedPublicKey : MetadataAuthorizedPublicKey, IAuthorizedPublicKey
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
