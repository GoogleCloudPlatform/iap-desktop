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
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Views;
using System;
using System.Linq;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Views.RemoteDesktop
{
    public interface IRemoteDesktopSession
    {
        void Close();

        bool TrySetFullscreen(bool fullscreen);

        bool IsConnected { get; }

        void ShowSecurityScreen();

        void ShowTaskManager();
    }

    // TODO: Rename to RemoteDesktopConnectionBroker
    public interface IRemoteDesktopService : IConnectionBroker
    {
        IRemoteDesktopSession ActiveSession { get; }

        IRemoteDesktopSession Connect(
            InstanceLocator vmInstance,
            string server,
            ushort port,
            VmInstanceConnectionSettings settings);
    }

    [Service(typeof(IRemoteDesktopService), ServiceLifetime.Singleton)]
    public class RemoteDesktopService : IRemoteDesktopService
    {
        private readonly IExceptionDialog exceptionDialog;
        private readonly IEventService eventService;
        private readonly DockPanel dockPanel;

        public RemoteDesktopService(IServiceProvider serviceProvider)
        {
            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.eventService = serviceProvider.GetService<IEventService>();

            // Register as connection broker so that the status of connections
            // managed by this broker is surfaced in Project Explorer.
            serviceProvider.GetService<IGlobalConnectionBroker>().Register(this);
        }

        private RemoteDesktopPane TryGetExistingPane(InstanceLocator vmInstance)
            => this.dockPanel.Documents
                .EnsureNotNull()
                .OfType<RemoteDesktopPane>()
                .Where(pane => pane.Instance == vmInstance)
                .FirstOrDefault();

        public IRemoteDesktopSession ActiveSession
            => this.dockPanel.ActiveDocument as IRemoteDesktopSession;

        public bool IsConnected(InstanceLocator vmInstance)
            => TryGetExistingPane(vmInstance) != null;

        public bool TryActivate(InstanceLocator vmInstance)
        {
            // Check if there is an existing session/pane.
            var rdpPane = TryGetExistingPane(vmInstance);
            if (rdpPane != null)
            {
                // Pane found, activate.
                rdpPane.Show(this.dockPanel, DockState.Document);
                return true;
            }
            else
            {
                return false;
            }
        }

        public IRemoteDesktopSession Connect(
            InstanceLocator vmInstance,
            string server,
            ushort port,
            VmInstanceConnectionSettings settings)
        {
            var rdpPane = new RemoteDesktopPane(
                this.eventService,
                this.exceptionDialog,
                vmInstance);
            rdpPane.Show(this.dockPanel, DockState.Document);

            rdpPane.Connect(server, port, settings);

            return rdpPane;
        }
    }
}
