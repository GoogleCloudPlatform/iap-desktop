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

using Google.Apis.Util;
using Google.Solutions.Common.Net;
using Google.Solutions.Common.Util;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Host
{
    /// <summary>
    /// Represents the current installation.
    /// 
    /// The class also maintains a history of previously installed versions 
    /// in the registry. This history is used to distinguish between "fresh"
    /// instals and upgraded installs.
    /// </summary>
    public class Install
    {
        public const string FriendlyName = "IAP Desktop";

        private const string VersionHistoryValueName = "InstalledVersionHistory";

        public const string DefaultBaseKeyPath = @"Software\Google\IapDesktop";

        private static readonly Version assemblyVersion;

        //---------------------------------------------------------------------
        // Static properties (based on assembly metadata).
        //---------------------------------------------------------------------

        /// <summary>
        /// User agent to use in all HTTP requests.
        /// </summary>
        public static UserAgent UserAgent { get; }

        public static bool IsExecutingTests { get; }

        static Install()
        {
            assemblyVersion = typeof(Install).Assembly.GetName().Version;
            UserAgent = new UserAgent("IAP-Desktop", assemblyVersion);
            IsExecutingTests = Assembly.GetEntryAssembly() == null;
        }

        //---------------------------------------------------------------------
        // Public properties (based on registry data).
        //---------------------------------------------------------------------

        /// <summary>
        /// Currently installed and running version.
        /// </summary>
        public Version CurrentVersion => assemblyVersion;

        /// <summary>
        /// Base registry key for profiles, etc.
        /// </summary>
        public string BaseKeyPath { get; }

        public Install(string baseKeyPath)
        {
            this.BaseKeyPath = baseKeyPath.ThrowIfNull(nameof(baseKeyPath));

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

        /// <summary>
        /// Version that was installed initially. This may be differnt from the
        /// current version.
        /// </summary>
        public Version InitialVersion
        {
            get
            {
                using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
                {
                    using (var key = hkcu.CreateSubKey(this.BaseKeyPath))
                    {
                        var history = ((string[])key.GetValue(VersionHistoryValueName))
                            .EnsureNotNull();

                        return history.Any()
                            ? history.Select(v => new Version(v)).Min()
                            : this.CurrentVersion;
                    }
                }
            }
        }
    }
}
