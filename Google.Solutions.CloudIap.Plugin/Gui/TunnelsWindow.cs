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

using Google.Solutions.CloudIap.Plugin.Integration;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.CloudIap.Plugin.Gui
{
    public partial class TunnelsWindow : Form
    {
        private TunnelManagerBase TunnelManager { get; set; }

        public TunnelsWindow()
        {
            InitializeComponent();
        }

        private void RefreshTunnels()
        {
            this.tunnelsList.Items.Clear();

            foreach (var tunnel in this.TunnelManager.OpenTunnels)
            {
                ListViewItem item = new ListViewItem(new string[] {
                    tunnel.Endpoint.Instance.InstanceName,
                    tunnel.Endpoint.Instance.ProjectId,
                    tunnel.Endpoint.Instance.Zone,
                    tunnel.LocalPort.ToString(),
                    tunnel.ProcessId != null ? tunnel.ProcessId.ToString() : string.Empty
                });
                item.Tag = tunnel;
                this.tunnelsList.Items.Add(item);
            }
        }

        private void tunnelsList_SelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            this.terminateTunnelButton.Enabled = this.tunnelsList.SelectedIndices.Count > 0;
        }

        private void terminateTunnelButton_Click(object sender, EventArgs eventArgse)
        {
            var selectedItem = this.tunnelsList.SelectedItems
                .Cast<ListViewItem>()
                .FirstOrDefault();
            if (selectedItem == null)
            {
                return;
            }

            var selectedTunnel = (ITunnel)selectedItem.Tag;

            if (MessageBox.Show(
                this,
                $"Are you sure you wish to terminate the tunnel to {selectedTunnel.Endpoint}",
                "Terminate tunnel",
                MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
            {
                try
                {
                    this.TunnelManager.CloseTunnel(selectedTunnel.Endpoint);
                    RefreshTunnels();
                }
                catch (Exception e)
                {
                    ExceptionUtil.HandleException(this, "Terminating tunnel failed", e);
                }
            }
        }

        internal static void ShowDialog(IWin32Window owner, TunnelManagerBase tunnelManager)
        {
            var window = new TunnelsWindow()
            {
                TunnelManager = tunnelManager
            };
            window.RefreshTunnels();
            window.ShowDialog(owner);
        }
    }
}
