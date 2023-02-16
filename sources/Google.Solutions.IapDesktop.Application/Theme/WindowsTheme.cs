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
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Theme;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    internal abstract class WindowsTheme : IControlTheme
    {
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
        public static bool IsDarkModeEnabled
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
                return NativeMethods.ShouldAppsUseDarkMode();
            }
        }

        /// <summary>
        /// Get theme based on what's configured in Windows.
        /// </summary>
        /// <returns></returns>
        public static WindowsTheme GetSystemTheme()
        {
            return IsDarkModeEnabled
                ? (WindowsTheme)new DarkTheme()
                : new ClassicTheme();
        }

        /// <summary>
        /// Return the default theme, which works on all OS versions.
        /// </summary>
        public static WindowsTheme GetDefaultTheme()
        {
            return new ClassicTheme();
        }

        /// <summary>
        /// Return the default theme, which works on all OS versions.
        /// </summary>
        public static WindowsTheme GetDarkTheme()
        {
            return new DarkTheme();
        }

        public virtual void ApplyTo(Control control)
        { }

        public virtual bool IsDark => false;

        //---------------------------------------------------------------------
        // Implementations.
        //---------------------------------------------------------------------

        /// <summary>
        /// Classic, light theme.
        /// </summary>
        private class ClassicTheme : WindowsTheme
        {
        }

        /// <summary>
        /// Windows 10-style dark theme. See
        /// https://github.com/microsoft/WindowsAppSDK/issues/41 for details
        /// on undocumented method calls.
        /// </summary>
        private class DarkTheme : WindowsTheme
        {
            public DarkTheme()
            {
                //
                // Force Win32 controls to use dark mode.
                //
                var ret = NativeMethods.SetPreferredAppMode(NativeMethods.APPMODE.FORCEDARK);
                Debug.Assert(ret == 0);
            }

            public override bool IsDark => true;

            public override void ApplyTo(Control control)
            {
                void ApplyWithHandleCreated()
                {
                    Debug.Assert(control.IsHandleCreated);
                    if (control is Form form && !(control is ToolWindow))
                    {
                        //
                        // Use dark title bar, see
                        // https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/apply-windows-themes
                        //
                        int darkMode = 1;
                        var hr = NativeMethods.DwmSetWindowAttribute(
                            form.Handle,
                            NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
                            ref darkMode,
                            sizeof(int));
                        if (hr != HRESULT.S_OK)
                        {
                            throw new Win32Exception(
                                "Updating window attributes failed");
                        }
                    }

                    NativeMethods.AllowDarkModeForWindow(control.Handle, true);
                }

                //
                // NB. The control handle may or may not be created yet.
                //
                if (control.IsHandleCreated)
                {
                    ApplyWithHandleCreated();
                }
                else
                {
                    control.HandleCreated += (_, __) =>
                    {
                        ApplyWithHandleCreated();
                    };
                }
            }
        }

        private static class NativeMethods
        {
            public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

            public enum APPMODE : int
            {
                DEFAULT = 0,
                ALLOWDARK = 1,
                FORCEDARK = 2,
                FORCELIGHT = 3,
                MAX = 4
            }

            [DllImport("uxtheme.dll", SetLastError = true, EntryPoint = "#132")]
            public static extern bool ShouldAppsUseDarkMode();

            [DllImport("uxtheme.dll", EntryPoint = "#133")]
            public static extern bool AllowDarkModeForWindow(IntPtr hWnd, bool allow);

            [DllImport("uxtheme.dll", EntryPoint = "#135")]
            public static extern int SetPreferredAppMode(APPMODE appMode);

            [DllImport("dwmapi.dll")]
            public static extern HRESULT DwmSetWindowAttribute(
                IntPtr hwnd, 
                int attr, 
                ref int attrValue, 
                int attrSize);
        }
    }
}
