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

using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Windows
{
    public partial class ErrorDialog : Form
    {
        private readonly BugReport report;

        public ErrorDialog(Exception exception)
        {
            InitializeComponent();

            this.report = new BugReport(GetType(), exception);

            this.errorText.Text = this.report.ToString().Replace("\n", "\r\n");
            this.errorText.SelectionStart = 0;
            this.errorText.SelectionLength = 0;
        }

        public static void Show(Exception e)
        {
            new ErrorDialog(e).ShowDialog();
        }

        private void reportButton_Click(object sender, EventArgs e)
        {
            new BugReportClient().ReportBug(this.report);
        }
    }
}
