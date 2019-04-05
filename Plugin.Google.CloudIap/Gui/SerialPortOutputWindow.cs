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
using Plugin.Google.CloudIap.Integration;

namespace Plugin.Google.CloudIap.Gui
{
    public partial class SerialPortOutputWindow : Form
    {
        private volatile bool keepTailing = true;

        public SerialPortOutputWindow()
        {
            InitializeComponent();

            this.Icon = Icon.FromHandle(Resources.ActionLog.WithMagentaAsTransparent().GetHicon());
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        internal void TailSerialPortStream(ComputeEngineAdapter.SerialPortStream stream)
        {
            Task.Run(async () =>
            {
                while (this.keepTailing)
                {
                    var newOutput = await stream.ReadAsync();

                    newOutput = newOutput.Replace("\n", "\r\n");

                    if (this.keepTailing)
                    { 
                        BeginInvoke((Action)(() => this.log.AppendText(newOutput)));
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            });
        }

        internal static void Show(IWin32Window owner, ComputeEngineAdapter.SerialPortStream stream)
        {
            var window = new SerialPortOutputWindow();
            window.Text += $": {stream.Instance.InstanceName} ({stream.Instance.Zone})";
            window.TailSerialPortStream(stream);
            window.Show(owner);
            window.Activate();
        }

        private void SerialPortOutputWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.keepTailing = false;
        }
    }
}
