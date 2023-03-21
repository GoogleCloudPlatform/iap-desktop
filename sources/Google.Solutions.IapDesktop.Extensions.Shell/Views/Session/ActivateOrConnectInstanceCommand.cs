//
// Copyright 2023 Google LLC
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
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Session
{
    internal class ActivateOrConnectInstanceCommand : ToolContextCommand<IProjectModelNode>
    {
        private readonly ICommandContainer<ISession> sessionContextMenu;
        private readonly Service<IRdpConnectionService> rdpConnectionService;
        private readonly Service<ISshConnectionService> sshConnectionService;
        private readonly Service<IGlobalSessionBroker> sessionBroker;

        public bool AlwaysAvailable { get; set; } = false;
        public bool AvailableForSsh { get; set; } = false;
        public bool AvailableForRdp { get; set; } = false;
        public bool AllowPersistentRdpCredentials { get; set; } = true;
        public bool ForceNewSshConnection { get; set; } = false;

        public ActivateOrConnectInstanceCommand(
            string text,
            ICommandContainer<ISession> sessionContextMenu,
            Service<IRdpConnectionService> rdpConnectionService,
            Service<ISshConnectionService> sshConnectionService,
            Service<IGlobalSessionBroker> sessionBroker)
            : base(text)
        {
            this.sessionContextMenu = sessionContextMenu;
            this.rdpConnectionService = rdpConnectionService;
            this.sshConnectionService = sshConnectionService;
            this.sessionBroker = sessionBroker;
        }

        protected override bool IsAvailable(IProjectModelNode node)
        {
            if (this.AlwaysAvailable) // For toolbars.
            {
                return true;
            }
            else
            {
                return node != null &&
                    node is IProjectModelInstanceNode instanceNode &&
                    ((this.AvailableForSsh && instanceNode.IsSshSupported()) ||
                        (this.AvailableForRdp && instanceNode.IsRdpSupported()));
            }
        }

        protected override bool IsEnabled(IProjectModelNode node)
        {
            return node != null &&
                node is IProjectModelInstanceNode instanceNode &&
                ((this.AvailableForSsh && instanceNode.IsSshSupported()) ||
                    (this.AvailableForRdp && instanceNode.IsRdpSupported())) &&
                instanceNode.IsRunning;
        }

        public override async Task ExecuteAsync(IProjectModelNode node)
        {
            Debug.Assert(IsAvailable(node));
            Debug.Assert(IsEnabled(node));

            ISession session = null;
            if (node is IProjectModelInstanceNode rdpNode && rdpNode.IsRdpSupported())
            {
                if (this.sessionBroker
                    .GetInstance()
                    .TryActivate(rdpNode.Instance, out session)) 
                {
                    //
                    // There is an existing session, and it's now active.
                    //
                    Debug.Assert(session != null);
                    Debug.Assert(session is IRemoteDesktopSession);
                }
                else
                {
                    //
                    // Create new session.
                    //
                    session = await this.rdpConnectionService
                        .GetInstance()
                        .ConnectInstanceAsync(
                            rdpNode,
                            this.AllowPersistentRdpCredentials)
                        .ConfigureAwait(true);
                }

                Debug.Assert(session != null);
            }
            else if (node is IProjectModelInstanceNode sshNode && sshNode.IsSshSupported())
            {
                if (this.ForceNewSshConnection)
                {
                    //
                    // Create new session (event if there is one already).
                    //
                    session = await this.sshConnectionService
                        .GetInstance()
                        .ConnectInstanceAsync(sshNode)
                        .ConfigureAwait(true);
                }
                else if (this.sessionBroker
                    .GetInstance()
                    .TryActivate(sshNode.Instance, out session))
                {
                    //
                    // There is an existing session, and it's now active.
                    //
                    Debug.Assert(session != null);
                    Debug.Assert(session is ISshTerminalSession);
                }
                else
                {
                    //
                    // Create new session.
                    //
                    session = await this.sshConnectionService
                        .GetInstance()
                        .ConnectInstanceAsync(sshNode)
                        .ConfigureAwait(true);
                }

                Debug.Assert(session != null);
            }

            if (session != null &&
                session is SessionViewBase sessionPane &&
                sessionPane.ContextCommands == null)
            {
                //
                // Use commands from Session menu as context menu.
                //
                sessionPane.ContextCommands = this.sessionContextMenu;
            }
        }
    }
}