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
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Session
{
    public interface IInstanceSessionBroker : ISessionBroker
    {
        /// <summary>
        /// Create a new SSH session.
        /// </summary>
        Task<ISshTerminalSession> ConnectSshSessionAsync(
            ConnectionTemplate<SshSessionParameters> template);

        /// <summary>
        /// Create a new RDP session.
        /// </summary>
        IRemoteDesktopSession ConnectRdpSession(
            ConnectionTemplate<RdpSessionParameters> template);

        /// <summary>
        /// Command menu for sessions, exposed in the main menu
        /// and as context menu.
        /// </summary>
        ICommandContainer<ISession> SessionMenu { get; }
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

            //
            // NB. The ServiceCategory attribute causes this class to be 
            // announced to the global connection broker.
            //

            //
            // Register Session menu.
            //
            // On pop-up of the menu, query the active session and use it as context.
            //
            this.SessionMenu = this.mainForm.AddMenu(
                "&Session", 1,
                () => this.ActiveSession);
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

        public ICommandContainer<ISession> SessionMenu { get; }

        public async Task<ISshTerminalSession> ConnectSshSessionAsync(
            ConnectionTemplate<SshSessionParameters> template)
        {
            var window = ToolWindow.GetWindow<SshTerminalView, SshTerminalViewModel>(
                this.serviceProvider);

            window.ViewModel.Instance = template.Transport.Instance;
            window.ViewModel.Endpoint = template.Transport.Endpoint;
            window.ViewModel.AuthorizedKey = template.Session.AuthorizedKey;
            window.ViewModel.Language = template.Session.Language;
            window.ViewModel.ConnectionTimeout = template.Session.ConnectionTimeout;

            var session = window.Bind();
            window.Show();

            await session.ConnectAsync()
                .ConfigureAwait(false);

            Debug.Assert(session.ContextCommands == null);
            session.ContextCommands = this.SessionMenu;

            return session;
        }

        public IRemoteDesktopSession ConnectRdpSession(
            ConnectionTemplate<RdpSessionParameters> template)
        {
            var window = ToolWindow.GetWindow<RemoteDesktopView, RemoteDesktopViewModel>(
                this.serviceProvider);

            window.ViewModel.Instance = template.Transport.Instance;
            window.ViewModel.Server = IPAddress.IsLoopback(template.Transport.Endpoint.Address)
                ? "localhost"
                : template.Transport.Endpoint.Address.ToString();
            window.ViewModel.Port = (ushort)template.Transport.Endpoint.Port;
            window.ViewModel.Parameters = template.Session;

            var session = window.Bind();
            window.Show();

            session.Connect();

            Debug.Assert(session.ContextCommands == null);
            session.ContextCommands = this.SessionMenu;

            return session;
        }
    }
}
