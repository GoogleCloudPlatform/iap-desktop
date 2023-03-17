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

using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views
{
    [Service]
    public class ConnectCommands
    {
        public ConnectCommands(
            UrlCommands urlCommands,
            Service<IRdpConnectionService> rdpConnectionService,
            Service<ISshConnectionService> sshConnectionService,
            Service<IProjectModelService> modelService,
            ICommandContainer<ISession> sessionContextMenu)
        {
            //
            // Install command for launching URLs.
            //
            urlCommands.LaunchRdpUrl = new LaunchRdpUrlCommand(rdpConnectionService);

            this.ToolbarActivateOrConnectInstance = new ActivateOrConnectInstanceCommand(
                "&Connect",
                sessionContextMenu,
                rdpConnectionService,
                sshConnectionService)
            {
                AlwaysAvailable = true,                  // Never hide to avoid flicker.
                AvailableForSsh = true,
                AvailableForRdp = true,
                Image = Resources.Connect_16,
                ActivityText = "Connecting to VM instance"
            };
            this.ContextMenuActivateOrConnectInstance = new ActivateOrConnectInstanceCommand(
                "&Connect",
                sessionContextMenu,
                rdpConnectionService,
                sshConnectionService)
            {
                AvailableForSsh = true,
                AvailableForRdp = true,
                Image = Resources.Connect_16,
                IsDefault = true,
                ActivityText = "Connecting to VM instance"
            };
            this.ContextMenuConnectRdpAsUser = new ActivateOrConnectInstanceCommand(
                "Connect &as user...",
                sessionContextMenu,
                rdpConnectionService,
                sshConnectionService)
            {
                AvailableForSsh = false,
                AvailableForRdp = true,                  // Windows/RDP only.
                AllowPersistentRdpCredentials = false,   // Force auth prompt.
                Image = Resources.Connect_16,
                ActivityText = "Connecting to VM instance"
            };
            this.ContextMenuConnectSshInNewTerminal = new ActivateOrConnectInstanceCommand(
                "Connect in &new terminal",
                sessionContextMenu,
                rdpConnectionService,
                sshConnectionService)
            {
                AvailableForSsh = true,                  // Linux/SSH only.
                AvailableForRdp = false,
                ForceNewSshConnection = true,            // Force new.
                Image = Resources.Connect_16,
                ActivityText = "Connecting to VM instance"
            };

            //
            // Session commands.
            //
            this.DuplicateSession = new DuplicateSessionCommand(
                "D&uplicate",
                modelService,
                this.ContextMenuConnectSshInNewTerminal) // Forward.
            {
                Image = Resources.Duplicate,
                ActivityText = "Duplicating session"
            };
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> ToolbarActivateOrConnectInstance { get; }
        public IContextCommand<IProjectModelNode> ContextMenuActivateOrConnectInstance { get; }
        public IContextCommand<IProjectModelNode> ContextMenuConnectRdpAsUser { get; }
        public IContextCommand<IProjectModelNode> ContextMenuConnectSshInNewTerminal { get; }
        public IContextCommand<ISession> DuplicateSession { get; }

        //---------------------------------------------------------------------
        // RDP URL commands.
        //---------------------------------------------------------------------

        private class LaunchRdpUrlCommand : ToolContextCommand<IapRdpUrl>
        {
            private readonly Service<IRdpConnectionService> connectionService;

            public LaunchRdpUrlCommand(
                Service<IRdpConnectionService> connectionService)
                : base("Launch &RDP URL")
            {
                this.connectionService = connectionService;
            }

            protected override bool IsAvailable(IapRdpUrl url)
            {
                return url != null;
            }

            protected override bool IsEnabled(IapRdpUrl url)
            {
                return url != null;
            }

            public override Task ExecuteAsync(IapRdpUrl url)
            {
                return this.connectionService
                    .GetInstance()
                    .ActivateOrConnectInstanceAsync(url);
            }
        }

        private abstract class ConnectInstanceCommandBase : ToolContextCommand<IProjectModelNode>
        {
            protected ConnectInstanceCommandBase(string text) : base(text)
            {
            }
        }

        private class ActivateOrConnectInstanceCommand : ConnectInstanceCommandBase
        {
            private readonly ICommandContainer<ISession> sessionContextMenu;
            private readonly Service<IRdpConnectionService> rdpConnectionService;
            private readonly Service<ISshConnectionService> sshConnectionService;

            public bool AlwaysAvailable { get; set; } = false;
            public bool AvailableForSsh { get; set; } = false;
            public bool AvailableForRdp { get; set; } = false;
            public bool AllowPersistentRdpCredentials { get; set; } = true;
            public bool ForceNewSshConnection { get; set; } = false;

            public ActivateOrConnectInstanceCommand(
                string text,
                ICommandContainer<ISession> sessionContextMenu,
                Service<IRdpConnectionService> rdpConnectionService,
                Service<ISshConnectionService> sshConnectionService)
                : base(text)
            {
                this.sessionContextMenu = sessionContextMenu;
                this.rdpConnectionService = rdpConnectionService;
                this.sshConnectionService = sshConnectionService;
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
                ISession session = null;
                if (node is IProjectModelInstanceNode rdpNode && rdpNode.IsRdpSupported())
                {
                    session = await this.rdpConnectionService
                        .GetInstance()
                        .ActivateOrConnectInstanceAsync(
                            rdpNode,
                            this.AllowPersistentRdpCredentials)
                        .ConfigureAwait(true);

                    Debug.Assert(session != null);
                }
                else if (node is IProjectModelInstanceNode sshNode && sshNode.IsSshSupported())
                {
                    if (this.ForceNewSshConnection)
                    {
                        session = await this.sshConnectionService
                            .GetInstance()
                            .ConnectInstanceAsync(sshNode)
                            .ConfigureAwait(true);
                    }
                    else
                    {
                        session = await this.sshConnectionService
                            .GetInstance()
                            .ActivateOrConnectInstanceAsync(sshNode)
                            .ConfigureAwait(true);
                    }

                    Debug.Assert(session != null);
                }

                if (session != null &&
                    session is SessionViewBase sessionPane &&
                    sessionPane.ContextCommands == null)
                {
                    //
                    // Use commands from Session menu as
                    // context menu.
                    //
                    sessionPane.ContextCommands = this.sessionContextMenu;
                }
            }
        }

        private class DuplicateSessionCommand : ToolContextCommand<ISession>
        {
            private readonly Service<IProjectModelService> modelService;
            private IContextCommand<IProjectModelNode> connectInNewTerminalCommand;

            public DuplicateSessionCommand(
                string text,
                Service<IProjectModelService> modelService,
                IContextCommand<IProjectModelNode> connectInNewTerminalCommand)
                : base(text)
            {
                this.modelService = modelService;
                this.connectInNewTerminalCommand = connectInNewTerminalCommand;
            }

            protected override bool IsAvailable(ISession context)
            {
                return true;
            }

            protected override bool IsEnabled(ISession session)
            {
                return session != null &&
                    session is ISshTerminalSession sshSession &&
                    sshSession.IsConnected;
            }

            public override async Task ExecuteAsync(ISession session)
            {
                var sshSession = (ISshTerminalSession)session;

                //
                // Try to lookup node for this session. In some cases,
                // we might not find it (for example, if the project has
                // been unloaded in the meantime).
                //
                var node = await this.modelService
                    .GetInstance()
                    .GetNodeAsync(sshSession.Instance, CancellationToken.None)
                    .ConfigureAwait(true);

                if (node is IProjectModelInstanceNode vmNode &&
                    vmNode != null &&
                    this.connectInNewTerminalCommand.QueryState(vmNode) == CommandState.Enabled)
                {
                    await this.connectInNewTerminalCommand
                        .ExecuteAsync(vmNode)
                        .ConfigureAwait(true);
                }
            }
        }
    }
}
