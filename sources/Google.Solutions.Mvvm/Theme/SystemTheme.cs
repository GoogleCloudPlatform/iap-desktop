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

using Google.Solutions.Common.Interop;
using Google.Solutions.Platform.Interop;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Exposes theming settings applied to Windows.
    /// </summary>
    public static class SystemTheme
    {
        /// <summary>
        /// User-chosen accent color, used in title bars and elsewhere.
        /// </summary>
        public static Color AccentColor
        {
            get
            {
                var hr = NativeMethods.DwmGetColorizationColor(
                    out var colorization,
                    out var opaqueBlend);
                if (hr.Failed())
                {
                    return SystemColors.ActiveBorder;
                }

                colorization |= (opaqueBlend ? 0xFF000000 : 0);
                return Color.FromArgb((int)colorization);
            }
        }

        /// <summary>
        /// Check if the Windows version supports dark mode.
        /// </summary>
        public static bool IsDarkModeSupported
        {
            get
            {
                //
                // We're using UxTheme.dll, which is undocumented. The
                // export ordinals should be constant since this build, but
                // were different or missing before.
                //
                var osVersion = Environment.OSVersion.Version;
                return osVersion.Major > 10 ||
                       (osVersion.Major == 10 && osVersion.Build >= 18985);
            }
        }

        /// <summary>
        /// Check if Windows apps should use dark mode.
        /// </summary>
        public static bool ShouldAppsUseDarkMode
        {
            get
            {
                if (!IsDarkModeSupported)
                {
                    return false;
                }

                //
                // Running on an OS version that supports dark mode, so
                // it should be safe to make this API call.
                //
                return NativeMethods.ShouldAppsUseDarkMode() != 0;
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("dwmapi.dll")]
            public static extern HRESULT DwmGetColorizationColor(
                out uint pcrColorization,
                [MarshalAs(UnmanagedType.Bool)] out bool pfOpaqueBlend);

            /// <summary>
            /// Check if apps should enable dark mode.
            /// 
            /// Note: This returns a 1-byte boolean, not a 4-byte boolean (BOOL)
            /// as Win32 APIs typically do.
            /// </summary>
            [DllImport("uxtheme.dll", SetLastError = true, EntryPoint = "#132")]
            public static extern byte ShouldAppsUseDarkMode();
        }
    }
}
