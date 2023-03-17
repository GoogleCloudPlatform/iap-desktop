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
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Shell.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Diagnostics;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshKeys;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.TunnelsViewer;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton, DelayCreation = false)]
    public class ShellExtension
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IWin32Window window;
        private readonly ICommandContainer<ISession> sessionCommands;

        private static CommandState GetToolbarCommandStateWhenRunningInstanceRequired(
            IProjectModelNode node)
        {
            return node is IProjectModelInstanceNode vmNode && vmNode.IsRunning
                ? CommandState.Enabled
                : CommandState.Disabled;
        }

        private static CommandState GetToolbarCommandStateWhenRunningWindowsInstanceRequired(
            IProjectModelNode node)
        {
            return node is IProjectModelInstanceNode vmNode &&
                        vmNode.IsRunning &&
                        vmNode.IsWindowsInstance()
                ? CommandState.Enabled
                : CommandState.Disabled;
        }

        private static CommandState GetContextMenuCommandStateWhenRunningInstanceRequired(IProjectModelNode node)
        {
            if (node is IProjectModelInstanceNode vmNode)
            {
                return vmNode.IsRunning
                    ? CommandState.Enabled
                    : CommandState.Disabled;
            }
            else
            {
                return CommandState.Unavailable;
            }
        }

        private static CommandState GetContextMenuCommandStateWhenRunningWindowsInstanceRequired(IProjectModelNode node)
        {
            if (node is IProjectModelInstanceNode vmNode && vmNode.IsWindowsInstance())
            {
                return vmNode.IsRunning
                    ? CommandState.Enabled
                    : CommandState.Disabled;
            }
            else
            {
                return CommandState.Unavailable;
            }
        }


        private static CommandState GetContextMenuCommandStateWhenRunningSshInstanceRequired(IProjectModelNode node)
        {
            if (node is IProjectModelInstanceNode vmNode && vmNode.IsSshSupported())
            {
                return vmNode.IsRunning
                    ? CommandState.Enabled
                    : CommandState.Disabled;
            }
            else
            {
                return CommandState.Unavailable;
            }
        }

        private CommandState GetSessionMenuCommandState<TRequiredSession>(
            ISession session,
            Predicate<TRequiredSession> predicate)
            where TRequiredSession : class, ISession
        {
            if (session is TRequiredSession typedSession)
            {
                return predicate(typedSession)
                    ? CommandState.Enabled
                    : CommandState.Disabled;
            }
            else
            {
                //
                // If it doesn't apply, we're still showing it as
                // disabled.
                //
                return CommandState.Disabled;
            }
        }

        //---------------------------------------------------------------------
        // Commands.
        //---------------------------------------------------------------------

        private async Task GenerateCredentialsAsync(IProjectModelNode node)
        {
            if (node is IProjectModelInstanceNode vmNode)
            {
                Debug.Assert(vmNode.IsWindowsInstance());

                var settingsService = this.serviceProvider
                    .GetService<IConnectionSettingsService>();
                var settings = settingsService.GetConnectionSettings(vmNode);

                await this.serviceProvider.GetService<ICreateCredentialsWorkflow>()
                    .CreateCredentialsAsync(
                        this.window,
                        vmNode.Instance,
                        settings.TypedCollection,
                        false)
                    .ConfigureAwait(true);

                settings.Save();
            }
        }

        private async Task ConnectAsync(
            IProjectModelNode node,
            bool allowPersistentCredentials,
            bool forceNewConnection)
        {
            ISession session = null;
            if (node is IProjectModelInstanceNode rdpNode && rdpNode.IsRdpSupported())
            {
                session = await this.serviceProvider
                    .GetService<IRdpConnectionService>()
                    .ActivateOrConnectInstanceAsync(
                        rdpNode,
                        allowPersistentCredentials)
                    .ConfigureAwait(true);

                Debug.Assert(session != null);
            }
            else if (node is IProjectModelInstanceNode sshNode && sshNode.IsSshSupported())
            {
                if (forceNewConnection)
                {
                    session = await this.serviceProvider
                        .GetService<ISshConnectionService>()
                        .ConnectInstanceAsync(sshNode)
                        .ConfigureAwait(true);
                }
                else
                {
                    session = await this.serviceProvider
                        .GetService<ISshConnectionService>()
                        .ActivateOrConnectInstanceAsync(sshNode)
                        .ConfigureAwait(true);
                }

                Debug.Assert(session != null);
            }

            if (session is SessionViewBase sessionPane &&
                sessionPane.ContextCommands == null)
            {
                //
                // Use commands from Session menu as
                // context menu.
                //
                sessionPane.ContextCommands = this.sessionCommands;
            }
        }

        private async Task DuplicateSessionAsync(ISshTerminalSession session)
        {
            //
            // Try to lookup node for this session. In some cases,
            // we might not find it (for example, if the project has
            // been unloaded in the meantime).
            //
            var node = await this.serviceProvider
                .GetService<IProjectModelService>()
                .GetNodeAsync(session.Instance, CancellationToken.None)
                .ConfigureAwait(true);

            if (node is IProjectModelInstanceNode vmNode && vmNode != null)
            {
                await ConnectAsync(vmNode, false, true)
                    .ConfigureAwait(true);
            }
        }

        //---------------------------------------------------------------------
        // Setup
        //---------------------------------------------------------------------

        public ShellExtension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            var mainForm = serviceProvider.GetService<IMainWindow>();

            //
            // Let this extension handle all URL activations.
            //
            var connectionCommands = new ConnectionCommands(
                serviceProvider.GetService<UrlCommands>(),
                serviceProvider.GetService<Service<IRdpConnectionService>>());
            Debug.Assert(serviceProvider
                .GetService<UrlCommands>()
                .LaunchRdpUrl.QueryState(new IapRdpUrl(
                    new InstanceLocator("project", "zone", "name"),
                    new NameValueCollection()))
                == CommandState.Enabled,
                "URL command installed");

            this.window = mainForm;

            //
            // Connect.
            //
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            projectExplorer.ContextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "&Connect",
                    GetContextMenuCommandStateWhenRunningInstanceRequired,
                    node => ConnectAsync(node, true, false))
                {
                    Image = Resources.Connect_16,
                    IsDefault = true,
                    ActivityText = "Connecting to VM instance"
                },
                0);
            projectExplorer.ContextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Connect &as user...",
                    GetContextMenuCommandStateWhenRunningWindowsInstanceRequired,   // Windows/RDP only.
                    node => ConnectAsync(node, false, false))
                {
                    Image = Resources.Connect_16,
                    ActivityText = "Connecting to VM instance"
                },
                1);
            projectExplorer.ContextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Connect in &new terminal",
                    GetContextMenuCommandStateWhenRunningSshInstanceRequired,   // Linux/SSH only.
                    node => ConnectAsync(node, false, true))
                {
                    Image = Resources.Connect_16,
                    ActivityText = "Connecting to VM instance"
                },
                2);
            projectExplorer.ToolbarCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Connect",
                    GetToolbarCommandStateWhenRunningInstanceRequired,
                    node => ConnectAsync(node, true, false))
                {
                    Image = Resources.Connect_16,
                    ActivityText = "Connecting to VM instance"
                });

            //
            // Generate credentials (Windows/RDP only).
            //
            projectExplorer.ContextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "&Generate Windows logon credentials...",
                    GetContextMenuCommandStateWhenRunningWindowsInstanceRequired,
                    GenerateCredentialsAsync)
                {
                    Image = Resources.AddCredentials_16,
                    ActivityText = "Generating Windows logon credentials"
                },
                3);

            projectExplorer.ToolbarCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Generate Windows logon credentials",
                    GetToolbarCommandStateWhenRunningWindowsInstanceRequired,
                    GenerateCredentialsAsync)
                {
                    Image = Resources.AddCredentials_16,
                    ActivityText = "Generating Windows logon credentials"
                });

            //
            // Connection settings.
            //
            var connectionSettingsCommands = serviceProvider.GetService<ConnectionSettingsCommands>();
            projectExplorer.ContextMenuCommands.AddCommand(
                connectionSettingsCommands.ContextMenuOpen,
                4);

            projectExplorer.ToolbarCommands.AddCommand(
                connectionSettingsCommands.ToolbarOpen,
                3);

            //
            // Authorized keys.
            //
            var authorizedKeyCommands = serviceProvider.GetService<AuthorizedPublicKeysCommands>();
            projectExplorer.ContextMenuCommands.AddCommand(
                authorizedKeyCommands.ContextMenuOpen,
                11);
