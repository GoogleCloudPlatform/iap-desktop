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
using System.Collections.Generic;
using Google.Solutions.Common.Util;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Services.Windows.TunnelsViewer
{
    internal class TunnelsViewModel : ObservableBase
    {
        private readonly ITunnelBrokerService tunnelBrokerService;
        private IEnumerable<Tunnel> selectedTunnels = Enumerable.Empty<Tunnel>();

        //---------------------------------------------------------------------
        // Properties for data binding.
        //---------------------------------------------------------------------

        public ObservableCollection<Tunnel> Tunnels { get; }
            = new ObservableCollection<Tunnel>();

        public IEnumerable<Tunnel> SelectedTunnels
        {
            get => this.selectedTunnels;
            set
            {
                this.selectedTunnels = value;

                RaisePropertyChange();
                RaisePropertyChange("IsDisconnectButtonEnabled");
            }
        }

        public bool IsDisconnectButtonEnabled => this.selectedTunnels.Any();

        //---------------------------------------------------------------------


        public TunnelsViewModel(
            ITunnelBrokerService tunnelBrokerService,
            IEventService eventService)
        {
            this.tunnelBrokerService = tunnelBrokerService;

            // Keep the model up to date.
            eventService.BindHandler<TunnelOpenedEvent>(_ => RefreshTunnels());
            eventService.BindHandler<TunnelClosedEvent>(_ => RefreshTunnels());
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

        public async Task DisconnectSelectedTunnels()
        {
            foreach (var tunnel in this.selectedTunnels.EnsureNotNull())
            {
                await this.tunnelBrokerService.DisconnectAsync(tunnel.Destination);
            }

            // Reset selection.
            this.SelectedTunnels = Enumerable.Empty<Tunnel>();
                
            RefreshTunnels();
        }
    }
}
