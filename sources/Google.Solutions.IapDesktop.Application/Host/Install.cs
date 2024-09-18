//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Properties;
using Google.Solutions.Platform;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.IapDesktop.Application.Host
{
    /// <summary>
    /// Represents the current installation.
    /// 
    /// The class also maintains a history of previously installed versions 
    /// in the registry. This history is used to distinguish between "fresh"
    /// instals and upgraded installs.
    /// </summary>
    public interface IInstall
    {
        /// <summary>
        /// Currently installed and running version.
        /// </summary>
        Version CurrentVersion { get; }

        /// <summary>
        /// First version that was ever installed. This might be the same as the
        /// current version.
        /// </summary>
        Version InitialVersion { get; }

        /// <summary>
        /// Version that was installed previously. Null if the user never upgraded.
        /// </summary>
        Version? PreviousVersion { get; }

        /// <summary>
        /// Base registry key for profiles, etc.
        /// </summary>
        string BaseKeyPath { get; }

        /// <summary>
        /// Base directory.
        /// </summary>
        string BaseDirectory { get; }

        /// <summary>
        /// Unique ID for this installation.
        /// </summary>
        string UniqueId { get; }

        /// <summary>
        /// List profiles.
        /// </summary>
        IEnumerable<string> Profiles { get; }

        /// <summary>
        /// Create a new (secondary) profile.
        /// </summary>
        UserProfile CreateProfile(string name);

        /// <summary>
        /// Open the default profile or a secondary profile. The default profile
        /// is created automatically if it doesn't exist yet.
        /// </summary>
        UserProfile OpenProfile(string? name);

        /// <summary>
        /// Delete a secondary profile.
        /// </summary>
        void DeleteProfile(string name);
    }

    public class Install : IInstall
    {
        private const string VersionHistoryValueName = "InstalledVersionHistory";

        private const string DefaultProfileKey = "1.0";
        private const string ProfileKeyPrefix = DefaultProfileKey + ".";

        /// <summary>
        /// Base path to profile settings.
        /// </summary>
        public const string DefaultBaseKeyPath = @"Software\Google\IapDesktop";

        /// <summary>
        /// Path to policies. This path is independent of the profile.
        /// </summary>
        private const string PoliciesKeyPath = @"Software\Policies\Google\IapDesktop\1.0";

        private static readonly Version assemblyVersion;
        private static readonly string uniqueId;

        //---------------------------------------------------------------------
        // Static properties (based on assembly metadata).
        //---------------------------------------------------------------------

        /// <summary>
        /// Friendly name.
        /// </summary>
        public const string ProductName = "IAP Desktop";

        /// <summary>
        /// Default product icon to use for windows.
        /// </summary>
        public static Icon ProductIcon => Resources.logo;

        /// <summary>
        /// User agent to use in all HTTP requests.
        /// </summary>
        public static UserAgent UserAgent { get; }

        public static bool IsExecutingTests { get; }

        static Install()
        {
            var platform =
                $"{Environment.OSVersion.VersionString}; " +
                $"{ProcessEnvironment.ProcessArchitecture.ToString().ToLower()}/" +
                $"{ProcessEnvironment.NativeArchitecture.ToString().ToLower()}";

            assemblyVersion = typeof(Install).Assembly.GetName().Version;
            UserAgent = new UserAgent(
                "IAP-Desktop",
                assemblyVersion,
                platform);
            IsExecutingTests = Assembly.GetEntryAssembly() == null;

            using (var hklm = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                RegistryView.Default))
            using (var crptoKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
            {
                //
                // Read the machine GUID. This is unique ID that's generated
                // during Windows setup.
                //
                var machineGuid = (string?)crptoKey?.GetValue("MachineGuid") ?? string.Empty;

                //
                // Create a hash and use the first few bytes as unique ID.
                //
                // NB. Use SHA256.Create for FIPS-awareness.
                //
                using (var hash = SHA256.Create())
                {
                    uniqueId = Convert.ToBase64String(
                        hash.ComputeHash(Encoding.UTF8.GetBytes(machineGuid)), 0, 12);
                }
            }
        }

        //---------------------------------------------------------------------
        // Public properties (based on registry data).
        //---------------------------------------------------------------------

        public Version CurrentVersion => assemblyVersion;

        public string UniqueId => uniqueId;

        public string BaseKeyPath { get; }

        public string BaseDirectory { get; }

        public Install(string baseKeyPath)
        {
            this.BaseKeyPath = baseKeyPath.ExpectNotNull(nameof(baseKeyPath));
            this.BaseDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location)
                .DirectoryName;

            //
            // Create or amend version history.
            //
            using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            {
                using (var key = hkcu.CreateSubKey(baseKeyPath))
                {
                    var history = ((string[])key.GetValue(VersionHistoryValueName))
                        .EnsureNotNull()
                        .ToHashSet();

                    Debug.Assert(history != null);
                    history = history.ExpectNotNull(nameof(history));

                    history.Add(this.CurrentVersion.ToString());

                    key.SetValue(
                        VersionHistoryValueName,
                        history.ToArray(),
                        RegistryValueKind.MultiString);
                }
            }
        }

        public Version InitialVersion
        {
            get
            {
                using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
                {
                    using (var key = hkcu.CreateSubKey(this.BaseKeyPath))
                    {
                        var history = ((string[])key.GetValue(VersionHistoryValueName))
                            .EnsureNotNull()
                            .Select(v => new Version(v));

                        return history.Any()
                            ? history.Min()
                            : this.CurrentVersion;
                    }
                }
            }
        }

        public Version? PreviousVersion
        {
            get
            {
                using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
                {
                    using (var key = hkcu.CreateSubKey(this.BaseKeyPath))
                    {
                        var history = ((string[])key.GetValue(VersionHistoryValueName))
                            .EnsureNotNull()
                            .Select(v => new Version(v))
                            .Where(v => v != this.CurrentVersion);

                        return history.Any()
                            ? history.Max()
                            : null;
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Manage profiles.
        //---------------------------------------------------------------------

        public UserProfile CreateProfile(string name)
        {
            if (!UserProfile.IsValidProfileName(name))
            {
                throw new ArgumentException("Invalid profile name");
            }

            using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            {
                using (var profileKey = hkcu.CreateSubKey($@"{this.BaseKeyPath}\{ProfileKeyPrefix}{name}"))
                {
                    //
                    // Store the current schema version to allow future readers
                    // to decide whether certain backward-compatibility is needed
                    // or not.
                    //
                    profileKey.SetValue(
                        UserProfile.SchemaVersionValueName,
                        UserProfile.SchemaVersion.Current,
                        RegistryValueKind.DWord);
                }
            }

            return OpenProfile(name);
        }

        public UserProfile OpenProfile(string? name)
        {
            if (name != null && !UserProfile.IsValidProfileName(name))
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
                    var profileKeyPath = $@"{this.BaseKeyPath}\{DefaultProfileKey}";
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
                            UserProfile.SchemaVersionValueName,
                            UserProfile.SchemaVersion.Current,
                            RegistryValueKind.DWord);
                    }

                    return new UserProfile(
                        UserProfile.DefaultName,
                        profileKey,
                        hklm.OpenSubKey(PoliciesKeyPath),
                        hkcu.OpenSubKey(PoliciesKeyPath),
                        true);
                }
                else
                {
                    //
                    // Open existing profile.
                    //
                    var profileKey = hkcu.OpenSubKey($@"{this.BaseKeyPath}\{ProfileKeyPrefix}{name}", true);
                    if (profileKey == null)
                    {
                        throw new ProfileNotFoundException("Unknown profile: " + name);
                    }

                    return new UserProfile(
                        name,
                        profileKey,
                        hklm.OpenSubKey(PoliciesKeyPath),
                        hkcu.OpenSubKey(PoliciesKeyPath),
                        false);
                }
            }
        }

        public void DeleteProfile(string name)
        {
            using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            {
                var path = $@"{this.BaseKeyPath}\{ProfileKeyPrefix}{name}";
                using (var key = hkcu.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        hkcu.DeleteSubKeyTree(path);
                    }
                }
            }
        }

        public IEnumerable<string> Profiles
        {
            get
            {
                using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
                using (var profiles = hkcu.OpenSubKey(this.BaseKeyPath))
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
                                ? UserProfile.DefaultName
                                : n.Substring(ProfileKeyPrefix.Length));
                    }
                }
            }
        }
    }
}
