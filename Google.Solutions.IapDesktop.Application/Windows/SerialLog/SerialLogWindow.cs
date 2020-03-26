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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Compute;
using Google.Solutions.Compute.Extensions;

namespace Google.Solutions.IapDesktop.Application.Windows.SerialLog
{
    public partial class SerialLogWindow : ToolWindow
    {
        private volatile bool keepTailing = true;
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
                while (this.keepTailing && !exceptionCaught)
                {
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

                    if (this.keepTailing)
                    { 
                        BeginInvoke((Action)(() => this.log.AppendText(newOutput)));
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            });
        }

        private void SerialPortOutputWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.keepTailing = false;
        }
    }
}
