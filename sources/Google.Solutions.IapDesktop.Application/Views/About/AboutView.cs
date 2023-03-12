﻿//
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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.About
{
    [SkipCodeCoverage("UI code")]
    [Service]
    public partial class AboutView : Form, IView<AboutViewModel>
    {

        public static Version ProgramVersion => typeof(AboutView).Assembly.GetName().Version;

        public AboutView()
        {
            InitializeComponent();
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
                c => c.Rtf,
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
