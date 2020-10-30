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
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Authentication
{
    [SkipCodeCoverage("UI")]
    public partial class DeviceFlyoutWindow : FlyoutWindow
    {
        private readonly DeviceFlyoutViewModel viewModel;

        public DeviceFlyoutWindow(DeviceFlyoutViewModel viewModel) : base()
        {
            this.viewModel = viewModel; 
            
            InitializeComponent();

            this.enrollmentStateLabel.Text = this.viewModel.EnrollmentStateDescription;
            this.detailsLink.Visible = this.viewModel.IsDetailsLinkVisible;
            this.detailsLink.Text = this.viewModel.DetailsLinkCaption;

            this.deviceEnrolledIcon.Visible = this.viewModel.IsDeviceEnrolledIconVisible;
            this.deviceNotEnrolledIcon.Visible = this.viewModel.IsDeviceNotEnrolledIconVisible;
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void detailsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => this.viewModel.OpenDetails();

        private void closeButton_Click(object sender, EventArgs e)
            => Close();
    }
}
