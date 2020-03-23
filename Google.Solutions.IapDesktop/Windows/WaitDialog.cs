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
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Windows
{
    public partial class WaitDialog : Form
    {
        private int ticksElapsed = 0;
        private double seriesValue = 0;
        private volatile bool formShown = false;

        private readonly CancellationTokenSource cancellationSource;

        public bool IsShowing => this.formShown;
        
        public WaitDialog() : this(null, null)
        {
            // For Designer only.
        }

        public WaitDialog(string message, CancellationTokenSource cancellationSource)
        {
            InitializeComponent();

            this.messageLabel.Text = message;
            this.cancellationSource = cancellationSource;

            this.timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            this.ticksElapsed++;

            // We do not know how long this operation is actually going
            // to take. So show a fake progress bar that tracks a series
            // that converges to 100% but gets increasingly slower.
            this.seriesValue += 1.0 / (1 + Math.Pow(2, this.ticksElapsed));
            this.progressBar.Value = (int) (this.seriesValue * 100);
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.cancellationSource.Cancel();
            this.Close();
        }

        private void WaitDialog_Shown(object sender, EventArgs e)
        {
            this.formShown = true;
        }
    }
}
