//
// Copyright 2022 Google LLC
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

using Microsoft.Win32;
using System;
using System.Text.RegularExpressions;

namespace Google.Solutions.IapDesktop.Application.Profile
{
    /// <summary>
    /// User profile containing settings.
    /// </summary>
    public interface IUserProfile
    {
        /// <summary>
        /// Name of the profile.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Determines if this is the default profile.
        /// </summary>
        public bool IsDefault { get; }
    }

    public sealed class UserProfile : IUserProfile, IDisposable
    {
        public enum SchemaVersion : uint
        {
            Initial = 1,
            Version229 = 2,
            Version240 = 3,
            Current = Version240
        }

        /// <summary>
        /// Default profile name.
        /// </summary>
        public const string DefaultName = "Default";

        /// <summary>
        /// Value name (in profile key) that indicates the name of the schema.
        /// </summary>
        internal const string SchemaVersionValueName = "SchemaVersion";

        private static readonly Regex ProfileNamePattern = new Regex(@"^[a-zA-Z0-9_\-\ ]+$");

        /// <summary>
        /// Settings key. This is never null.
        /// </summary>
        public RegistryKey SettingsKey { get; }

        /// <summary>
        /// Key for machine policies, can be null.
        /// </summary>
        public RegistryKey? MachinePolicyKey { get; }

        /// <summary>
        /// Key for user policies, can be null.
        /// </summary>
        public RegistryKey? UserPolicyKey { get; }

        /// <summary>
        /// Name of the profile.
        /// </summary>
        public string Name { get; }

        public bool IsDefault { get; }

        /// <summary>
        /// Version of the profile schema. The schema defines which
        /// subkeys and values reader can expect, and which default
        /// to apply.
        /// </summary>
        public SchemaVersion Version
        {
            get => this.SettingsKey.GetValue(SchemaVersionValueName, null) is int version
                ? (SchemaVersion)(uint)version
                : SchemaVersion.Initial;
        }

        internal UserProfile(
            string name,
            RegistryKey settingsKey,
            RegistryKey? machinePolicyKey,
            RegistryKey? userPolicyKey,
            bool isDefault)
        {
            this.Name = name;
            this.SettingsKey = settingsKey;
            this.MachinePolicyKey = machinePolicyKey;
            this.UserPolicyKey = userPolicyKey;
            this.IsDefault = isDefault;
        }

        public static bool IsValidProfileName(string? name)
        {
            return name != null &&
                !name.Equals(DefaultName, StringComparison.OrdinalIgnoreCase) &&
                name.Trim() == name &&
                name.Length < 32 &&
                ProfileNamePattern.IsMatch(name);
        }

        public void Dispose()
        {
            this.SettingsKey.Dispose();
            this.UserPolicyKey?.Dispose();
            this.MachinePolicyKey?.Dispose();
        }
    }

    public class ProfileNotFoundException : Exception
    {
        public ProfileNotFoundException(string message)
            : base(message)
        {
        }
    }
}
