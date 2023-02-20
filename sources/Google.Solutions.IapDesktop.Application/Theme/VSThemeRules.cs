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

        private static void StyleCheckBox(CheckBox checkbox, VSTheme theme)
        {
            checkbox.ForeColor = theme.Palette.Label.Text;
        }

        private static void StyleRadioButton(RadioButton radio, VSTheme theme)
        {
            radio.ForeColor = theme.Palette.Label.Text;
        }

        private static void StyleTextBox(TextBoxBase text, VSTheme theme)
        {
            text.BorderStyle = BorderStyle.FixedSingle;
            text.ForeColor = theme.Palette.TextBox.Text;
            SetBackColor();

            //
            // NB. If the textbox has a scrollbar, it'll remain light gray.
            // There's no good way to apply a dark theme (or any custom colors)
            // to child scroll bar controls as they don't generate a
            // WM_CTLCOLORSCROLLBAR message.
            //

            //
            // Update colors when enabled/readonly status changes.
            //
            text.ReadOnlyChanged += OnEnabledOrReadonyChanged;
            text.EnabledChanged += OnEnabledOrReadonyChanged;
            text.Disposed += (_, __) =>
            {
                text.ReadOnlyChanged -= OnEnabledOrReadonyChanged;
                text.EnabledChanged -= OnEnabledOrReadonyChanged;
            };

            void OnEnabledOrReadonyChanged(object _, EventArgs __)
            {
                SetBackColor();
            }

            void SetBackColor()
            {
                text.BackColor = text.ReadOnly || text.ReadOnly
                    ? theme.Palette.TextBox.BackgroundDisabled
                    : theme.Palette.TextBox.Background;
            }
        }

        private static void StyleComboBox(ComboBox combo, VSTheme theme)
        {
            combo.ForeColor = theme.Palette.ComboBox.Text;
            combo.BackColor = theme.Palette.ComboBox.Background;
        }

        private static void StyleGroupBox(GroupBox group, VSTheme theme)
        {
            group.ForeColor = theme.Palette.Label.Text;

            // TODO: Draw  gray border, https://stackoverflow.com/questions/76455/how-do-you-change-the-color-of-the-border-on-a-group-box
        }

        private static void StyleTabControl(TabControl tab, VSTheme theme)
        {
            foreach (var page in tab.TabPages.Cast<TabPage>())
            {
                page.BackColor = theme.Palette.ToolWindowInnerTabInactive.Background;
            }

            // TODO: Owner-draw the header, border
            // - https://dotnetrix.co.uk/tabcontrol.htm#tip2
            // - http://www.glennslayden.com/code/win32/tab-control-background-brush
        }

        private static void StyleProgressBar(ProgressBar bar, VSTheme theme)
        {
            //
            // The normal visual styles look better unless we need dark mode.
            //
            if (theme.IsDark)
            {
                WindowsTheme.ResetWindowTheme(bar);

                bar.BackColor = theme.Palette.ProgressBar.Background;
                bar.ForeColor = theme.Palette.ProgressBar.Indicator;
            }
        }

        // TODO: InfoBar

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
            controlTheme.AddRule<CheckBox>(c => StyleCheckBox(c, theme));
            controlTheme.AddRule<RadioButton>(c => StyleRadioButton(c, theme));
            controlTheme.AddRule<TextBoxBase>(c => StyleTextBox(c, theme));
            controlTheme.AddRule<ComboBox>(c => StyleComboBox(c, theme));
            controlTheme.AddRule<GroupBox>(c => StyleGroupBox(c, theme));
            controlTheme.AddRule<TabControl>(c => StyleTabControl(c, theme));
            controlTheme.AddRule<ProgressBar>(c => StyleProgressBar(c, theme));

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
