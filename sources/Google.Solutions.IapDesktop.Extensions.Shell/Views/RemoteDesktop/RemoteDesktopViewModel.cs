//
// Copyright 2023 Google LLC
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

using Google.Solutions.Common.Util;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.Mvvm.Binding;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop
{
    [Service]
    public class RemoteDesktopViewModel : ViewModelBase
    {
        public enum ConnectionState
        {
            Uninitialized,
            Connecting,
            Connected,
            LoggedOn,
            ConnectionLost
        }

        public RemoteDesktopViewModel()
        {
            this.State = ObservableProperty.Build(ConnectionState.Uninitialized);

#if DEBUG
            this.DiagnosticsStatusText = ObservableProperty.Build(
                this.State, 
                s => s.ToString() + "\n\n" + this.Parameters.DumpProperties());
#endif
        }

        //---------------------------------------------------------------------
        // Initialization properties.
        //---------------------------------------------------------------------

        public InstanceLocator Instance { get; set; }
        public string Server { get; set; }
        public ushort Port { get; set; }
        public RdpSessionParameters Parameters { get; set; }

        protected override void OnValidate()
        {
            this.Instance.ExpectNotNull(nameof(this.Instance));
            this.Server.ExpectNotNull(nameof(this.Server));
            this.Parameters.ExpectNotNull(nameof(this.Parameters));
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<ConnectionState> State { get; }

#if DEBUG
        public ObservableFunc<string> DiagnosticsStatusText { get; }
#endif
    }
}
