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
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    internal static class VSThemeRules
    {
        private const float IconGrayScaleFactor = .65f;
        private static Color AccentColor { get; set; } = Color.FromArgb(98, 136, 242); // TODO: Use color from theme?

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        private static void StyleDockWindow(Form form, VSTheme theme)
        {
            form.BackColor = theme.Palette.ToolWindowInnerTabInactive.Background;
        }

        private static void StyleDialog(Form form, VSTheme theme)
        {
            form.BackColor = theme.Palette.Window.Background;
        }

        private static void StyleFlyoutWindow(FlyoutWindow flyout, VSTheme theme)
        {
            flyout.BackColor = theme.Palette.ToolWindowInnerTabInactive.Background;
            flyout.BorderColor = Color.FromArgb(0, 122, 204); // TODO: Use color from theme?
        }

        private static void StyleDockPanel(DockPanel dockPanel, VSTheme theme)
        {
            dockPanel.Theme = theme;
        }

        private static void StyleHeaderLabel(HeaderLabel headerLabel)
        {
            headerLabel.ForeColor = AccentColor;
        }

        private static void StyleTreeView(TreeView treeView, VSTheme theme)
        {
            treeView.BackColor = theme.Palette.ToolWindowInnerTabInactive.Background;
            treeView.ForeColor = theme.Palette.ToolWindowInnerTabInactive.Text;

            if (theme.IsDark)
            {
                IconTweaks.InvertAndScaleGrays(treeView.ImageList, IconGrayScaleFactor);
                IconTweaks.InvertAndScaleGrays(treeView.StateImageList, IconGrayScaleFactor);
            }
        }

        private static void StyleListView(ListView listView, VSTheme theme)
        {
            listView.BackColor = theme.Palette.ToolWindowInnerTabInactive.Background;
            listView.ForeColor = theme.Palette.ToolWindowInnerTabInactive.Text;

            if (theme.IsDark)
            {
                listView.GridLines = false;
                listView.HotTracking = false;
                IconTweaks.InvertAndScaleGrays(listView.SmallImageList, IconGrayScaleFactor);
                IconTweaks.InvertAndScaleGrays(listView.LargeImageList, IconGrayScaleFactor);
            }
        }

        private static void StylePropertyGrid(PropertyGrid grid, VSTheme theme)
        {
            grid.CategorySplitterColor = theme.Palette.GridHeading.Background;
            grid.LineColor = theme.Palette.GridHeading.Background;
            grid.CategoryForeColor = theme.Palette.GridHeading.Text;

            grid.ViewBackColor = theme.Palette.ToolWindowInnerTabInactive.Background;
            grid.ViewForeColor = theme.Palette.GridHeading.Text;
            grid.ViewBorderColor = theme.Palette.GridHeading.Background;

            grid.HelpBackColor = theme.Palette.ToolWindowInnerTabInactive.Background;
            grid.HelpForeColor = theme.Palette.GridHeading.Text;
            grid.HelpBorderColor = theme.Palette.GridHeading.Background;
        }

        private static void StyleToolStrip(ToolStrip strip, VSTheme theme)
        {
            theme.ApplyTo(strip);
        }

        private static void StyleToolStripItem(ToolStripItem item, VSTheme theme)
        {
            if (theme.IsDark)
            {
                if (item.Image is Bitmap bitmap)
                {
                    IconTweaks.InvertAndScaleGrays(bitmap, IconGrayScaleFactor);
                }
            }
        }

        private static void StyleButton(Button button, VSTheme theme)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = theme.Palette.Button.Border;
            button.FlatAppearance.MouseDownBackColor = theme.Palette.Button.BackgroundPressed;
            button.FlatAppearance.MouseOverBackColor = theme.Palette.Button.BackgroundHover;
            button.BackColor = theme.Palette.Button.Background;
            button.ForeColor = theme.Palette.Button.Text;
            button.UseVisualStyleBackColor = false;
        }

        private static void StyleLabel(Label label, VSTheme theme)
        {
            label.ForeColor = theme.Palette.Label.Text;
        }

        private static void StyleLinkLabel(LinkLabel link, VSTheme theme)
        {
            link.LinkColor = theme.Palette.LinkLabel.Text;
            link.ActiveLinkColor = theme.Palette.LinkLabel.Text;
        }

        //---------------------------------------------------------------------
        // Extension methods.
        //---------------------------------------------------------------------

        private static ControlTheme AddCommonRules(
            ControlTheme controlTheme,
            VSTheme theme)
        {
            controlTheme.AddRule<HeaderLabel>(StyleHeaderLabel);
            controlTheme.AddRule<PropertyGrid>(c => StylePropertyGrid(c, theme));//TODO: Apply before handle created
            controlTheme.AddRule<TreeView>(c => StyleTreeView(c, theme));
            controlTheme.AddRule<ListView>(c => StyleListView(c, theme));
            controlTheme.AddRule<PropertyGrid>(c => StylePropertyGrid(c, theme));
            controlTheme.AddRule<ToolStrip>(c => StyleToolStrip(c, theme));
            controlTheme.AddRule<Button>(c => StyleButton(c, theme));
            controlTheme.AddRule<Label>(c => StyleLabel(c, theme), ControlTheme.Options.IgnoreDerivedTypes);
            controlTheme.AddRule<LinkLabel>(c => StyleLinkLabel(c, theme));

            var menuTheme = new ToolStripItemTheme(true);
            menuTheme.AddRule(i => StyleToolStripItem(i, theme));
            controlTheme.AddRules(menuTheme);

            return controlTheme;
        }

        /// <summary>
        /// Register rules for main window and dialogs.
        /// </summary>
        public static ControlTheme AddDialogRules(
            this ControlTheme controlTheme,
            VSTheme theme)
        {
            controlTheme.ThrowIfNull(nameof(controlTheme));

            controlTheme.AddRule<Form>(c => StyleDialog(c, theme));

            return AddCommonRules(controlTheme, theme);
        }

        /// <summary>
        /// Register basic set of rules that apply to dock windows (main
        /// window and tool windows).
        /// </summary>
        public static ControlTheme AddDockWindowRules(
            this ControlTheme controlTheme,
            VSTheme theme)
        {
            controlTheme.ThrowIfNull(nameof(controlTheme));

            controlTheme.AddRule<Form>(c => StyleDockWindow(c, theme));
            controlTheme.AddRule<DockPanel>(c => StyleDockPanel(c, theme));
            controlTheme.AddRule<FlyoutWindow>(c => StyleFlyoutWindow(c, theme));

            return AddCommonRules(controlTheme, theme);
        }
    }
}
