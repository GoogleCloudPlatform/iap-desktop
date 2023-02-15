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

using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Theme;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    /// <summary>
    /// VS-style theme for any windows.
    /// </summary>
    public class ControlTheme : IControlTheme
    {
        protected Color ControlLightLight { get; set; } = SystemColors.ControlLightLight;
        protected Color Accent { get; set; } = Color.FromArgb(98, 136, 242);

        protected virtual void ApplyTo(TreeView treeView)
        {
            //
            // Apply post-Vista Explorer theme.
            //
            // TODO: AllowDarkMode - https://stackoverflow.com/questions/53501268/win10-dark-theme-how-to-use-in-winapi
            treeView.BackColor = this.ControlLightLight;
            treeView.HotTracking = true;

            treeView.HandleCreated += (_, __) =>
            {
                NativeMethods.SetWindowTheme(treeView.Handle, "Explorer", null);
            };
        }

        protected virtual void ApplyTo(ListView listView)
        {
            //
            // Apply post-Vista Explorer theme.
            //
            // TODO: AllowDarkMode - https://stackoverflow.com/questions/53501268/win10-dark-theme-how-to-use-in-winapi
            listView.BackColor = this.ControlLightLight;
            listView.HotTracking = false;

            listView.HandleCreated += (_, __) =>
            {
                NativeMethods.SetWindowTheme(listView.Handle, "Explorer", null);
            };
        }

        protected virtual void ApplyTo(PropertyGrid grid)
        {
            grid.ViewBackColor = this.ControlLightLight;
            grid.LineColor = SystemColors.Control;
        }

        protected virtual void ApplyTo(ToolStrip toolStrip)
        { }

        protected virtual void ApplyTo(HeaderLabel headerLabel)
        {
            headerLabel.ForeColor = this.Accent;
        }

        public virtual void ApplyTo(Control control)
        {
            if (control is TreeView treeView)
            {
                ApplyTo(treeView);
            }
            else if (control is ListView listView)
            {
                ApplyTo(listView);
            }
            else if (control is PropertyGrid grid)
            {
                ApplyTo(grid);
            }
            else if (control is ToolStrip toolStrip)
            {
                ApplyTo(toolStrip);
            }
            else if (control is HeaderLabel headerLabel)
            {
                ApplyTo(headerLabel);
            }

            foreach (var child in control.Controls.OfType<Control>())
            {
                ApplyTo(child);
            }
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

    /// <summary>
    /// VS-style theme for tool windows.
    /// </summary>
    public class ToolWindowTheme : ControlTheme
    {
        protected readonly ThemeBase dockPanelTheme;

        public ToolWindowTheme(ThemeBase dockPanelTheme)
        {
            this.dockPanelTheme = dockPanelTheme;

            //
            // Use a light gray instead of white.
            //
            this.ControlLightLight = Color.FromArgb(255, 245, 245, 245);
        }

        protected override void ApplyTo(ToolStrip toolStrip)
        {
            this.dockPanelTheme.ApplyTo(toolStrip);
        }
    }

    public class MainWindowTheme : ToolWindowTheme
    {
        public MainWindowTheme(ThemeBase dockPanelTheme) 
            : base(dockPanelTheme)
        {
        }

        public override void ApplyTo(Control control)
        {
            if (control is FlyoutWindow flyout)
            {
                flyout.BorderColor = Color.FromArgb(0, 122, 204);
            }
            else if (control is DockPanel dockPanel)
            {
                dockPanel.Theme = this.dockPanelTheme;
            }
            else
            {
                base.ApplyTo(control);
            }
        }
    }
}
