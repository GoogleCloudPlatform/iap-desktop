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
    internal class ControlTheme : IControlTheme
    {
        private readonly IControlTheme baseTheme;
        protected readonly VSTheme vsTheme;

        protected Color Accent { get; set; } = Color.FromArgb(98, 136, 242);

        public ControlTheme(
            IControlTheme baseTheme,
            VSTheme vsTheme)
        {
            this.baseTheme = baseTheme.ThrowIfNull(nameof(baseTheme));
            this.vsTheme = vsTheme.ThrowIfNull(nameof(vsTheme));
        }

        protected virtual void ApplyTo(TreeView treeView)
        {
            //
            // Apply post-Vista Explorer theme.
            //
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
            listView.HotTracking = false;
            listView.HandleCreated += (_, __) =>
            {
                NativeMethods.SetWindowTheme(listView.Handle, "Explorer", null);
            };
        }

        protected virtual void ApplyTo(PropertyGrid grid)
        {
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
            this.baseTheme.ApplyTo(control);

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
    internal class ToolWindowTheme : ControlTheme
    {
        private const float IconGrayScaleFactor = .65f;

        public ToolWindowTheme(
            IControlTheme baseTheme,
            VSTheme vsTheme)
            : base(baseTheme, vsTheme)
        {
        }

        protected override void ApplyTo(TreeView treeView)
        {
            base.ApplyTo(treeView);

            treeView.BackColor = this.vsTheme.Palette.ToolWindowInnerTabInactive.Background;
            treeView.ForeColor = this.vsTheme.Palette.ToolWindowInnerTabInactive.Text;

            if (this.vsTheme.IsDark)
            {
                IconTweaks.InvertAndScaleGrays(treeView.ImageList, IconGrayScaleFactor);
                IconTweaks.InvertAndScaleGrays(treeView.StateImageList, IconGrayScaleFactor);
            }
        }

        protected override void ApplyTo(ListView listView)
        {
            base.ApplyTo(listView);

            listView.BackColor = this.vsTheme.Palette.ToolWindowInnerTabInactive.Background;
            listView.ForeColor = this.vsTheme.Palette.ToolWindowInnerTabInactive.Text;

            if (this.vsTheme.IsDark)
            {
                listView.GridLines = false;
                listView.HotTracking = false;
                IconTweaks.InvertAndScaleGrays(listView.SmallImageList, IconGrayScaleFactor);
                IconTweaks.InvertAndScaleGrays(listView.LargeImageList, IconGrayScaleFactor);
            }
        }

        protected override void ApplyTo(PropertyGrid grid)
        {
            base.ApplyTo(grid);

            grid.ViewBackColor = this.vsTheme.Palette.ToolWindowInnerTabInactive.Background;
        }

        protected override void ApplyTo(ToolStrip toolStrip)
        {
            this.vsTheme.ApplyTo(toolStrip);
        }
    }

    internal class MainWindowTheme : ToolWindowTheme
    {
        public MainWindowTheme(
            IControlTheme baseTheme,
            VSTheme vsTheme) 
            : base(baseTheme, vsTheme)
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
                dockPanel.Theme = this.vsTheme;
            }
            else
            {
                base.ApplyTo(control);
            }
        }
    }
}
