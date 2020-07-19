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

using Google.Solutions.IapDesktop.Application.Services.Integration;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Windows.Forms;
using Google.Solutions.IapDesktop.Application.Services.Windows;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Credentials.TunnelsViewer
{
    internal class TunnelsViewModel : ViewModelBase
    {
        private readonly ITunnelBrokerService tunnelBrokerService;
        private readonly IConfirmationDialog confirmationDialog;
        private ITunnel selectedTunnel = null;

        //---------------------------------------------------------------------
        // Properties for data binding.
        //---------------------------------------------------------------------

        public ObservableCollection<ITunnel> Tunnels { get; }
            = new ObservableCollection<ITunnel>();

        public ITunnel SelectedTunnel
        {
            get => this.selectedTunnel;
            set
            {
                this.selectedTunnel = value;

                RaisePropertyChange();
                RaisePropertyChange((TunnelsViewModel m) => m.IsDisconnectButtonEnabled);
            }
        }

        public bool IsDisconnectButtonEnabled => this.selectedTunnel != null;

        //---------------------------------------------------------------------


        public TunnelsViewModel(
            ITunnelBrokerService tunnelBrokerService,
            IConfirmationDialog confirmationDialog,
            IEventService eventService)
        {
            this.tunnelBrokerService = tunnelBrokerService;
            this.confirmationDialog = confirmationDialog;

            // Keep the model up to date.
            eventService.BindHandler<TunnelOpenedEvent>(_ => RefreshTunnels());
            eventService.BindHandler<TunnelClosedEvent>(_ => RefreshTunnels());
        }

        public TunnelsViewModel(IServiceProvider serviceProvider)
            :this(
                serviceProvider.GetService<ITunnelBrokerService>(),
                serviceProvider.GetService<IConfirmationDialog>(),
                serviceProvider.GetService<IEventService>())
        {
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void RefreshTunnels()
        {
            this.Tunnels.Clear();

            foreach (var t in this.tunnelBrokerService.OpenTunnels)
            {
                this.Tunnels.Add(t);
            }
        }

        public async Task DisconnectSelectedTunnelAsync()
        {
            if (this.selectedTunnel == null)
            {
                return;
            }

            if (this.confirmationDialog.Confirm(
                this.View,
                "Are you sure you wish to terminate the tunnel to " +
                    this.selectedTunnel.Destination.Instance + "?",
                "Terminate tunnel") == DialogResult.Yes)
            {
                await this.tunnelBrokerService.DisconnectAsync(this.selectedTunnel.Destination);

                // Reset selection.
                this.SelectedTunnel = null;

                RefreshTunnels();
            }
        }
    }
}
