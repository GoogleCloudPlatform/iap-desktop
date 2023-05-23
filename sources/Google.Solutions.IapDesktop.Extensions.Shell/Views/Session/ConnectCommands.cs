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

using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Session
{
    [Service]
    public class ConnectCommands
    {
        public ConnectCommands(
            UrlCommands urlCommands,
            Service<ISessionContextFactory> sessionContextFactory,
            Service<IProjectModelService> modelService,
            Service<IInstanceSessionBroker> sessionBroker)
        {
            //
            // Install command for launching URLs.
            //
            urlCommands.LaunchRdpUrl = new ConnectRdpUrlCommand(
                sessionContextFactory,
                sessionBroker);

            this.ToolbarActivateOrConnectInstance = new ConnectInstanceCommand(
                "&Connect",
                sessionContextFactory,
                sessionBroker,
                modelService)
            {
                CommandType = MenuCommandType.ToolbarCommand, // Never hide to avoid flicker.
                AvailableForSsh = true,
                AvailableForRdp = true,
                Image = Resources.Connect_16,
                ActivityText = "Connecting to VM instance"
            };
            this.ContextMenuActivateOrConnectInstance = new ConnectInstanceCommand(
                "&Connect",
                sessionContextFactory,
                sessionBroker,
                modelService)
            {
                AvailableForSsh = true,
                AvailableForRdp = true,
                Image = Resources.Connect_16,
                IsDefault = true,
                ActivityText = "Connecting to VM instance"
            };
            this.ContextMenuConnectRdpAsUser = new ConnectInstanceCommand(
                "Connect &as user...",
                sessionContextFactory,
                sessionBroker,
                modelService)
            {
                AvailableForSsh = false,
                AvailableForRdp = true,                  // Windows/RDP only.
                Flags = RdpCreateSessionFlags.ForcePasswordPrompt,
                Image = Resources.Connect_16,
                ActivityText = "Connecting to VM instance"
            };
            this.ContextMenuConnectSshInNewTerminal = new ConnectInstanceCommand(
                "Connect in &new terminal",
                sessionContextFactory,
                sessionBroker,
                modelService)
            {
                AvailableForSsh = true,                  // Linux/SSH only.
                AvailableForRdp = false,
                ForceNewConnection = true,               // Force new.
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



        private class DuplicateSessionCommand : MenuCommandBase<ISession>
        {
            private readonly Service<IProjectModelService> modelService;
            private readonly IContextCommand<IProjectModelNode> connectInNewTerminalCommand;

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
