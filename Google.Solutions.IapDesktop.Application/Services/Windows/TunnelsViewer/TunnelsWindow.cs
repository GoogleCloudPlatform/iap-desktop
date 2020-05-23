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

using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using System;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Application.Services.Windows.TunnelsViewer
{
    [ComVisible(false)]
    public partial class TunnelsWindow : ToolWindow, ITunnelsViewer
    {
        private readonly DockPanel dockPanel;
        private readonly IExceptionDialog exceptionDialog;
        private readonly TunnelsViewModel viewModel;

        public TunnelsWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
            this.TabText = this.Text;

            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();


            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;

            this.viewModel = new TunnelsViewModel(
                serviceProvider.GetService<ITunnelBrokerService>(),
                serviceProvider.GetService<IEventService>());

            this.tunnelsList.Model = viewModel.Tunnels;
            this.tunnelsList.BindColumn(0, t => t.Destination.Instance.InstanceName);
            this.tunnelsList.BindColumn(1, t => t.Destination.Instance.ProjectId);
            this.tunnelsList.BindColumn(2, t => t.Destination.Instance.Zone);
            this.tunnelsList.BindColumn(3, t => t.LocalPort.ToString());
        }

        //---------------------------------------------------------------------
        // ITunnelsList.
        //---------------------------------------------------------------------

        public void ShowWindow()
        {
            ShowOrActivate(this.dockPanel, DockState.DockBottomAutoHide);

            this.viewModel.RefreshTunnels();
        }

        //---------------------------------------------------------------------
        // Window event handlers.
        //---------------------------------------------------------------------

        private void tunnelsList_SelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            // TODO: Solve via binding.
            this.disconnectToolStripButton.Enabled =
                this.disconnectTunnelToolStripMenuItem.Enabled =
                this.tunnelsList.SelectedIndices.Count > 0;

            this.viewModel.SelectedTunnel = (Tunnel)this.tunnelsList.SelectedItems
                .Cast<ListViewItem>()
                .FirstOrDefault()?.Tag;
        }

        private async void disconnectToolStripButton_Click(object sender, EventArgs eventArgse)
        {
            if (this.viewModel.SelectedTunnel != null)
            {
                if (MessageBox.Show(
                    this,
                    "Are you sure you wish to terminate the tunnel to " + 
                        this.viewModel.SelectedTunnel.Destination.Instance + "?",
                    "Terminate tunnel",
                    MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
                {
                    try
                    {
                        await this.viewModel.DisconnectActiveTunnel();
                    }
                    catch (Exception e)
                    {
                        this.exceptionDialog.Show(this, "Terminating tunnel failed", e);
                    }
                }
            }
        }
    }

    public class TunnelsListView : BindableListView<Tunnel>
    { }
}
