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
using Google.Solutions.Mvvm.Controls;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for using "Vista-style" common controls.
    /// </summary>
    public static class WindowsVistaTheme
    {
        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        private static void StyleTreeView(TreeView treeView)
        {
            treeView.HotTracking = true;

            //
            // NB. When called after AllowDarkModeForWindow, this also applies
            // dark mode-style scrollbars, etc.
            //
            NativeMethods.SetWindowTheme(treeView.Handle, "Explorer", null);
        }

        private static void StyleListView(ListView listView, bool darkMode)
        {
            listView.HotTracking = false;

            //
            // NB. When called after AllowDarkModeForWindow, this also applies
            // dark mode-style scrollbars, etc.
            //
            NativeMethods.SetWindowTheme(listView.Handle, "Explorer", null);

            if (darkMode)
            {
                //
                // In dark mode, we also need to apply a theme to the header,
                // otherwise it stays in light mode. Note that this doesn't
                // set the header text color correctly yet.
                //
                var headerHandle = listView.GetHeaderHandle();
                Debug.Assert(headerHandle != IntPtr.Zero);

                NativeMethods.SetWindowTheme(headerHandle, "ItemsView", null);
            }
        }

        //---------------------------------------------------------------------
        // Extension methods.
        //---------------------------------------------------------------------

        /// <summary>
        /// Register rules.
        /// 
        /// NB. When combined with dark mode, the dark mode rules must be
        /// added first.
        /// </summary>
        public static void AddWindowsVistaThemeRules(
            this ControlTheme controlTheme,
            bool darkMode)
        {
            controlTheme.ThrowIfNull(nameof(controlTheme));

            controlTheme.AddRule<TreeView>(
                StyleTreeView,
                ControlTheme.Options.ApplyWhenHandleCreated);
            controlTheme.AddRule<ListView>(
                c => StyleListView(c, darkMode),
                ControlTheme.Options.ApplyWhenHandleCreated);
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("uxtheme", ExactSpelling = true, CharSet = CharSet.Unicode)]
            public static extern int SetWindowTheme(
                IntPtr hWnd,
                string textSubAppName,
                string textSubIdList);
        }
    }
}
