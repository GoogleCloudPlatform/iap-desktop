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

using Google.Solutions.Mvvm.Controls;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    /// <summary>
    /// VS-style theme for common controls.
    /// </summary>
    public class ControlTheme : IControlTheme
    {
        public Color ControlLightLight { get; set; } = SystemColors.ControlLightLight;

        public void ApplyTheme(TreeView treeView)
        {
            //
            // Apply post-Vista Explorer theme.
            //
            treeView.BackColor = this.ControlLightLight;
            treeView.HotTracking = true;

            treeView.HandleCreated += (_, __) =>
            {
                NativeMethods.SetWindowTheme(treeView.Handle, "Explorer", null);
            };
        }

        public void ApplyTheme(ListView listView)
        {
            //
            // Apply post-Vista Explorer theme.
            //
            listView.BackColor = this.ControlLightLight;
            listView.HotTracking = false;

            listView.HandleCreated += (_, __) =>
            {
                NativeMethods.SetWindowTheme(listView.Handle, "Explorer", null);
            };
        }

        public void ApplyTheme(IThemedControl control)
        {
            control.Theme = this;
        }

        public virtual void ApplyTheme(ToolStrip toolStrip)
        { }

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

    public class ToolWindowTheme : ControlTheme
    {
        private ThemeBase dockPanelTheme;

        public ToolWindowTheme(ThemeBase dockPanelTheme)
        {
            this.dockPanelTheme = dockPanelTheme;

            //
            // Use a light gray instead of white.
            //
            this.ControlLightLight = Color.FromArgb(255, 245, 245, 245);
        }

        public override void ApplyTheme(ToolStrip toolStrip)
        {
            this.dockPanelTheme.ApplyTo(toolStrip);
        }
    }
}