#if DEBUG
            projectExplorer.ContextMenuCommands.AddCommand(
                serviceProvider.GetService<DiagnosticsCommands>().GenerateHtmlPage);
#endif

            //
            // View menu.
            //
            var tunnelsViewCommands = serviceProvider.GetService<TunnelsViewCommands>();
            mainForm.ViewMenu.AddCommand(
                tunnelsViewCommands.WindowMenuOpen,
                1);
            mainForm.ViewMenu.AddCommand(authorizedKeyCommands.WindowMenuOpen);

            //
            // Session menu.
            //
            // On pop-up of the menu, query the active session and use it as context.
            //
            this.sessionCommands = mainForm.AddMenu(
                "&Session", 1,
                () => this.serviceProvider
                    .GetService<IGlobalSessionBroker>()
                    .ActiveSession);

            this.sessionCommands.AddCommand(
                new ContextCommand<ISession>(
                    "&Full screen",
                    session => GetSessionMenuCommandState<IRemoteDesktopSession>(
                        session,
                        rdpSession => rdpSession.IsConnected && rdpSession.CanEnterFullScreen),
                    session => (session as IRemoteDesktopSession)?.TrySetFullscreen(FullScreenMode.SingleScreen))
                {
                    Image = Resources.Fullscreen_16,
                    ShortcutKeys = DocumentWindow.EnterFullScreenHotKey,
                    ActivityText = "Activating full screen"
                });
            this.sessionCommands.AddCommand(
                new ContextCommand<ISession>(
                    "&Full screen (multiple displays)",
                    session => GetSessionMenuCommandState<IRemoteDesktopSession>(
                        session,
                        rdpSession => rdpSession.IsConnected && rdpSession.CanEnterFullScreen),
                    session => (session as IRemoteDesktopSession)?.TrySetFullscreen(FullScreenMode.AllScreens))
                {
                    Image = Resources.Fullscreen_16,
                    ShortcutKeys = Keys.F11 | Keys.Shift,
                    ActivityText = "Activating full screen"
                });
            this.sessionCommands.AddCommand(
                new ContextCommand<ISession>(
                    "D&uplicate",
                    session => GetSessionMenuCommandState<ISshTerminalSession>(
                        session,
                        sshSession => sshSession.IsConnected),
                    session => DuplicateSessionAsync((ISshTerminalSession)session))
                {
                    Image = Resources.Duplicate,
                    ActivityText = "Duplicating session"
                });
            this.sessionCommands.AddCommand(
                new ContextCommand<ISession>(
                    "&Disconnect",
                    session => GetSessionMenuCommandState<ISession>(
                        session,
                        anySession => anySession.IsConnected),
                    session => session.Close())
                {
                    Image = Resources.Disconnect_16,
                    ShortcutKeys = Keys.Control | Keys.F4,
                    ActivityText = "Disconnecting"
                });
            this.sessionCommands.AddSeparator();
            this.sessionCommands.AddCommand(
                new ContextCommand<ISession>(
                    "Do&wnload files...",
                    session => GetSessionMenuCommandState<ISshTerminalSession>(
                        session,
                        sshSession => sshSession.IsConnected),
                    session => (session as ISshTerminalSession)?.DownloadFilesAsync())
                {
                    Image = Resources.DownloadFile_16,
                    ActivityText = "Downloading files"
                });
            this.sessionCommands.AddCommand(
                new ContextCommand<ISession>(
                    "Show &security screen (send Ctrl+Alt+Esc)",
                    session => GetSessionMenuCommandState<IRemoteDesktopSession>(
                        session,
                        rdpSession => rdpSession.IsConnected),
                    session => (session as IRemoteDesktopSession)?.ShowSecurityScreen()));
            this.sessionCommands.AddCommand(
                new ContextCommand<ISession>(
                    "Open &task manager (send Ctrl+Shift+Esc)",
                    session => GetSessionMenuCommandState<IRemoteDesktopSession>(
                        session,
                        rdpSession => rdpSession.IsConnected),
                    session => (session as IRemoteDesktopSession)?.ShowTaskManager()));
        }
    }
}
