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

using Google.Apis.Auth.OAuth2.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.CloudIap.Plugin.Gui
{
    public partial class WaitDialog : Form
    {
        private int ticksElapsed = 0;
        private double seriesValue = 0;
        private volatile bool formShown = false;

        public WaitDialog()
        {
            InitializeComponent();
            this.timer.Start();
        }

        public CancellationTokenSource CancellationToken { get; set; } = new CancellationTokenSource();

        public string Message
        {
            get { return this.messageLabel.Text; }
            set {
                this.Text = value;
                this.messageLabel.Text = value;
            }
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
            this.CancellationToken.Cancel();
            this.Close();
        }

        private void WaitDialog_Shown(object sender, EventArgs e)
        {
            this.formShown = true;
        }

        public static WaitDialog ForMessage(string message)
        {
            return new WaitDialog()
            {
                Message = message
            };
        }

        public static void Run<T>(
            Control parent,
            string message,
            Func<Task<T>> slowFunc,
            Action<T> updateGuiFunc)
        {
            Exception exception = null;
            using (var waitDialog = WaitDialog.ForMessage(message))
            {
                Task.Run(() =>
                {
                    try
                    {
                        T result = slowFunc().Result;

                        // If the function finished fast, it is possible that the dialog
                        // has not even been shown yet - that would cause BeginInvoke
                        // to fail.

                        while (!waitDialog.formShown)
                        {
                            Thread.Sleep(10);
                        }

                        if (!waitDialog.CancellationToken.IsCancellationRequested)
                        {
                            // Close the dialog immediately...
                            waitDialog.Invoke((Action)(() =>
                            {
                                waitDialog.Close();
                            }));

                            // ...then run the GUI function. If this function opens
                            // another dialog, the two dialogs will not overlap.
                            parent.BeginInvoke((Action)(() =>
                            {
                                updateGuiFunc(result);
                            }));
                        }
                    }
                    catch (Exception e)
                    {
                        exception = e;
                        parent.BeginInvoke((Action)(() =>
                        {
                            if (!waitDialog.CancellationToken.IsCancellationRequested)
                            {
                                waitDialog.Close();
                            }
                        }));
                    }
                });

                waitDialog.ShowDialog(parent);

                if (exception != null)
                {
                    throw ExceptionUtil.Unwrap(exception);
                }
            }
        }

        public static void RunWithDialog<T>(
            Control parent,
            string message,
            Func<Task<T>> slowFunc,
            Action<T> updateGuiFunc,
            Func<Task> reauthorize)
        {
            try
            {
                bool reauthAndRetry;
                do
                {
                    reauthAndRetry = false;

                    try
                    {
                        Run(parent, message, slowFunc, updateGuiFunc);
                    }
                    catch (TokenResponseException tokenException) 
                        when (tokenException.Error.Error == "invalid_grant")
                    {
                        // Reauth required or authorization has been revoked.
                        if (MessageBox.Show(
                            parent,
                            "Your session has expired or the authorization has been revoked. "+
                            "Do you want to sign in again?",
                            "Authorization required",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            // Reauthorize. This might take a while since the user has to use 
                            // a browser - show the WaitDialog in the meantime.
                            Run<T>(
                                parent, 
                                "Authorizing", 
                                async () => 
                                {
                                    await reauthorize();
                                    return default(T);
                                },
                                _ => 
                                {
                                    // No GUI update required.
                                });
                            reauthAndRetry = true;
                        }
                    }
                }
                while (reauthAndRetry);
            }
            catch (Exception e)
            {
                ExceptionUtil.HandleException(parent, message, e);
            }
        }
    }
}
