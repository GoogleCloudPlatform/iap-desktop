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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Properties;
using Microsoft.Win32;
using System;
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
        Version PreviousVersion { get; }

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
    }

    public enum Architecture
    {
        X86,
        X64
    }

    public class Install : IInstall
    {
        private const string VersionHistoryValueName = "InstalledVersionHistory";

        public const string DefaultBaseKeyPath = @"Software\Google\IapDesktop";

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

        /// <summary>
        /// Architecture of the CPU.
        /// </summary>
        public static Architecture CpuArchitecture
        {
            get => Environment.Is64BitOperatingSystem ? Architecture.X64 : Architecture.X86;
        }

        /// <summary>
        /// Architecture of the process (which might run emulated).
        /// </summary>
        public static Architecture ProcessArchitecture
        {
#if X86
            get => Architecture.X86;
#elif X64
            get => Architecture.X64;
#else
#error Unknown architecture
#endif
        }

        public static bool IsExecutingTests { get; }

        static Install()
        {
            var platform = 
                $"{Environment.OSVersion.VersionString}; " +
                $"{ProcessArchitecture.ToString().ToLower()}/" +
                $"{CpuArchitecture.ToString().ToLower()}";

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
                var machineGuid = (string)crptoKey?.GetValue("MachineGuid") ?? string.Empty;

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

        public Version PreviousVersion
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
    }
}
