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

using Google.Apis.Util;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth
{
    /// <summary>
    /// Authorized key entry in metadata. 
    /// See https://cloud.google.com/compute/docs/instances/adding-removing-ssh-keys.
    /// </summary>
    public abstract class MetadataAuthorizedKey
    {
        //
        // NB Managed and unmanaged keys use a different format,
        // this pattern matches both.
        //
        private static readonly Regex keyPattern = new Regex(
            @"^([^\s]+):([^\s]+) ([^\s]+) ([^\s]+)( \{.*\})?$");

        protected const string ManagedKeyToken = "google-ssh";

        public string LoginUsername { get; }
        public string KeyType { get; }
        public string Key { get; }

        protected MetadataAuthorizedKey(
            string loginUsername,
            string keyType,
            string key)
        {
            Utilities.ThrowIfNullOrEmpty(loginUsername, nameof(loginUsername));
            Utilities.ThrowIfNullOrEmpty(keyType, nameof(keyType));
            Utilities.ThrowIfNullOrEmpty(key, nameof(key));

            Debug.Assert(keyType.StartsWith("ssh-"));

            this.LoginUsername = loginUsername;
            this.KeyType = keyType;
            this.Key = key;
        }

        public static MetadataAuthorizedKey Parse(string line)
        {
            Debug.Assert(!line.Contains('\n'));

            var match = keyPattern.Match(line);
            if (!match.Success || match.Groups.Count != 6)
            {
                throw new ArgumentException("Invalid key format");
            }

            var username = match.Groups[1].Value;
            var keyType = match.Groups[2].Value;
            var key = match.Groups[3].Value;

            if (match.Groups[4].Value == ManagedKeyToken)
            {
                // This is a managed key.
                return new ManagedMetadataAuthorizedKey(
                    username,
                    keyType,
                    key,
                    JsonConvert.DeserializeObject<ManagedKeyMetadata>(
                        match.Groups[5].Value));
            }
            else
            {
                // This is an unmanaged key.
                return new UnmanagedMetadataAuthorizedKey(
                    username, 
                    keyType, 
                    key, 
                    match.Groups[4].Value);
            }
        }
    }

    public class UnmanagedMetadataAuthorizedKey : MetadataAuthorizedKey
    {
        public string Username { get; }

        public UnmanagedMetadataAuthorizedKey(
            string loginUsername,
            string keyType,
            string key,
            string username)
            : base(loginUsername, keyType, key)
        {
            Utilities.ThrowIfNullOrEmpty(username, nameof(username));
            
            this.Username = username;
        }

        public override string ToString()
        {
            return $"{this.LoginUsername}:{this.KeyType} {this.Key} {this.Username}";
        }
    }

    public class ManagedMetadataAuthorizedKey : MetadataAuthorizedKey
    {
        public string Username { get; }
        public ManagedKeyMetadata Metadata { get; }

        public ManagedMetadataAuthorizedKey(
            string loginUsername,
            string keyType,
            string key,
            ManagedKeyMetadata metadata)
            : base(loginUsername, keyType, key)
        {
            Utilities.ThrowIfNull(metadata, nameof(metadata));
            Debug.Assert(metadata.Username.Contains("@"));
            
            this.Metadata = metadata;
        }

        public override string ToString()
        {
            var metadata = JsonConvert.SerializeObject(
                this.Metadata,
                new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                });
            return $"{this.LoginUsername}:{this.KeyType} {this.Key} {ManagedKeyToken} {metadata}";
        }
    }

    public class ManagedKeyMetadata
    {
        [JsonProperty("userName")]
        public string Username { get; set; }

        [JsonProperty("expireOn")]
        public DateTime ExpireOn { get; set; }

        [JsonConstructor]
        public ManagedKeyMetadata(
            [JsonProperty("userName")] string username,
            [JsonProperty("expireOn")] DateTime expireOn)
        {
            this.Username = username;
            this.ExpireOn = expireOn;
        }
    }
}
