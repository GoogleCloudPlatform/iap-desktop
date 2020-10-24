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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    public class NetworkOptionsViewModel : ViewModelBase, IOptionsDialogPane
    {

        private bool isDirty = false;

        public NetworkOptionsViewModel(IServiceProvider serviceProvider)
        {
            // TODO
        }

        //---------------------------------------------------------------------
        // IOptionsDialogPane.
        //---------------------------------------------------------------------

        public string Title => "Network";

        public UserControl CreateControl() => new NetworkOptionsControl(this);

        public bool IsDirty
        {
            get => this.isDirty;
            set
            {
                this.isDirty = value;
                RaisePropertyChange();
            }
        }

        public void ApplyChanges()
        {
            Debug.Assert(this.IsDirty);

            // TODO

            this.IsDirty = false;
        }


        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsCustomProxyServerEnabled => false; // TODO

        public bool IsSystemProxyServerEnabled => true; // TODO

        public string ProxyServer => null; // TODO
        public string ProxyPort => null; // TODO

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public bool IsValidProxyPort(string port)
            => int.TryParse(port, out int portNumber) &&
                portNumber > 0 &&
                portNumber <= ushort.MaxValue;

        public void OpenProxyControlPanelApplet()
        {

            using (Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = false,
                Verb = "open",
                FileName = "rundll32.exe",
                Arguments = "shell32.dll,Control_RunDLL inetcpl.cpl,,4"
            }))
            { };
        }
    }
}
