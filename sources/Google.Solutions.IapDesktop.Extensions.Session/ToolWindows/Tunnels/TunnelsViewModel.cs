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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System.Collections.ObjectModel;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Tunnels
{
    [Service]
    public class TunnelsViewModel : ViewModelBase
    {
        private readonly IIapTransportFactory factory;
        private IIapTunnel? selectedTunnel = null;

        //---------------------------------------------------------------------
        // Properties for data binding.
        //---------------------------------------------------------------------

        public ObservableCollection<IIapTunnel> Tunnels { get; }
            = new ObservableCollection<IIapTunnel>();

        public IIapTunnel? SelectedTunnel
        {
            get => this.selectedTunnel;
            set
            {
                this.selectedTunnel = value;
                RaisePropertyChange();
            }
        }

        public bool IsRefreshButtonEnabled => this.Tunnels.Any();

        //---------------------------------------------------------------------

        public TunnelsViewModel(
            IIapTransportFactory factory,
            IEventQueue eventService)
        {
            this.factory = factory.ExpectNotNull(nameof(factory));

            //
            // Keep the model up to date.
            //
            eventService.Subscribe<TunnelEvents.TunnelCreated>(_ => RefreshTunnels());
            eventService.Subscribe<TunnelEvents.TunnelClosed>(_ => RefreshTunnels());
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void RefreshTunnels()
        {
            this.Tunnels.Clear();

            foreach (var t in this.factory.Pool)
            {
                this.Tunnels.Add(t);
            }

            RaisePropertyChange((TunnelsViewModel m) => m.IsRefreshButtonEnabled);
        }
    }
}
