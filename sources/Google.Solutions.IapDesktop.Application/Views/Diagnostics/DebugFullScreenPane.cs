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
using System;
using System.Runtime.InteropServices;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views.Diagnostics
{
    [ComVisible(false)]
    [SkipCodeCoverage("For debug purposes only")]
    public partial class DebugFullScreenPane : DocumentWindow
    {
        public DebugFullScreenPane()
        {
            // Constructor is for designer only.
        }

        public DebugFullScreenPane(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            InitializeComponent();
            this.TabText = this.Text;

            this.DockAreas = DockAreas.Document;
            this.HideOnClose = true;
        }

        private void fullScreenToggleButton_Click(object sender, EventArgs e)
        {
            if (base.IsFullscreen)
            {
                LeaveFullScreen();
            }
            else
            {
                EnterFullscreen(this.allScreensCheckBox.Checked);
            }
        }
    }
}
