//
// Copyright 2024 Google LLC
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
using System.Drawing;
using System.Windows.Forms;
using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Terminal.Controls;

namespace Google.Solutions.Terminal.TestApp
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] argv)
        {
            using (var form = new ClientDiagnosticsWindow<PowerShellClient>(
                new PowerShellClient()))
            {
                Application.Run(form);
            }

            //using (var f = new Form()
            //{
            //    Width = 800,
            //    Height = 600,
            //    Text = "Terminal TestApp"
            //})
            //{
            //    var control = new VirtualTerminal()
            //    {
            //        Dock = DockStyle.Fill,
            //        ForeColor = Color.LightGray,
            //        BackColor = Color.Black
            //    };

            //    f.Controls.Add(control);


            //    control.DeviceClosed += (_, __) => f.Close();
            //    control.DeviceError += (_, args) => MessageBox.Show(f, args.Exception.Message);

            //    IWin32Process? process = null;

            //    f.Shown += (_, __) =>
            //    {
            //        //
            //        // NB. We must initialize the pseudo-terminal with
            //        // the right dimensions. Now that the window has been
            //        // shown, we know these.
            //        //
            //        Debug.Assert(control.Dimensions.Width > 0);
            //        Debug.Assert(control.Dimensions.Height > 0);

            //        var processFactory = new Win32ProcessFactory();
            //        process = processFactory.CreateProcessWithPseudoConsole(
            //            "powershell.exe",
            //            null,
            //            control.Dimensions);

            //        process.PseudoTerminal!.OutputAvailable += (_, args) =>
            //        {
            //            Debug.WriteLine($"PTY: {args.Data}");
            //        };

            //        control.Device = process.PseudoTerminal;

            //        process.Resume();
            //    };

            //    control.Output += (_, args) =>
            //    {
            //        Debug.WriteLine("Out: " + args.Data);
            //    };
            //    control.UserInput += (_, args) =>
            //    {
            //        Debug.WriteLine("In: " + args.Data);
            //    };


            //    Application.Run(f);

            //    process?.Dispose();
            //}
        }
    }
}