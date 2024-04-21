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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials
{
    public interface IShowCredentialsDialog
    {
        void ShowDialog(
            IWin32Window? owner,
            string username,
            string password);
    }

    [Service(typeof(IShowCredentialsDialog))]
    [SkipCodeCoverage("View code")]
    public partial class ShowCredentialsView : Form, IShowCredentialsDialog
    {
        public ShowCredentialsView(IThemeService themeService)
        {
            InitializeComponent();

            themeService.DialogTheme.ApplyTo(this);

            var copyButton = this.passwordText.AddOverlayButton(Resources.Copy_16x);
            copyButton.Click += (s, a) => ClipboardUtil.SetText(this.passwordText.Text);
            copyButton.TabIndex = this.passwordText.TabIndex + 1;
        }

        public void ShowDialog(
            IWin32Window? owner,
            string username,
            string password)
        {
            this.usernameText.Text = username;
            this.passwordText.Text = password;

            ShowDialog(owner);
        }

        //---------------------------------------------------------------------
        // P/Invoke definitions.
        //---------------------------------------------------------------------

        private static class UnsafeNativeMethods
        {
            internal const int EM_SETMARGINS = 0xd3;

            [DllImport("user32.dll")]
            internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        }
    }
}
