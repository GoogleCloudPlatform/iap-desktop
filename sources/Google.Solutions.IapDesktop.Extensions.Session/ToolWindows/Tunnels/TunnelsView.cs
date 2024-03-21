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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Format;
using System;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Tunnels
{
    [Service(ServiceLifetime.Singleton)]
    [SkipCodeCoverage("All logic in view model")]
    public partial class TunnelsView : ToolWindowViewBase, IView<TunnelsViewModel>
    {
        public TunnelsView(
            IMainWindow mainWindow,
            ToolWindowStateRepository stateRepository)
            : base(mainWindow, stateRepository, DockState.DockBottomAutoHide)
        {
            InitializeComponent();

            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;
        }

        public void Bind(TunnelsViewModel viewModel, IBindingContext bindingContext)
        {
            this.tunnelsList.BindCollection(viewModel.Tunnels);
            this.tunnelsList.BindColumn(0, t => t.TargetInstance.Name);
            this.tunnelsList.BindColumn(1, t => t.TargetInstance.ProjectId);
            this.tunnelsList.BindColumn(2, t => t.TargetInstance.Zone);
            this.tunnelsList.BindColumn(3, t => ByteSizeFormatter.Format(t.Statistics.BytesTransmitted));
            this.tunnelsList.BindColumn(4, t => ByteSizeFormatter.Format(t.Statistics.BytesReceived));
            this.tunnelsList.BindColumn(5, t => t.LocalEndpoint.ToString());
            this.tunnelsList.BindColumn(6, t => t.TargetPort.ToString());
            this.tunnelsList.BindColumn(7, t => t.Protocol.Name);
            this.tunnelsList.BindColumn(8, t => t.Flags.HasFlag(IapTunnelFlags.Mtls) ? "mTLS" : "TLS");
            this.tunnelsList.BindColumn(9, t => t.Policy.Name);

            this.tunnelsList.BindProperty(
                v => this.tunnelsList.SelectedModelItem,
                viewModel,
                m => viewModel.SelectedTunnel,
                bindingContext);
            this.refreshToolStripButton.BindReadonlyProperty(
                b => b.Enabled,
                viewModel,
                m => m.IsRefreshButtonEnabled,
                bindingContext);
            this.refreshToolStripButton.Click += (_, __) => viewModel.RefreshTunnels();


            viewModel.RefreshTunnels();
        }
    }

    public class TunnelsListView : BindableListView<IIapTunnel>
    { }
}
