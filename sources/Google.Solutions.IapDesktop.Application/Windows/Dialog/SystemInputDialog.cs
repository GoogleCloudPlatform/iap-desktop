//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Dialog
{
    /// <summary>
    /// System-style input dialog.
    /// </summary>
    internal class SystemInputDialog : CompositeForm
    {
        private static void DefaultValidateInput(
            string input,
            out bool valid,
            out string warning)
        {
            valid = !string.IsNullOrEmpty(input);
            warning = null;
        }

        private ValidateInputCallback validateInput = DefaultValidateInput;

        /// <summary>
        /// Value provided by user.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Validation callback, optional.
        /// </summary>
        public ValidateInputCallback ValidateInput
        {
            get => this.validateInput;
            set
            {
                value.ExpectNotNull(nameof(value));
                this.validateInput = value;
            }
        }

        public SystemInputDialog(string title, string caption, string message)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = SizeGripStyle.Hide;

            this.Size = new Size(450, 225);

            //
            // Header and description.
            //
            this.Controls.Add(new Label()
            {
                Text = title,
                Location = new Point(24, 12),
                AutoSize = false,
                Size = new Size(this.Width - 50, 20),
            });
            this.Controls.Add(new HeaderLabel()
            {
                Text = caption,
                Location = new Point(24 - 2, 40),
                AutoSize = false,
                Size = new Size(this.Width - 50, 30),
            });
            this.Controls.Add(new Label()
            {
                Text = message,
                Location = new Point(24, 80),
                AutoSize = false,
                Size = new Size(this.Width - 50, 20),
            });

            //
            // Buttons.
            //
            var okButton = new Button()
            {
                DialogResult = DialogResult.OK,
                Location = new Point(24, 168),
                Size = new Size(200, 30),
                Text = "OK",
                Enabled = false,
                TabIndex = 1
            };
            this.Controls.Add(okButton);
            this.AcceptButton = okButton;

            var cancelButton = new Button()
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(230, 168),
                Size = new Size(200, 30),
                Text = "Cancel",
                TabIndex = 2
            };
            this.Controls.Add(cancelButton);
            this.CancelButton = cancelButton;

            //
            // Username.
            //
            var usernameTextBox = new TextBox()
            {
                Location = new Point(24, 112),
                Size = new Size(296, 30),
                TabIndex = 0,
                MaxLength = 64,
            };
            this.Controls.Add(usernameTextBox);

            var warningLabel = new Label()
            {
                Location = new Point(24, 136),
                Size = new Size(296, 20),
                AutoSize = false,
                ForeColor = Color.Red
            };
            this.Controls.Add(warningLabel);

            usernameTextBox.HandleCreated += (_, __) =>
            {
                usernameTextBox.SetCueBanner("User name", true);
            };

            usernameTextBox.TextChanged += (_, __) =>
            {
                this.Value = usernameTextBox.Text;

                this.validateInput(usernameTextBox.Text, out var valid, out var warning);

                okButton.Enabled = valid;
                warningLabel.Visible = !valid;
                warningLabel.Text = warning ?? string.Empty;
            };
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left &&
                e.Y <= NativeMethods.GetSystemMetrics(NativeMethods.SystemMetric.SM_CYCAPTION))
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(
                    this.Handle, 
                    NativeMethods.WM_NCLBUTTONDOWN, 
                    NativeMethods.HT_CAPTION, 0);
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            public const int WM_NCLBUTTONDOWN = 0xA1;
            public const int HT_CAPTION = 0x2;

            [DllImport("user32.dll")]
            public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

            [DllImport("user32.dll")]
            public static extern bool ReleaseCapture();


            [DllImport("user32.dll")]
            public static extern int GetSystemMetrics(SystemMetric smIndex);

            public enum SystemMetric
            {
                /// <summary>
                /// The height of a caption area, in pixels.
                /// </summary>
                SM_CYCAPTION = 4
            }
        }
    }
}