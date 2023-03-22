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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Session
{
    public interface IInstanceSessionBroker : ISessionBroker
    {
        /// <summary>
        /// Create a new SSH session.
        /// </summary>
        Task<ISshTerminalSession> ConnectAsync(SshConnectionTemplate template);

        /// <summary>
        /// Create a new RDP session.
        /// </summary>
        IRemoteDesktopSession Connect(RdpConnectionTemplate template);
    }

    [Service(typeof(IInstanceSessionBroker), ServiceLifetime.Singleton, ServiceVisibility.Global)]
    [ServiceCategory(typeof(ISessionBroker))]
    public class InstanceSessionBroker : IInstanceSessionBroker
    {

        private readonly IServiceProvider serviceProvider;
        private readonly IMainWindow mainForm;

        public InstanceSessionBroker(IServiceProvider serviceProvider)
        {
            this.mainForm = serviceProvider.GetService<IMainWindow>();
            this.serviceProvider = serviceProvider;

            // NB. The ServiceCategory attribute causes this class to be 
            // announced to the global connection broker.
        }

        //---------------------------------------------------------------------
        // ISessionBroker.
        //---------------------------------------------------------------------

        public ISession ActiveSession
        {
            get => (ISession)RemoteDesktopView.TryGetActivePane(this.mainForm)
                    ?? SshTerminalView.TryGetActivePane(this.mainForm)
                    ?? null;
        }

        public bool IsConnected(InstanceLocator vmInstance)
        {
            return 
                RemoteDesktopView.TryGetExistingPane(this.mainForm, vmInstance) != null ||
                SshTerminalView.TryGetExistingPane(this.mainForm, vmInstance) != null;
        }

        public bool TryActivate(InstanceLocator vmInstance, out ISession session)
        {
            if (RemoteDesktopView.TryGetExistingPane(this.mainForm, vmInstance) is
                RemoteDesktopView existingRdpSession && 
                existingRdpSession != null)
            {
                // Pane found, activate.
                existingRdpSession.SwitchToDocument();
                session = existingRdpSession;
                return true;
            }
            else if (SshTerminalView.TryGetExistingPane(this.mainForm, vmInstance) is
                SshTerminalView existingSshSession &&
                existingSshSession != null)
            {
                // Pane found, activate.
                existingSshSession.SwitchToDocument();
                session = existingSshSession;
                return true;
            }
            else
            {
                session = null;
                return false;
            }
        }

        //---------------------------------------------------------------------
        // IInstanceSessionBroker.
        //---------------------------------------------------------------------

        public async Task<ISshTerminalSession> ConnectAsync(SshConnectionTemplate template)
        {
            var window = ToolWindow.GetWindow<SshTerminalView, SshTerminalViewModel>(this.serviceProvider);
            window.ViewModel.Instance = template.Instance;
            window.ViewModel.Endpoint = template.Endpoint;
            window.ViewModel.AuthorizedKey = template.AuthorizedKey;
            window.ViewModel.Language = template.Language;
            window.ViewModel.ConnectionTimeout = template.ConnectionTimeout;

            var pane = window.Bind();
            window.Show();

            await pane.ConnectAsync()
                .ConfigureAwait(false);

            return pane;
        }

        public IRemoteDesktopSession Connect(RdpConnectionTemplate template)
        {
            var window = ToolWindow.GetWindow<RemoteDesktopView, RemoteDesktopViewModel>(this.serviceProvider);
            window.ViewModel.Instance = template.Instance;
            window.ViewModel.Server = template.Endpoint;
            window.ViewModel.Port = template.EndpointPort;
            window.ViewModel.Settings = template.Settings;

            var pane = window.Bind();
            window.Show();

            pane.Connect();

            return pane;
        }
    }
}
