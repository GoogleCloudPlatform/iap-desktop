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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth;
using Google.Solutions.Ssh;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Views.Terminal
{
    public interface ISshTerminalSession : IDisposable
    {
        void Close();
    }

    public interface ISshTerminalSessionBroker : ISessionBroker
    {
        ISshTerminalSession ActiveSession { get; }

        Task<ISshTerminalSession> ConnectAsync(
            InstanceLocator vmInstance,
            IPEndPoint endpoint,
            AuthorizedKey authorizedKey);
    }

    [Service(typeof(ISshTerminalSessionBroker), ServiceLifetime.Singleton, ServiceVisibility.Global)]
    [ServiceCategory(typeof(ISessionBroker))]
    public class SshTerminalSessionBroker : ISshTerminalSessionBroker
    {
        private readonly IServiceProvider serviceProvider;
        private readonly DockPanel dockPanel;

        public SshTerminalSessionBroker(IServiceProvider serviceProvider)
        {
            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
            this.serviceProvider = serviceProvider;

            // NB. The ServiceCategory attribute causes this class to be 
            // announced to the global connection broker.
        }

        private SshTerminalPane TryGetExistingPane(InstanceLocator vmInstance)
            => this.dockPanel.Documents
                .EnsureNotNull()
                .OfType<SshTerminalPane>()
                .Where(pane => pane.Instance == vmInstance)
                .FirstOrDefault();

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public ISshTerminalSession ActiveSession
            => this.dockPanel.ActiveDocument as ISshTerminalSession;

        public bool IsConnected(InstanceLocator vmInstance)
            => TryGetExistingPane(vmInstance) != null;

        public bool TryActivate(InstanceLocator vmInstance)
        {
            // Check if there is an existing session/pane.
            var pane = TryGetExistingPane(vmInstance);
            if (pane != null)
            {
                // Pane found, activate.
                pane.Show(this.dockPanel, DockState.Document);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<ISshTerminalSession> ConnectAsync(
            InstanceLocator vmInstance,
            IPEndPoint endpoint,
            AuthorizedKey authorizedKey)
        {
            var pane = new SshTerminalPane(
                this.serviceProvider,
                vmInstance,
                endpoint,
                authorizedKey);
            pane.ShowWindow(true);

            await pane.ConnectAsync()
                .ConfigureAwait(false);

            return pane;
        }
    }
}
