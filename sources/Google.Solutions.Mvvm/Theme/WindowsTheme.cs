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
using Google.Solutions.Common.Interop;
using Google.Solutions.Mvvm.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for using dark mode.
    /// </summary>
    public class WindowsTheme
    {
        /// <summary>
        /// Check if this application uses dark mode.
        /// </summary>
        public bool IsDarkModeEnabled { get; }

        public WindowsTheme(bool darkMode)
        {
            Debug.Assert(!darkMode || IsDarkModeSupported);

            this.IsDarkModeEnabled = darkMode;
            if (darkMode)
            {
                //
                // Force Win32 controls to use dark mode.
                //
                NativeMethods.SetPreferredAppMode(NativeMethods.APPMODE.FORCEDARK);
            }
        }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

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
                return NativeMethods.ShouldAppsUseDarkMode();
            }
        }

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        /// <summary>
        /// Use dark title bar for top-level windows, see
        /// https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/apply-windows-themes
        /// </summary>
        /// <param name="form"></param>
        internal void StyleTitleBar(Form form)
        {
            if (!this.IsDarkModeEnabled || !form.TopLevel)
            {
                return;
            }

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

        /// <summary>
        /// Opt-in window to use dark mode.
        /// </summary>
        internal void StyleControl(Control control)
        {
            if (this.IsDarkModeEnabled)
            {
                NativeMethods.AllowDarkModeForWindow(control.Handle, true);
            }
        }


        internal void StyleTreeView(TreeView treeView)
        {
            treeView.HotTracking = true;

            //
            // NB. When called after AllowDarkModeForWindow, this also applies
            // dark mode-style scrollbars, etc.
            //
            NativeMethods.SetWindowTheme(treeView.Handle, "Explorer", null);
        }

        internal void StyleListView(ListView listView)
        {
            listView.HotTracking = false;

            //
            // NB. When called after AllowDarkModeForWindow, this also applies
            // dark mode-style scrollbars, etc.
            //
            NativeMethods.SetWindowTheme(listView.Handle, "Explorer", null);

            if (this.IsDarkModeEnabled)
            {
                //
                // In dark mode, we also need to apply a theme to the header,
                // otherwise it stays in light mode. Note that this doesn't
                // set the header text color correctly yet.
                //
                var headerHandle = listView.GetHeaderHandle();
                Debug.Assert(headerHandle != IntPtr.Zero);

                NativeMethods.AllowDarkModeForWindow(headerHandle, true);
                NativeMethods.SetWindowTheme(headerHandle, "ItemsView", null);
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //
        // NB. Most APIs are undocumented. See
        // https://github.com/microsoft/WindowsAppSDK/issues/41 for details.
        //---------------------------------------------------------------------

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

            [DllImport("uxtheme", ExactSpelling = true, CharSet = CharSet.Unicode)]
            public static extern int SetWindowTheme(
                IntPtr hWnd,
                string textSubAppName,
                string textSubIdList);
        }
    }

    public static class WindowsThemeExtensions
    {
        /// <summary>
        /// Register rules.
        /// </summary>
        public static ControlTheme AddRules(this ControlTheme controlTheme, WindowsTheme theme)
        {
            controlTheme.ThrowIfNull(nameof(controlTheme));
            theme.ThrowIfNull(nameof(theme));

            controlTheme.AddRule<Form>(
                c => theme.StyleTitleBar(c),
                ControlTheme.Options.ApplyWhenHandleCreated);
            controlTheme.AddRule<Control>(
                c => theme.StyleControl(c),
                ControlTheme.Options.ApplyWhenHandleCreated);
            controlTheme.AddRule<TreeView>(
                c => theme.StyleTreeView(c),
                ControlTheme.Options.ApplyWhenHandleCreated);
            controlTheme.AddRule<ListView>(
                c => theme.StyleListView(c),
                ControlTheme.Options.ApplyWhenHandleCreated);

            return controlTheme;
        }
    }
}
