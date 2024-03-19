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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    public partial class WaitDialog : Form, IJobUserFeedback
    {
        private volatile bool formShown = false;
        private bool disposed = false;

        private readonly CancellationTokenSource cancellationSource;
        private readonly IWin32Window parent;

        public WaitDialog(
            IWin32Window parent,
            string message,
            CancellationTokenSource cancellationSource)
        {
            InitializeComponent();

            this.parent = parent;
            this.messageLabel.Text = message;
            this.cancellationSource = cancellationSource;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            try
            {
                this.cancellationSource.Cancel();
            }
            catch
            {
                //
                // Canceallation may fail if the task has been completed
                // or cancelled already.
                //
            }

            Close();
        }

        private void WaitDialog_Shown(object sender, EventArgs e)
        {
            this.formShown = true;
        }

        //---------------------------------------------------------------------
        // IJobUserFeedback.
        //---------------------------------------------------------------------

        public bool IsShowing => this.formShown;

        public void Start()
        {
            Debug.Assert(!this.InvokeRequired, "Start must be called on UI thread");
            Debug.Assert(!this.disposed);

            ShowDialog(this.parent);
        }

        public void Finish()
        {
            Debug.Assert(!this.InvokeRequired, "Finish must be called on UI thread");
            this.disposed = true;
            Close();
        }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

        /// <summary>
        /// Show dialog until a task completes, and propagates any exceptions
        /// (incl. TaskCancellationExceptions).
        /// </summary>
        public static void Wait(
            IWin32Window parent,
            string message,
            Func<CancellationToken, Task> asyncFunc)
        {
            using (var tokenSource = new CancellationTokenSource())
            using (var dialog = new WaitDialog(
                parent,
                message,
                tokenSource)
            {
                StartPosition = parent == null
                    ? FormStartPosition.CenterScreen
                    : FormStartPosition.CenterParent
            })
            {
                //
                // NB. Because we're forcing the callback to run
                // on the window thread, the callback won't run
                // after Start() was called.
                //
                Exception? exception = null;
                var tx = asyncFunc(tokenSource.Token).ContinueWith(
                    t =>
                    {
                        dialog.Finish();

                        if (t.IsFaulted)
                        {
                            exception = t.Exception;
                        }
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());

                if (dialog.ShowDialog() == DialogResult.Cancel && tokenSource.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }

                if (exception != null)
                {
                    throw exception;
                }
            }
        }
    }
}
