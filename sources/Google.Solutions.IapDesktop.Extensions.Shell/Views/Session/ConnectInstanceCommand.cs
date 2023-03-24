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
    /// <summary>
    /// Connect to a VM by model node, or activate an existing session
    /// if present.
    /// </summary>
    internal class ConnectInstanceCommand : ConnectInstanceCommandBase<IProjectModelNode>
    {
        private readonly ICommandContainer<ISession> sessionContextMenu;
        private readonly Service<IRdpConnectionService> rdpConnectionService;
        private readonly Service<ISshConnectionService> sshConnectionService;
        private readonly Service<IInstanceSessionBroker> sessionBroker;

        public bool AvailableForSsh { get; set; } = false;
        public bool AvailableForRdp { get; set; } = false;
        public bool AllowPersistentRdpCredentials { get; set; } = true;
        public bool ForceNewConnection { get; set; } = false;

        public ConnectInstanceCommand(
            string text,
            ICommandContainer<ISession> sessionContextMenu,
            Service<IRdpConnectionService> rdpConnectionService,
            Service<ISshConnectionService> sshConnectionService,
            Service<IInstanceSessionBroker> sessionBroker)
            : base(text)
        {
            this.sessionContextMenu = sessionContextMenu;
            this.rdpConnectionService = rdpConnectionService;
            this.sshConnectionService = sshConnectionService;
            this.sessionBroker = sessionBroker;
        }

        protected override bool IsAvailable(IProjectModelNode node)
        {
            return node != null &&
                node is IProjectModelInstanceNode instanceNode &&
                ((this.AvailableForSsh && instanceNode.IsSshSupported()) ||
                    (this.AvailableForRdp && instanceNode.IsRdpSupported()));
        }

        protected override bool IsEnabled(IProjectModelNode node)
        {
            Debug.Assert(IsAvailable(node));
            return ((IProjectModelInstanceNode)node).IsRunning;
        }

        public override async Task ExecuteAsync(IProjectModelNode node)
        {
            Debug.Assert(IsAvailable(node));
            Debug.Assert(IsEnabled(node));

            var instanceNode = (IProjectModelInstanceNode)node;
            ISession session = null;

            if (!this.ForceNewConnection && this.sessionBroker
                .GetInstance()
                .TryActivate(instanceNode.Instance, out session))
            {
                //
                // There is an existing session, and it's now active.
                //
                Debug.Assert(session != null);
                Debug.Assert(
                    (instanceNode.IsRdpSupported() && session is IRemoteDesktopSession) ||
                    (instanceNode.IsSshSupported() && session is ISshTerminalSession));
                return;
            }

            //
            // Create new session.
            //
            if (instanceNode.IsRdpSupported())
            {
                var template = await this.rdpConnectionService
                    .GetInstance()
                    .PrepareConnectionAsync(
                        instanceNode,
                        this.AllowPersistentRdpCredentials)
                    .ConfigureAwait(true);

                session = this.sessionBroker
                    .GetInstance()
                    .Connect(template);
            }
            else if (instanceNode.IsSshSupported())
            {
                var template = await this.sshConnectionService
                    .GetInstance()
                    .PrepareConnectionAsync(instanceNode)
                    .ConfigureAwait(true);

                session = await this.sessionBroker
                    .GetInstance()
                    .ConnectAsync(template)
                    .ConfigureAwait(true);
            }

            Debug.Assert(session != null);

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