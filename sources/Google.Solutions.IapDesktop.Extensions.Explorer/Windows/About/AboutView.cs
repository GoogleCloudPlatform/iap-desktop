//
// Copyright 2020 Google LLC
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
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Explorer.Windows.About
{
    [SkipCodeCoverage("UI code")]
    [Service]
    public partial class AboutView : CompositeForm, IView<AboutViewModel>
    {

        public static Version ProgramVersion => typeof(AboutView).Assembly.GetName().Version;

        public AboutView()
        {
            InitializeComponent();

            this.SuspendLayout();
            this.licenseText.TextPadding = 5;
            this.licenseText.Fonts.Text = new FontFamily("Segoe UI");
            this.licenseText.Fonts.FontSize = 7;
            this.licenseText.Fonts.FontSizeHeading1 = 11;
            this.licenseText.Fonts.FontSizeHeading2 = 8;
            this.licenseText.Fonts.FontSizeHeading3 = 8;
            this.licenseText.Fonts.FontSizeHeading4 = 8;
            this.licenseText.Fonts.FontSizeHeading5 = 8;
            this.licenseText.Fonts.FontSizeHeading6 = 8;
            this.ResumeLayout();
        }

        public void Bind(AboutViewModel viewModel, IBindingContext bindingContext)
        {
            this.infoLabel.BindReadonlyProperty(
                c => c.Text,
                viewModel,
                m => m.Information,
                bindingContext);
            this.copyrightLabel.BindReadonlyProperty(
                c => c.Text,
                viewModel,
                m => m.Copyright,
                bindingContext);
            this.authorLink.BindReadonlyProperty(
                c => c.Text,
                viewModel,
                m => m.AuthorText,
                bindingContext);
            this.licenseText.BindReadonlyProperty(
                c => c.Markdown,
                viewModel,
                m => m.LicenseText,
                bindingContext);

            this.authorLink.LinkClicked += (sender, args) =>
            {
                using (Process.Start(viewModel.AuthorLink))
                { }
            };
        }

        private void licenseText_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            using (Process.Start(e.LinkText))
            { }
        }
    }
}
