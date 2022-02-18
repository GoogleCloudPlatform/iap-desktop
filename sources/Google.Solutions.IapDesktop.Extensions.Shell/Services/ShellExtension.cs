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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Shell.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.TunnelsViewer;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class ShellExtension
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IWin32Window window;

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

        private CommandState GetCommandStateWhenActiveRemoteDesktopSessionRequired()
        {
            var activeSession = this.serviceProvider
                .GetService<IRemoteDesktopSessionBroker>()
                .ActiveRemoteDesktopSession;

            return (activeSession != null && activeSession.IsConnected)
                ? CommandState.Enabled
                : CommandState.Disabled;
        }

        //---------------------------------------------------------------------
        // Commands.
        //---------------------------------------------------------------------

        private async void GenerateCredentials(IProjectModelNode node)
        {
            try
            {
                if (node is IProjectModelInstanceNode vmNode)
                {
                    Debug.Assert(vmNode.IsWindowsInstance());

                    var settingsService = this.serviceProvider
                        .GetService<IConnectionSettingsService>();
                    var settings = settingsService.GetConnectionSettings(vmNode);

                    await this.serviceProvider.GetService<ICredentialsService>()
                        .GenerateCredentialsAsync(
                            this.window,
                            vmNode.Instance,
                            settings.TypedCollection,
                            false)
                        .ConfigureAwait(true);

                    settings.Save();
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

        private async void Connect(
            IProjectModelNode node,
            bool allowPersistentCredentials,
            bool forceNewConnection)
        {
            try
            {
                if (node is IProjectModelInstanceNode rdpNode && rdpNode.IsRdpSupported())
                {
                    await this.serviceProvider
                        .GetService<IRdpConnectionService>()
                        .ActivateOrConnectInstanceAsync(
                            rdpNode,
                            allowPersistentCredentials)
                        .ConfigureAwait(true);
                }
                else if (node is IProjectModelInstanceNode sshNode && sshNode.IsSshSupported())
                {
                    if (forceNewConnection)
                    {
                        await this.serviceProvider
                            .GetService<ISshConnectionService>()
                            .ConnectInstanceAsync(sshNode)
                            .ConfigureAwait(true);
                    }
                    else
                    {
                        await this.serviceProvider
                            .GetService<ISshConnectionService>()
                            .ActivateOrConnectInstanceAsync(sshNode)
                            .ConfigureAwait(true);
                    }
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

        private void OpenConnectionSettings()
        {
            this.serviceProvider
                .GetService<IConnectionSettingsWindow>()
                .ShowWindow();
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
                    .GetService<IRdpConnectionService>()
                    .ActivateOrConnectInstanceAsync(url);
        }

        public ShellExtension(IServiceProvider serviceProvider)
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
                new Command<IProjectModelNode>(
                    "&Connect",
                    GetContextMenuCommandStateWhenRunningInstanceRequired,
                    node => Connect(node, true, false))
                {
                    Image = Resources.Connect_16,
                    IsDefault = true
                },
                0);
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Connect &as user...",
                    GetContextMenuCommandStateWhenRunningWindowsInstanceRequired,   // Windows/RDP only.
                    node => Connect(node, false, false))
                {
                    Image = Resources.Connect_16
                },
                1);
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Connect in &new terminal",
                    GetContextMenuCommandStateWhenRunningSshInstanceRequired,   // Linux/SSH only.
                    node => Connect(node, false, true))
                {
                    Image = Resources.Connect_16
                },
                2);

            projectExplorer.ToolbarCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Connect",
                    GetToolbarCommandStateWhenRunningInstanceRequired,
                    node => Connect(node, true, false))
                {
                    Image = Resources.Connect_16
                });

            //
            // Connection settings.
            //
            var settingsService = serviceProvider.GetService<IConnectionSettingsService>();
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Connection settings",
                    node => settingsService.IsConnectionSettingsAvailable(node)
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => OpenConnectionSettings())
                {
                    ShortcutKeys = Keys.F4,
                    Image = Resources.Settings_16
                },
                4);

            projectExplorer.ToolbarCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Connection &settings",
                    node => settingsService.IsConnectionSettingsAvailable(node)
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    _ => OpenConnectionSettings())
                {
                    Image = Resources.Settings_16
                },
                3);

            //
            // Generate credentials (Windows/RDP only).
            //
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "&Generate Windows logon credentials...",
                    GetContextMenuCommandStateWhenRunningWindowsInstanceRequired,
                    GenerateCredentials)
                {
                    Image = Resources.Password_16
                },
                3);

            projectExplorer.ToolbarCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Generate Windows logon credentials",
                    GetToolbarCommandStateWhenRunningWindowsInstanceRequired,
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
                    pseudoContext => serviceProvider.GetService<ITunnelsWindow>().ShowWindow())
                {
                    Image = Resources.Tunnel_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.T
                },
                1);

            //
            // Session menu.
            //
            var desktopMenu = mainForm.AddMenu("&Session", 1);
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "&Full screen",
                    _ => GetCommandStateWhenActiveRemoteDesktopSessionRequired(),
                    _ => DoWithActiveRemoteDesktopSession(session => session.TrySetFullscreen(FullScreenMode.SingleScreen)))
                {
                    Image = Resources.Fullscreen_16,
                    ShortcutKeys =  DocumentWindow.EnterFullScreenHotKey
                });
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "&Full screen (multiple displays)",
                    _ => GetCommandStateWhenActiveRemoteDesktopSessionRequired(),
                    _ => DoWithActiveRemoteDesktopSession(session => session.TrySetFullscreen(FullScreenMode.AllScreens)))
                {
                    Image = Resources.Fullscreen_16,
                    ShortcutKeys = Keys.F11 | Keys.Shift
                });
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "&Disconnect",
                    _ => GetCommandStateWhenActiveRemoteDesktopSessionRequired(),
                    _ => DoWithActiveRemoteDesktopSession(session => session.Close()))
                {
                    Image = Resources.Disconnect_16,
                    ShortcutKeys = Keys.Control | Keys.F4
                });
            desktopMenu.AddSeparator();
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "Show &security screen (send Ctrl+Alt+Esc)",
                    _ => GetCommandStateWhenActiveRemoteDesktopSessionRequired(),
                    _ => DoWithActiveRemoteDesktopSession(session => session.ShowSecurityScreen())));
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "Open &task manager (send Ctrl+Shift+Esc)",
                    _ => GetCommandStateWhenActiveRemoteDesktopSessionRequired(),
                    _ => DoWithActiveRemoteDesktopSession(session => session.ShowTaskManager())));
        }

        private void DoWithActiveRemoteDesktopSession(Action<IRemoteDesktopSession> action)
        {
            try
            {
                var session = this.serviceProvider.GetService<IRemoteDesktopSessionBroker>().ActiveRemoteDesktopSession;
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
