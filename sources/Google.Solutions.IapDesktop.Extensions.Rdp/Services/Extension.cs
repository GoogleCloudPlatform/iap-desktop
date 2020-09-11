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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Rdp.Properties;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.Credentials;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.TunnelsViewer;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.RemoteDesktop;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class Extension
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IWin32Window window;

        private static CommandState GetToolbarCommandStateWhenRunningInstanceRequired(IProjectExplorerNode node)
        {
            return node is IProjectExplorerVmInstanceNode vmNode && vmNode.IsRunning
                ? CommandState.Enabled
                : CommandState.Disabled;
        }

        private static CommandState GetContextMenuCommandStateWhenRunningInstanceRequired(IProjectExplorerNode node)
        {
            if (node is IProjectExplorerVmInstanceNode vmNode)
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

        private CommandState GetCommandStateWhenActiveSessionRequired()
        {
            var activeSession = this.serviceProvider
                .GetService<IRemoteDesktopConnectionBroker>()
                .ActiveSession;

            return (activeSession != null && activeSession.IsConnected)
                ? CommandState.Enabled
                : CommandState.Disabled;
        }

        //---------------------------------------------------------------------
        // Commands.
        //---------------------------------------------------------------------

        private async void GenerateCredentials(IProjectExplorerNode node)
        {
            try
            {
                if (node is IProjectExplorerVmInstanceNode vmNode)
                {
                    await this.serviceProvider.GetService<ICredentialsService>()
                        .GenerateCredentialsAsync(
                            this.window,
                            vmNode.Reference,
                            vmNode.SettingsEditor,
                            false)
                        .ConfigureAwait(true);
                }
            }
            catch (Exception e) when (e.IsCancellation())
            {
                // Ignore.
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this.window, "Generating credentials failed", e);
            }
        }

        private async void Connect(IProjectExplorerNode node)
        {
            try
            {
                if (node is IProjectExplorerVmInstanceNode vmNode)
                {
                    await this.serviceProvider
                        .GetService<IapRdpConnectionService>()
                        .ActivateOrConnectInstanceAsync(vmNode)
                        .ConfigureAwait(true);
                }
            }
            catch (Exception e) when (e.IsCancellation())
            {
                // Ignore.
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this.window, "Connecting to VM instance failed", e);
            }
        }

        //---------------------------------------------------------------------
        // Setup
        //---------------------------------------------------------------------

        private class UrlHandler : IIapUrlHandler
        {
            private readonly IServiceProvider serviceProvider;

            public UrlHandler(IServiceProvider serviceProvider)
            {
                this.serviceProvider = serviceProvider;
            }

            public Task ActivateOrConnectInstanceAsync(IapRdpUrl url)
                => this.serviceProvider
                    .GetService<IapRdpConnectionService>()
                    .ActivateOrConnectInstanceAsync(url);
        }

        public Extension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            var mainForm = serviceProvider.GetService<IMainForm>();

            //
            // Let this extension handle all URL activations.
            //
            // NB. We cannot instantiate the service here because we 
            // are in a constructor. So pass a delegate object that
            // instantiates the object lazily.
            //
            mainForm.SetUrlHandler(new UrlHandler(serviceProvider));

            this.window = mainForm.Window;

            //
            // Connect.
            //
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "&Connect",
                    GetContextMenuCommandStateWhenRunningInstanceRequired,
                    Connect)
                {
                    Image = Resources.Connect_16
                },
                0);

            projectExplorer.ToolbarCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Connect to remote desktop",
                    GetToolbarCommandStateWhenRunningInstanceRequired,
                    Connect)
                {
                    Image = Resources.Connect_16
                });


            //
            // Generate credentials.
            //
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "&Generate Windows logon credentials...",
                    GetContextMenuCommandStateWhenRunningInstanceRequired,
                    GenerateCredentials)
                {
                    Image = Resources.Password_16
                },
                1);

            projectExplorer.ToolbarCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Generate Windows logon credentials",
                    GetToolbarCommandStateWhenRunningInstanceRequired,
                    GenerateCredentials)
                {
                    Image = Resources.Password_16
                });

            //
            // TunnelsViewer
            //
            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "Active IAP &tunnels",
                    pseudoContext => CommandState.Enabled,
                    pseudoContext => serviceProvider.GetService<ITunnelsViewer>().ShowWindow())
                {
                    Image = Resources.Tunnel_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.T
                },
                1);

            //
            // Desktop menu.
            //
            var desktopMenu = mainForm.AddMenu("&Desktop", 1);
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "&Full screen",
                    _ => GetCommandStateWhenActiveSessionRequired(),
                    _ => DoWithActiveSession(session => session.TrySetFullscreen(true)))
                {
                    Image = Resources.Fullscreen_16,
                    ShortcutKeys = Keys.F11
                });
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "&Disconnect",
                    _ => GetCommandStateWhenActiveSessionRequired(),
                    _ => DoWithActiveSession(session => session.Close()))
                {
                    Image = Resources.Disconnect_16,
                    ShortcutKeys = Keys.Control | Keys.F4
                });
            desktopMenu.AddSeparator();
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "Show &security screen (send Ctrl+Alt+Esc)",
                    _ => GetCommandStateWhenActiveSessionRequired(),
                    _ => DoWithActiveSession(session => session.ShowSecurityScreen()))
                {
                    ShortcutKeys = Keys.Control | Keys.F4
                });
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "Show &task manager (send Ctrl+Shift+Esc)",
                    _ => GetCommandStateWhenActiveSessionRequired(),
                    _ => DoWithActiveSession(session => session.ShowTaskManager()))
                {
                    ShortcutKeys = Keys.Control | Keys.F4
                });
        }

        private void DoWithActiveSession(Action<IRemoteDesktopSession> action)
        {
            try
            {
                var session = this.serviceProvider.GetService<IRemoteDesktopConnectionBroker>().ActiveSession;
                if (session != null)
                {
                    action(session);
                }
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this.window, "Remote Desktop action failed", e);
            }
        }
    }
}
