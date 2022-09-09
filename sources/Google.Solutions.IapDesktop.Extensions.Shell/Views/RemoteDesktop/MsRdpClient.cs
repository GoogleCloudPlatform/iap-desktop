//
// Copyright 2021 Google LLC
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

using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop
{
    internal class MsRdpClient : AxMSTSCLib.AxMsRdpClient9NotSafeForScripting
    {
        private const int WM_MOUSEACTIVATE = 0x0021;

        protected override void WndProc(ref Message m)
        {
            //
            // When the RDP control loses focus and you later click into the
            // control again, the control does not re-gain focus. Fix this
            // default behavior by forcing the focus back on the control 
            // whenever we see a WM_MOUSEACTIVATE message.
            //
            // Cf. https://www.codeproject.com/Tips/109917/Fix-the-focus-issue-on-RDP-Client-from-the-AxInter
            //
            // NB. Only do this when the control does not have focus yet, 
            // otherwise we're interfering with operations such as dragging 
            // or resizing windows.
            //
            if (!this.ContainsFocus && m.Msg == WM_MOUSEACTIVATE)
            {
                this.Focus();
            }

            base.WndProc(ref m);
        }
    }
}
