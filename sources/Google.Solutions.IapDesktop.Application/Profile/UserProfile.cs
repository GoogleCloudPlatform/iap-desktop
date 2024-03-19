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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public RegistryKey SettingsKey { get; }

        /// <summary>
        /// Key for machine policies, can be null.
        /// </summary>
        public RegistryKey? MachinePolicyKey { get; private set; }

        /// <summary>
        /// Key for user policies, can be null.
        /// </summary>
        public RegistryKey? UserPolicyKey { get; private set; }

        public string Name { get; }

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

        private UserProfile(string name, RegistryKey settingsKey)
        {
            this.Name = name;
            this.SettingsKey = settingsKey;
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
        public static UserProfile CreateProfile(
            IInstall install,
            string name)
        {
            install.ExpectNotNull(nameof(install));
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
        public static UserProfile OpenProfile(
            IInstall install,
            string name)
        {
            install.ExpectNotNull(nameof(install));
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
                    // Open or create default profile. For backwards compatibility
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

                    return new UserProfile(DefaultProfileName, profileKey)
                    {
                        IsDefault = true,
                        MachinePolicyKey = hklm.OpenSubKey(PoliciesKeyPath),
                        UserPolicyKey = hkcu.OpenSubKey(PoliciesKeyPath),
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

                    return new UserProfile(name, profileKey)
                    {
                        IsDefault = false,
                        MachinePolicyKey = hklm.OpenSubKey(PoliciesKeyPath),
                        UserPolicyKey = hkcu.OpenSubKey(PoliciesKeyPath),
                    };
                }
            }
        }

        public static void DeleteProfile(
            IInstall install,
            string name)
        {
            install.ExpectNotNull(nameof(install));

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

        public static IEnumerable<string> ListProfiles(IInstall install)
        {
            install.ExpectNotNull(nameof(install));

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
