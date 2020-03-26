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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Windows;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Windows.TunnelsViewer
{
    public partial class TunnelsWindow : ToolWindow, ITunnelsViewer
    {
        private readonly DockPanel dockPanel;
        private readonly TunnelBrokerService tunnelBrokerService;
        private readonly IExceptionDialog exceptionDialog;

        public TunnelsWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
            this.tunnelBrokerService = serviceProvider.GetService<TunnelBrokerService>();
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();

            this.TabText = this.Text;

            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;
        }

        private void RefreshTunnels()
        {
            this.tunnelsList.Items.Clear();

            foreach (var tunnel in this.tunnelBrokerService.OpenTunnels)
            {
                ListViewItem item = new ListViewItem(new string[] {
                    tunnel.Destination.Instance.InstanceName,
                    tunnel.Destination.Instance.ProjectId,
                    tunnel.Destination.Instance.Zone,
                    tunnel.LocalPort.ToString(),
                    tunnel.ProcessId != null ? tunnel.ProcessId.ToString() : string.Empty
                });
                item.Tag = tunnel;
                this.tunnelsList.Items.Add(item);
            }
        }


        //---------------------------------------------------------------------
        // ITunnelsList.
        //---------------------------------------------------------------------

        public void ShowWindow()
        {
            Show(this.dockPanel, DockState.DockBottomAutoHide);
            this.dockPanel.ActiveAutoHideContent = this;
            Activate();

            RefreshTunnels();
        }

        //---------------------------------------------------------------------
        // Window event handlers.
        //---------------------------------------------------------------------

        private void tunnelsList_SelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            this.terminateToolStripButton.Enabled = this.tunnelsList.SelectedIndices.Count > 0;
        }

        private void terminateToolStripButton_Click(object sender, EventArgs eventArgse)
        {
            var selectedItem = this.tunnelsList.SelectedItems
                .Cast<ListViewItem>()
                .FirstOrDefault();
            if (selectedItem == null)
            {
                return;
            }

            var selectedTunnel = (Tunnel)selectedItem.Tag;

            if (MessageBox.Show(
                this,
                $"Are you sure you wish to terminate the tunnel to {selectedTunnel.Destination.Instance}",
                "Terminate tunnel",
                MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
            {
                try
                {
                    this.tunnelBrokerService.CloseTunnel(selectedTunnel.Destination);
                    RefreshTunnels();
                }
                catch (Exception e)
                {
                    this.exceptionDialog.Show(this, "Terminating tunnel failed", e);
                }
            }
        }
    }
}
