﻿//
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
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Drawing;
using Google.Solutions.Mvvm.Theme;
using System;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    /// <summary>
    /// Theming rules for applying a VS Theme (i.e., Visual Studio theme file).
    /// </summary>
    internal abstract class VSThemeRuleSetBase
    {
        private static Bitmap listBackgroundImage;

        private readonly IconInverter darkModeIconInverter;

        protected static Color AccentColor { get; set; } = Color.FromArgb(98, 136, 242);

        protected readonly VSTheme theme;

        protected VSThemeRuleSetBase(VSTheme theme)
        {
            this.theme = theme.ThrowIfNull(nameof(theme));
            this.darkModeIconInverter = new IconInverter()
            {
                //
                // NB. These factors are chosen based on what looked good, there's
                // no science behind them.
                //
                InvertGray = true,
                GrayFactor = .63f,
                ColorFactor = .95f,
#if DEBUG
                MarkerPixel = true
#endif
            };
        }

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        private void StyleHeaderLabel(HeaderLabel headerLabel)
        {
            headerLabel.ForeColor = AccentColor;
        }

        private void StyleTreeView(TreeView treeView)
        {
            treeView.BackColor = this.theme.Palette.ToolWindowInnerTabInactive.Background;
            treeView.ForeColor = this.theme.Palette.ToolWindowInnerTabInactive.Text;

            if (this.theme.IsDark)
            {
                darkModeIconInverter.Invert(treeView.ImageList);
                darkModeIconInverter.Invert(treeView.StateImageList);
            }
        }

        private void StyleListView(ListView listView)
        {
            listView.BackColor = this.theme.Palette.ToolWindowInnerTabInactive.Background;
            listView.ForeColor = this.theme.Palette.ToolWindowInnerTabInactive.Text;
            listView.BorderStyle = BorderStyle.FixedSingle;

            if (this.theme.IsDark)
            {
                listView.GridLines = false;
                listView.HotTracking = false;
                darkModeIconInverter.Invert(listView.SmallImageList);
                darkModeIconInverter.Invert(listView.LargeImageList);

                //
                // When disabled, the list view's background turns gray by default.
                // That's fine in light mode, but looks bad in dark mode. It doesn't
                // seem to be possible to override this behavior by using subclassing,
                // but we can disable it by using a background image.
                //
                // Create a 1x1 image with the intended background color, and
                // set that as a tiled background image.
                //
                if (listBackgroundImage == null)
                {
                    listBackgroundImage = new Bitmap(1, 1);
                    using (var g = Graphics.FromImage(listBackgroundImage))
                    using (var brush = new SolidBrush(listView.BackColor))
                    {
                        g.FillRectangle(brush, new Rectangle(Point.Empty, listBackgroundImage.Size));
                    }
                }

                listView.BackgroundImageTiled = true;
                listView.BackgroundImage = listBackgroundImage;
            }
        }

        private void StylePropertyGrid(PropertyGrid grid)
        {
            grid.CategorySplitterColor = this.theme.Palette.GridHeading.Background;
            grid.LineColor = this.theme.Palette.GridHeading.Background;
            grid.CategoryForeColor = this.theme.Palette.GridHeading.Text;

            grid.ViewBackColor = this.theme.Palette.ToolWindowInnerTabInactive.Background;
            grid.ViewForeColor = this.theme.Palette.GridHeading.Text;
            grid.ViewBorderColor = this.theme.Palette.GridHeading.Background;

            grid.HelpBackColor = this.theme.Palette.ToolWindowInnerTabInactive.Background;
            grid.HelpForeColor = this.theme.Palette.GridHeading.Text;
            grid.HelpBorderColor = this.theme.Palette.GridHeading.Background;
        }

        private void StyleToolStrip(ToolStrip strip)
        {
            this.theme.ApplyTo(strip);
        }

        private void StyleToolStripItem(ToolStripItem item)
        {
            if (this.theme.IsDark && !(item.Owner is StatusStrip))
            {
                if (item.Image is Bitmap bitmap)
                {
                    this.darkModeIconInverter.Invert(bitmap);
                }
            }
        }

        private void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = this.theme.Palette.Button.Border;
            button.FlatAppearance.MouseDownBackColor = this.theme.Palette.Button.BackgroundPressed;
            button.FlatAppearance.MouseOverBackColor = this.theme.Palette.Button.BackgroundHover;
            button.BackColor = this.theme.Palette.Button.Background;
            button.ForeColor = this.theme.Palette.Button.Text;
            button.UseVisualStyleBackColor = false;
        }

        private void StyleDropDownButton(DropDownButton button)
        {
            StyleToolStrip(button.Menu);
        }

        private void StyleLabel(Label label)
        {
            //
            // Don't change the color if it was set to something
            // custom (for example, as done in info bars).
            //
            if (label.ForeColor == Control.DefaultForeColor)
            {
                label.ForeColor = this.theme.Palette.Label.Text;
            }
        }

        private void StyleLinkLabel(LinkLabel link)
        {
            link.LinkColor = this.theme.Palette.LinkLabel.Text;
            link.ActiveLinkColor = this.theme.Palette.LinkLabel.Text;
        }

        private void StyleCheckBox(CheckBox checkbox)
        {
            checkbox.ForeColor = this.theme.Palette.Label.Text;
        }

        private void StyleRadioButton(RadioButton radio)
        {
            radio.ForeColor = this.theme.Palette.Label.Text;
        }

        private void StyleTextBox(TextBoxBase text)
        {
            if (text is RichTextBox rtfBox)
            {
                //
                // RichTextBoxes don't support FixedSingle. Fixed3D looks
                // okay in light mode, but awful in Dark mode.
                //
                rtfBox.BorderStyle = this.theme.IsDark
                    ? BorderStyle.None
                    : BorderStyle.Fixed3D;
            }
            else
            {
                text.BorderStyle = BorderStyle.FixedSingle;
            }

            text.ForeColor = this.theme.Palette.TextBox.Text;
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
                    ? this.theme.Palette.TextBox.BackgroundDisabled
                    : this.theme.Palette.TextBox.Background;
            }
        }

        private void StyleComboBox(ComboBox combo)
        {
            //
            // NB. Use FlatStyle.System to prevent a white border
            // around the control when used as a ToolStripDropDown.
            //
            combo.FlatStyle = FlatStyle.System;
            combo.ForeColor = this.theme.Palette.ComboBox.Text;
            combo.BackColor = this.theme.Palette.ComboBox.Background;
        }

        private void StyleGroupBox(GroupBox groupBox)
        {
            groupBox.ForeColor = this.theme.Palette.Label.Text;

            if (theme.IsDark)
            {
                //
                // In dark mode, the border is drawn in black by default,
                // which doesn't look good. Draw a custom border instead
                // that uses the button border color.
                //
                groupBox.Paint += (sender, e) =>
                {
                    var box = (GroupBox)sender;

                    using (var textBrush = new SolidBrush(box.ForeColor))
                    using (var borderBrush = new SolidBrush(theme.Palette.Button.Border))
                    using (var borderPen = new Pen(borderBrush))
                    {
                        var headerTextSize = e.Graphics.MeasureString(box.Text, box.Font);
                        var boxRect = new Rectangle(
                            box.ClientRectangle.X,
                            box.ClientRectangle.Y + (int)(headerTextSize.Height / 2),
                            box.ClientRectangle.Width - 1,
                            box.ClientRectangle.Height - (int)(headerTextSize.Height / 2) - 1);

                        //
                        // Clear text and border.
                        //
                        e.Graphics.Clear(box.BackColor);

                        //
                        // Draw header.
                        //
                        e.Graphics.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);

                        //
                        // Draw Border, starting from the header in clockwise direction.
                        //
                        e.Graphics.DrawLines(
                            borderPen,
                            new Point[] {
                                new Point(boxRect.X + box.Padding.Left + (int)(headerTextSize.Width), boxRect.Y),
                                new Point(boxRect.X + boxRect.Width, boxRect.Y),
                                new Point(boxRect.X + boxRect.Width, boxRect.Y + boxRect.Height),
                                new Point(boxRect.X, boxRect.Y + boxRect.Height),
                                boxRect.Location,
                                new Point(boxRect.X + box.Padding.Left, boxRect.Y)
                            });
                    }
                };
            }
        }

        private void StyleTabControl(VerticalTabControl tab)
        {
            tab.SheetBackColor = this.theme.Palette.ToolWindowInnerTabInactive.Background;
            tab.InactiveTabBackColor = this.theme.Palette.TabControl.TabBackground;
            tab.InactiveTabForeColor = this.theme.Palette.TabControl.TabText;
            tab.ActiveTabBackColor = this.theme.Palette.TabControl.SelectedTabBackground;
            tab.ActiveTabForeColor = this.theme.Palette.TabControl.SelectedTabText;
            tab.HoverTabBackColor = this.theme.Palette.TabControl.MouseOverTabBackground;
            tab.HoverTabForeColor = this.theme.Palette.TabControl.MouseOverTabText;
        }

        private void StylePropertiesDialog(PropertiesDialog dialog)
        {
            dialog.SheetBackColor = this.theme.Palette.ToolWindowInnerTabInactive.Background;
        }

        private void StyleProgressBar(ProgressBarBase bar)
        {
            bar.BackColor = this.theme.Palette.ProgressBar.Background;
            bar.ForeColor = this.theme.Palette.ProgressBar.Indicator;
        }

        public virtual void AddRules(ControlTheme controlTheme)
        {
            controlTheme.AddRule<HeaderLabel>(StyleHeaderLabel);
            controlTheme.AddRule<PropertyGrid>(c => StylePropertyGrid(c));
            controlTheme.AddRule<TreeView>(c => StyleTreeView(c));
            controlTheme.AddRule<ListView>(c => StyleListView(c));
            controlTheme.AddRule<PropertyGrid>(c => StylePropertyGrid(c));
            controlTheme.AddRule<ToolStrip>(c => StyleToolStrip(c));
            controlTheme.AddRule<Button>(c => StyleButton(c));
            controlTheme.AddRule<DropDownButton>(c => StyleDropDownButton(c));
            controlTheme.AddRule<Label>(c => StyleLabel(c), ControlTheme.Options.IgnoreDerivedTypes);
            controlTheme.AddRule<LinkLabel>(c => StyleLinkLabel(c));
            controlTheme.AddRule<CheckBox>(c => StyleCheckBox(c));
            controlTheme.AddRule<RadioButton>(c => StyleRadioButton(c));
            controlTheme.AddRule<TextBoxBase>(c => StyleTextBox(c));
            controlTheme.AddRule<ComboBox>(c => StyleComboBox(c));
            controlTheme.AddRule<GroupBox>(c => StyleGroupBox(c));
            controlTheme.AddRule<VerticalTabControl>(c => StyleTabControl(c));
            controlTheme.AddRule<PropertiesDialog>(c => StylePropertiesDialog(c));
            controlTheme.AddRule<ProgressBarBase>(c => StyleProgressBar(c));

            var menuTheme = new ToolStripItemTheme(true);
            menuTheme.AddRule(i => StyleToolStripItem(i));
            controlTheme.AddRules(menuTheme);
        }
    }

    /// <summary>
    /// Rule set for main window and dialogs.
    /// </summary>
    internal class VSThemeDialogRuleSet : VSThemeRuleSetBase, ControlTheme.IRuleSet
    {
        public VSThemeDialogRuleSet(VSTheme theme) : base(theme)
        {
        }

        private void StyleDialog(Form form)
        {
            form.BackColor = this.theme.Palette.Window.Background;
        }

        /// <summary>
        /// Register rules.
        /// </summary>
        public override void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ThrowIfNull(nameof(controlTheme));
            controlTheme.AddRule<Form>(c => StyleDialog(c));

            base.AddRules(controlTheme);
        }
    }

    /// <summary>
    /// Rule set for dock windows (main window and tool windows).
    /// </summary>
    internal class VSThemeDockWindowRuleSet : VSThemeRuleSetBase, ControlTheme.IRuleSet
    {
        private void StyleDockWindow(Form form)
        {
            form.BackColor = this.theme.Palette.ToolWindowInnerTabInactive.Background;
        }

        private void StyleFlyoutWindow(FlyoutWindow flyout)
        {
            flyout.BackColor = this.theme.Palette.ToolWindowInnerTabInactive.Background;
            flyout.BorderColor = AccentColor;
        }

        private void StyleDockPanel(DockPanel dockPanel)
        {
            dockPanel.Theme = this.theme;
        }

        private void StyleToolWindow(ToolWindow window, ControlTheme controlTheme)
        {
            if (window.TabPageContextMenuStrip != null)
            {
                //
                // Apply the entire control theme.
                //
                controlTheme.ApplyTo(window.TabPageContextMenuStrip);
            }
        }

        public VSThemeDockWindowRuleSet(VSTheme theme) : base(theme)
        {
        }

        /// <summary>
        /// Register rules.
        /// </summary>
        public override void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ThrowIfNull(nameof(controlTheme));
            controlTheme.AddRule<Form>(c => StyleDockWindow(c));
            controlTheme.AddRule<DockPanel>(c => StyleDockPanel(c));
            controlTheme.AddRule<ToolWindow>(c => StyleToolWindow(c, controlTheme));
            controlTheme.AddRule<FlyoutWindow>(c => StyleFlyoutWindow(c));

            base.AddRules(controlTheme);
        }
    }
}
