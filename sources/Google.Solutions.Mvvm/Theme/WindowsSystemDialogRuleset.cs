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

using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Controls;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for system dialogs.
    /// </summary>
    public class WindowsSystemDialogRuleset : ControlTheme.IRuleSet
    {
        private readonly FontFamily fontFamily;
        private readonly Font labelFont;
        private readonly Font headerFont;

        public WindowsSystemDialogRuleset()
        {
            this.fontFamily = new FontFamily("Segoe UI");
            this.labelFont = new Font(this.fontFamily, 9.75f);
            this.headerFont = new Font(this.fontFamily, 15f);
        }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

        public static Color AccentColor
        {
            get
            {
                var hr = NativeMethods.DwmGetColorizationColor(
                    out var colorization,
                    out var opaqueBlend);
                if (hr.Failed())
                {
                    return SystemColors.ActiveBorder;
                }

                colorization |= (opaqueBlend ? 0xFF000000 : 0);
                return Color.FromArgb((int)colorization);
            }
        }

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        private void StyleForm(Form form)
        {
            form.BackColor = SystemColors.ControlLight; // TODO: Dark mode
            form.FormBorderStyle = FormBorderStyle.None;
            form.Controls.Add(new Label()
            {
                Text = form.Text,
                Location = new Point(16, 8)
            });

            form.Paint += (sender, e) =>
            {
                //
                // Draw a border in the Windows accent color.
                //
                using (var pen = new Pen(new SolidBrush(AccentColor), 1f))
                {
                    e.Graphics.DrawRectangle(
                        pen,
                        new Rectangle(
                            Point.Empty,
                            new Size(form.Width - 2, form.Height - 2)));
                }
            };
        }

        private void StyleButton(Button button)
        {
            button.BackColor = SystemColors.ControlDark;
            button.Font = this.labelFont;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;

            //button.Font.Size = 10f;
            // TODO: Set colors, border
        }

        private void StyleTextbox(TextBox text)
        {
            // TODO: Set margins, border
        }

        private void StyleLabel(Label text)
        {
            text.Font = this.labelFont;
            // TODO: Set font, size
        }

        private void StyleHeaderLabel(HeaderLabel text)
        {
            text.Font = this.headerFont;
            // TODO: Set font, size
        }

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        /// <summary>
        /// Register rules.
        /// </summary>
        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            controlTheme.AddRule<Form>(c => StyleForm(c));
            controlTheme.AddRule<Button>(c => StyleButton(c));
            controlTheme.AddRule<TextBox>(c => StyleTextbox(c));
            controlTheme.AddRule<Label>(c => StyleLabel(c));
            controlTheme.AddRule<HeaderLabel>(c => StyleHeaderLabel(c));
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("dwmapi.dll")]
            public static extern HRESULT DwmGetColorizationColor(
                out uint pcrColorization,
                [MarshalAs(UnmanagedType.Bool)] out bool pfOpaqueBlend);
        }
    }
}
