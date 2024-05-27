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
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Auth
{
    [SkipCodeCoverage("UI")]
    public partial class AccessInfoFlyoutView : FlyoutWindow, IView<AccessInfoViewModel>
    {
        public AccessInfoFlyoutView()
        {
            InitializeComponent();
        }

        public void Bind(
            AccessInfoViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.pscLink.Text = viewModel.PrivateServiceConnectText;
            this.pscLink.LinkClicked += (_, __) => viewModel.OpenPrivateServiceConnectDetails();

            this.dcaLink.Text = viewModel.DeviceCertificateLinkText;
            this.dcaLink.LinkClicked += (_, __) => viewModel.OpenDeviceCertificateDetails(this.FlyoutOwner);

            this.closeButton.Click += (_, __) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.DrawLine(
                SystemPens.ControlDark,
                this.LogicalToDeviceUnits(new Point(0, 28)),
                this.LogicalToDeviceUnits(new Point(this.Width, 28)));
        }
    }
}
