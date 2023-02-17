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
using System;
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

        private static void UseExplorerThemeForTreeView(TreeView treeView)
        {
            treeView.HotTracking = true;

            //
            // NB. When called after AllowDarkModeForWindow, this also applies
            // dark mode-style scrollbars, etc.
            //
            NativeMethods.SetWindowTheme(treeView.Handle, "Explorer", null);
        }

        private static void UseExplorerThemeForListView(ListView listView)
        {
            listView.HotTracking = false;

            //
            // NB. When called after AllowDarkModeForWindow, this also applies
            // dark mode-style scrollbars, etc.
            //
            NativeMethods.SetWindowTheme(listView.Handle, "Explorer", null);
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
        public static void AddRules(this ControlTheme controlTheme)
        {
            controlTheme.ThrowIfNull(nameof(controlTheme));

            controlTheme.AddRule<TreeView>(UseExplorerThemeForTreeView);
            controlTheme.AddRule<ListView>(UseExplorerThemeForListView);
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
