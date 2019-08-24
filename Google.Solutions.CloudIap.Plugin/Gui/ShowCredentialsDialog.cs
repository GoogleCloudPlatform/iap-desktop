//
// Copyright 2019 Google LLC
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

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.CloudIap.Plugin.Gui
{
    public partial class ShowCredentialsDialog : Form
    {
        private const int EM_SETMARGINS = 0xd3;
        [System.Runtime.InteropServices.DllImport("user32.dll")]

        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        public ShowCredentialsDialog()
        {
            InitializeComponent();
        }

        private void ShowCredentialsDialog_Load(object sender, System.EventArgs e)
        {
            var copyPasswordButton = new Button();
            copyPasswordButton.Size = new Size(25, passwordText.ClientSize.Height + 2);
            copyPasswordButton.Location = new Point(passwordText.ClientSize.Width - copyPasswordButton.Width, -1);
            copyPasswordButton.Image = Resources.Copy_16x;
            copyPasswordButton.Cursor = Cursors.Default;
            copyPasswordButton.Click += (s, a) => { Clipboard.SetText(passwordText.Text); };
            passwordText.Controls.Add(copyPasswordButton);

            // Send EM_SETMARGINS to prevent text from disappearing underneath the button
            SendMessage(passwordText.Handle, EM_SETMARGINS, (IntPtr)2, (IntPtr)(copyPasswordButton.Width << 16));
        }

        internal static void ShowDialog(
            IWin32Window owner,
            string username,
            string password)
        {
            var dialog = new ShowCredentialsDialog();
            dialog.usernameText.Text = username;
            dialog.passwordText.Text = password;

            dialog.ShowDialog(owner);
        }
    }
}
