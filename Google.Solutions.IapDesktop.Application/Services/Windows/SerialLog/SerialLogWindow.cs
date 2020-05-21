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
using Google.Solutions.Common;
using Google.Solutions.Common.Util;
using Google.Solutions.IapTunneling;
using Google.Solutions.Common.ApiExtensions.Instance;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Windows.SerialLog
{
    [ComVisible(false)]
    public partial class SerialLogWindow : ToolWindow
    {
        private readonly ManualResetEvent keepTailing = new ManualResetEvent(true);
        private volatile bool formClosing = false;

        public VmInstanceReference Instance { get; }

        public SerialLogWindow(VmInstanceReference vmInstance)
        {
            InitializeComponent();

            this.TabText = $"Log ({vmInstance.InstanceName})";
            this.Instance = vmInstance;
        }

        internal void TailSerialPortStream(SerialPortStream stream)
        {
            Task.Run(async () =>
            {
                bool exceptionCaught = false;
                while (!exceptionCaught)
                {
                    // Check if we can continue to tail.
                    this.keepTailing.WaitOne();

                    string newOutput;
                    try
                    {
                        newOutput = await stream.ReadAsync();
                        newOutput = newOutput.Replace("\n", "\r\n");
                    }
                    catch (TokenResponseException e)
                    {
                        newOutput = "Reading from serial port failed - session timed out " +
                            $"({e.Error.ErrorDescription})";
                        exceptionCaught = true;
                    }
                    catch (Exception e)
                    {
                        newOutput = $"Reading from serial port failed: {e.Unwrap().Message}";
                        exceptionCaught = true;
                    }

                    // By the time we read the data, the form might have begun closing. In this
                    // case, updating the UI would cause an exception.
                    if (!this.formClosing)
                    {
                        BeginInvoke((Action)(() => this.log.AppendText(newOutput)));
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            });
        }

        private void SerialPortOutputWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.formClosing = true;
        }

        private void SerialLogWindow_Enter(object sender, EventArgs e)
        {
            // Start tailing (again).
            this.keepTailing.Set();
        }

        private void SerialLogWindow_Leave(object sender, EventArgs e)
        {
            // Pause.
            this.keepTailing.Reset();
        }
    }
}
