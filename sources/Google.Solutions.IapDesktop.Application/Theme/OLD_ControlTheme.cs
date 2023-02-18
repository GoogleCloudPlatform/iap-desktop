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
using System.Collections.Generic;
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
    internal class OLD_ControlTheme : IControlTheme
    {
        private readonly IControlTheme baseTheme;

        protected bool ApplyThemeToNewMenuItems { get; set; } = false;

        protected Color Accent { get; set; } = Color.FromArgb(98, 136, 242);

        public OLD_ControlTheme(IControlTheme baseTheme)
        {
            this.baseTheme = baseTheme.ThrowIfNull(nameof(baseTheme));
        }

        protected virtual void ApplyTo(TreeView treeView)
        {
            //
            // Apply post-Vista Explorer theme.
            //
            treeView.HotTracking = true;
            treeView.HandleCreated += (_, __) =>
            {
                //
                // NB. When called after AllowDarkModeForWindow, this applies
                // dark mode-style scrollbars, etc.
                //
                NativeMethods.SetWindowTheme(treeView.Handle, "Explorer", null);
            };

            if (treeView.ContextMenuStrip != null)
            {
                ApplyTo(treeView.ContextMenuStrip);
            }
        }

        protected virtual void ApplyTo(ListView listView)
        {
            //
            // Apply post-Vista Explorer theme.
            //
            listView.HotTracking = false;
            listView.HandleCreated += (_, __) =>
            {
                //
                // NB. When called after AllowDarkModeForWindow, this applies
                // dark mode-style scrollbars, etc.
                //
                NativeMethods.SetWindowTheme(listView.Handle, "Explorer", null);
            };

            if (listView.ContextMenuStrip != null)
            {
                ApplyTo(listView.ContextMenuStrip);
            }
        }

        protected virtual void ApplyTo(PropertyGrid grid)
        {
            grid.LineColor = SystemColors.Control;
        }

        protected virtual void ApplyTo(ToolStrip toolStrip)
        {
            //
            // Apply theme to current items...
            //
            foreach (var item in toolStrip.Items.OfType<ToolStripItem>())
            {
                ApplyTo(item);
            }

            //
            // ...and future items.
            //
            if (this.ApplyThemeToNewMenuItems)
            {
                toolStrip.ItemAdded += (_, args) =>
                {
                    ApplyTo(args.Item);
                };
            }
        }

        protected virtual void ApplyTo(ToolStripItem item)
        {
            if (item is ToolStripDropDownItem dropDownItem)
            {
                //
                // Apply theme to current items...
                //
                var appliedItems = new List<WeakReference<ToolStripItem>>();
                if (dropDownItem.DropDownItems != null)
                {
                    foreach (var subItem in dropDownItem.DropDownItems.OfType<ToolStripItem>())
                    {
                        ApplyTo(subItem);
                        appliedItems.Add(new WeakReference<ToolStripItem>(subItem));
                    }
                }

                //
                // ...and future items.
                //
                // There's no ItemAdded event, so we have to check for new items
                // every time the menu pops up.
                //
                if (this.ApplyThemeToNewMenuItems)
                {
                    dropDownItem.DropDownOpening += (_, args) =>
                    {
                        if (dropDownItem.DropDownItems != null)
                        {
                            foreach (var subItem in dropDownItem.DropDownItems.OfType<ToolStripItem>())
                            {
                                if (!appliedItems.Any(i =>
                                    i.TryGetTarget(out var appliedItem) && appliedItem == subItem))
                                {
                                    ApplyTo(subItem);
                                }

                                appliedItems.Add(new WeakReference<ToolStripItem>(subItem));
                            }
                        }
                    };
                }
            }
        }

        protected virtual void ApplyTo(HeaderLabel headerLabel)
        {
            headerLabel.ForeColor = this.Accent;
        }

        protected virtual void ApplyTo(ToolWindow toolWindow)
        {
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
            else if (control is ToolWindow toolWindow)
            {
                ApplyTo(toolWindow);
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
    internal class ToolWindowTheme : OLD_ControlTheme
    {
        private const float IconGrayScaleFactor = .65f;
        protected readonly VSTheme vsTheme;

        public ToolWindowTheme(
            IControlTheme baseTheme,
            VSTheme vsTheme)
            : base(baseTheme)
        {
            this.vsTheme = vsTheme.ThrowIfNull(nameof(vsTheme));
            this.ApplyThemeToNewMenuItems = this.vsTheme.IsDark;
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

            grid.CategorySplitterColor = this.vsTheme.Palette.GridHeading.Background;
            grid.LineColor = this.vsTheme.Palette.GridHeading.Background;
            grid.CategoryForeColor = this.vsTheme.Palette.GridHeading.Text;

            grid.ViewBackColor = this.vsTheme.Palette.ToolWindowInnerTabInactive.Background;
            grid.ViewForeColor = this.vsTheme.Palette.GridHeading.Text;
            grid.ViewBorderColor = this.vsTheme.Palette.GridHeading.Background;

            grid.HelpBackColor = this.vsTheme.Palette.ToolWindowInnerTabInactive.Background;
            grid.HelpForeColor = this.vsTheme.Palette.GridHeading.Text;
            grid.HelpBorderColor = this.vsTheme.Palette.GridHeading.Background;
        }

        protected override void ApplyTo(ToolStrip toolStrip)
        {
            base.ApplyTo(toolStrip);

            this.vsTheme.ApplyTo(toolStrip);
        }

        protected override void ApplyTo(ToolStripItem item)
        {
            base.ApplyTo(item);

            if (this.vsTheme.IsDark)
            {
                if (item.Image is Bitmap bitmap)
                {
                    IconTweaks.InvertAndScaleGrays(bitmap, IconGrayScaleFactor);
                }
            }
        }

        protected override void ApplyTo(ToolWindow toolWindow)
        {
            base.ApplyTo(toolWindow);

            toolWindow.BackColor = this.vsTheme.Palette.ToolWindowInnerTabInactive.Background;
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
