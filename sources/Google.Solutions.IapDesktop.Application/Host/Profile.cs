﻿//
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

using Google.Apis.Util;
using Google.Solutions.Common.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Google.Solutions.IapDesktop.Application.Host
{
    /// <summary>
    /// User profile containing settings.
    /// </summary>
    public sealed class Profile : IDisposable
    {
        public enum SchemaVersion : uint
        {
            Initial = 1,
            Version229 = 2,
            Current = Version229
        }

        public const string DefaultProfileName = "Default";

        private const string DefaultProfileKey = "1.0";
        private const string ProfileKeyPrefix = DefaultProfileKey + ".";
        private const string SchemaVersionValueName = "SchemaVersion";

        private static readonly Regex ProfileNamePattern = new Regex(@"^[a-zA-Z0-9_\-\ ]+$");

        /// <summary>
        /// Path to policies. This path is independent of the profile.
        /// </summary>
        private const string PoliciesKeyPath = @"Software\Policies\Google\IapDesktop\1.0";

        /// <summary>
        /// Settings key. This is never null.
        /// </summary>
        public RegistryKey SettingsKey { get; private set; }

        /// <summary>
        /// Key for machine policies, can be null.
        /// </summary>
        public RegistryKey MachinePolicyKey { get; private set; }

        /// <summary>
        /// Key for user policies, can be null.
        /// </summary>
        public RegistryKey UserPolicyKey { get; private set; }

        /// <summary>
        /// Name of the profile.
        /// </summary>
        public string Name { get; private set; }

        public bool IsDefault { get; private set; }

        /// <summary>
        /// Version of the profile schema. The schema defines which
        /// subkeys and values reader can expect, and which default
        /// to apply.
        /// </summary>
        public SchemaVersion Version
            => this.SettingsKey.GetValue(SchemaVersionValueName, null) is int version
                ? (SchemaVersion)(uint)version
                : SchemaVersion.Initial;

        private Profile()
        {
        }

        public static bool IsValidProfileName(string name)
        {
            return name != null &&
                !name.Equals(DefaultProfileName, StringComparison.OrdinalIgnoreCase) &&
                name.Trim() == name &&
                name.Length < 32 &&
                ProfileNamePattern.IsMatch(name);
        }

        /// <summary>
        /// Create a new (secondary) profile.
        /// </summary>
        public static Profile CreateProfile(
            Install install,
            string name)
        {
            install.ThrowIfNull(nameof(install));
            if (!IsValidProfileName(name))
            {
                throw new ArgumentException("Invalid profile name");
            }

            using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            {
                using (var profileKey = hkcu.CreateSubKey($@"{install.BaseKeyPath}\{ProfileKeyPrefix}{name}"))
                {
                    //
                    // Store the current schema version to allow future readers
                    // to decide whether certain backward-compatibility is needed
                    // or not.
                    //
                    profileKey.SetValue(
                        SchemaVersionValueName,
                        SchemaVersion.Current,
                        RegistryValueKind.DWord);
                }
            }

            return OpenProfile(install, name);
        }

        /// <summary>
        /// Open the default profile or a secondary profile. The default profile
        /// is created automatically if it doesn't exist yet.
        /// </summary>
        public static Profile OpenProfile(
            Install install,
            string name)
        {
            install.ThrowIfNull(nameof(install));
            if (name != null && !IsValidProfileName(name))
            {
                throw new ArgumentException($"Invalid profile name: {name}");
            }

            using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
            {
                if (name == null)
                {
                    //
                    // Open or create default profile. For backwards compatbility
                    // reasons, the default profile uses the key "1.0".
                    //
                    var profileKeyPath = $@"{install.BaseKeyPath}\{DefaultProfileKey}";
                    var profileKey = hkcu.OpenSubKey(profileKeyPath, true);
                    if (profileKey != null)
                    {
                        //
                        // Default profile exists, open it.
                        //
                    }
                    else
                    {
                        //
                        // Key doesn't exist yet. Create new default profile and
                        // mark it as latest-version.
                        //
                        profileKey = hkcu.CreateSubKey(profileKeyPath, true);
                        profileKey.SetValue(
                            SchemaVersionValueName,
                            SchemaVersion.Current,
                            RegistryValueKind.DWord);
                    }

                    return new Profile()
                    {
                        Name = DefaultProfileName,
                        IsDefault = true,
                        MachinePolicyKey = hklm.OpenSubKey(PoliciesKeyPath),
                        UserPolicyKey = hkcu.OpenSubKey(PoliciesKeyPath),
                        SettingsKey = profileKey
                    };
                }
                else
                {
                    //
                    // Open existing profile.
                    //
                    var profileKey = hkcu.OpenSubKey($@"{install.BaseKeyPath}\{ProfileKeyPrefix}{name}", true);
                    if (profileKey == null)
                    {
                        throw new ProfileNotFoundException("Unknown profile: " + name);
                    }

                    return new Profile()
                    {
                        Name = name,
                        IsDefault = false,
                        MachinePolicyKey = hklm.OpenSubKey(PoliciesKeyPath),
                        UserPolicyKey = hkcu.OpenSubKey(PoliciesKeyPath),
                        SettingsKey = profileKey
                    };
                }
            }
        }

        public static void DeleteProfile(
            Install install,
            string name)
        {
            install.ThrowIfNull(nameof(install));

            using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            {
                var path = $@"{install.BaseKeyPath}\{ProfileKeyPrefix}{name}";
                using (var key = hkcu.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        hkcu.DeleteSubKeyTree(path);
                    }
                }
            }
        }

        public static IEnumerable<string> ListProfiles(Install install)
        {
            install.ThrowIfNull(nameof(install));

            using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            using (var profiles = hkcu.OpenSubKey(install.BaseKeyPath))
            {
                if (profiles == null)
                {
                    return Enumerable.Empty<string>();
                }
                else
                {
                    return profiles.GetSubKeyNames()
                        .EnsureNotNull()
                        .Where(n => n == DefaultProfileKey || n.StartsWith(ProfileKeyPrefix))
                        .Select(n => n == DefaultProfileKey
                            ? DefaultProfileName
                            : n.Substring(ProfileKeyPrefix.Length));
                }
            }
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
