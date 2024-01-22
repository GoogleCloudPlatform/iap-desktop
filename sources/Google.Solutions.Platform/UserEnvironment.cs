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

using Google.Solutions.Common.Util;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Google.Solutions.Platform
{
    public static class UserEnvironment
    {
        private const string AppPathRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

        /// <summary>
        /// Expands environment-variable strings and replaces them with 
        /// the values defined for the current user.
        /// </summary>
        public static string? ExpandEnvironmentStrings(string? source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            Debug.Assert(source != null);

            var buffer = new StringBuilder(0);
            var sizeRequired = NativeMethods.ExpandEnvironmentStrings(
                source!,
                buffer,
                buffer.Capacity);

            buffer.EnsureCapacity(sizeRequired);
            NativeMethods.ExpandEnvironmentStrings(
                source!,
                buffer,
                buffer.Capacity);

            return buffer.ToString();
        }

        /// <summary>
        /// Try to resolve the full path of a registered application,
        /// considering both per-user and per-machine apps.
        /// </summary>
        /// <see cref="https://learn.microsoft.com/en-us/windows/win32/shell/app-registration"/>
        public static bool TryResolveAppPath(string exeName, out string? path)
        {
            Precondition.ExpectNotEmpty(exeName, nameof(exeName));

            if (exeName.Contains('\\') || exeName.Contains('/'))
            {
                //
                // This looks like a path, not an app name.
                //
                path = null;
                return false;
            }
            else if (!exeName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                //
                // Not an app.
                //
                path = null;
                return false;
            }

            var hives = new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine };
            foreach (var hive in hives)
            {
                using (var hiveKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
                using (var appKey = hiveKey.OpenSubKey($@"{AppPathRegistryKey}\{exeName}", false))
                {
                    //
                    // NB. If the value is of kind REG_EXPAND_SZ, GetValue()
                    // automatically resolves environment variables.
                    //
                    path = (string?)appKey?.GetValue(null);
                    if (!string.IsNullOrEmpty(path))
                    {
                        return true;
                    }
                }
            }

            path = null;
            return false;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int ExpandEnvironmentStrings(
                [MarshalAs(UnmanagedType.LPTStr)] string source,
                [Out] StringBuilder destination,
                int size);
        }
    }
}
