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
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using System;
using System.Runtime.InteropServices;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.TunnelsViewer
{
    [ComVisible(false)]
    [Service(typeof(ITunnelsWindow), ServiceLifetime.Singleton)]
    [SkipCodeCoverage("All logic in view model")]
    public partial class TunnelsWindow : ToolWindow, ITunnelsWindow
    {
        private readonly TunnelsViewModel viewModel;

        public TunnelsWindow(IServiceProvider serviceProvider)
            : base(serviceProvider, DockState.DockBottomAutoHide)
        {
            InitializeComponent();

            this.TabText = this.Text;

            this.theme.ApplyTo(this.toolStrip);

            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;

            this.viewModel = new TunnelsViewModel(serviceProvider)
            {
                View = this
            };

            this.tunnelsList.BindCollection(viewModel.Tunnels);
            this.tunnelsList.BindColumn(0, t => t.Destination.Instance.Name);
            this.tunnelsList.BindColumn(1, t => t.Destination.Instance.ProjectId);
            this.tunnelsList.BindColumn(2, t => t.Destination.Instance.Zone);
            this.tunnelsList.BindColumn(3, t => ByteSizeFormatter.Format(t.BytesTransmitted));
            this.tunnelsList.BindColumn(4, t => ByteSizeFormatter.Format(t.BytesReceived));
            this.tunnelsList.BindColumn(5, t => t.LocalPort.ToString());
            this.tunnelsList.BindColumn(6, t => t.Destination.RemotePort.ToString());
            this.tunnelsList.BindColumn(7, t => t.IsMutualTlsEnabled ? "mTLS" : "TLS");

            this.tunnelsList.BindProperty(
                v => this.tunnelsList.SelectedModelItem,
                this.viewModel,
                m => this.viewModel.SelectedTunnel,
                this.components);
            this.refreshToolStripButton.BindProperty(
                b => b.Enabled,
                this.viewModel,
                m => m.IsRefreshButtonEnabled,
                this.components);
            this.disconnectToolStripButton.BindProperty(
                b => b.Enabled,
                this.viewModel,
                m => m.IsDisconnectButtonEnabled,
                this.components);
            this.disconnectTunnelToolStripMenuItem.BindProperty(
                b => b.Enabled,
                this.viewModel,
                m => m.IsDisconnectButtonEnabled,
                this.components);
        }

        //---------------------------------------------------------------------
        // ITunnelsList.
        //---------------------------------------------------------------------

        public override void ShowWindow()
        {
            base.ShowWindow();

            this.viewModel.RefreshTunnels();
        }

        //---------------------------------------------------------------------
        // Window event handlers.
        //---------------------------------------------------------------------

        private async void disconnectToolStripButton_Click(object sender, EventArgs _)
            => await this.viewModel
                .DisconnectSelectedTunnelAsync()
                .ConfigureAwait(true);

        private void refreshToolStripButton_Click(object sender, EventArgs _)
            => this.viewModel.RefreshTunnels();
    }

    public class TunnelsListView : BindableListView<ITunnel>
    { }
}
