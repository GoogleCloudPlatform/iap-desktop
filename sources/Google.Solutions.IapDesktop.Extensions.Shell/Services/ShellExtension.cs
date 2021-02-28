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
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Shell.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.TunnelsViewer;
using System;
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
        internal static bool IsSshEnabled { get; private set; } = true;

        private readonly IServiceProvider serviceProvider;
        private readonly IWin32Window window;

        private static CommandState GetToolbarCommandStateWhenRunningInstanceRequired(
            IProjectExplorerNode node)
        {
            return node is IProjectExplorerVmInstanceNode vmNode && 
                        vmNode.IsRunning && 
                        vmNode.IsWindowsInstance
                ? CommandState.Enabled
                : CommandState.Disabled;
        }

        private static CommandState GetContextMenuCommandStateWhenRunningInstanceRequired(IProjectExplorerNode node)
        {
            if (node is IProjectExplorerVmInstanceNode vmNode && vmNode.IsWindowsInstance)
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
                .GetService<IRemoteDesktopSessionBroker>()
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
                    var settingsService = this.serviceProvider
                        .GetService<IConnectionSettingsService>();
                    var settings = settingsService.GetConnectionSettings(vmNode);

                    await this.serviceProvider.GetService<ICredentialsService>()
                        .GenerateCredentialsAsync(
                            this.window,
                            vmNode.Reference,
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
            IProjectExplorerNode node,
            bool allowPersistentCredentials)
        {
            try
            {
                if (node is IProjectExplorerVmInstanceNode vmNode)
                {
                    await this.serviceProvider
                        .GetService<IRdpConnectionService>()
                        .ActivateOrConnectInstanceAsync(
                            vmNode,
                            allowPersistentCredentials)
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

            //
            // Hide SSH feature by default.
            //
            IsSshEnabled = serviceProvider
                .GetService<ApplicationSettingsRepository>()
                .GetSettings()
                .IsPreviewFeatureSetEnabled
                .BoolValue;

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
                    node => Connect(node, true))
                {
                    Image = Resources.Connect_16,
                    IsDefault = true
                },
                0);
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Connect &as user...",
                    GetContextMenuCommandStateWhenRunningInstanceRequired,
                    node => Connect(node, false))
                {
                    Image = Resources.Connect_16
                },
                1);

            projectExplorer.ToolbarCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Connect to remote desktop",
                    GetToolbarCommandStateWhenRunningInstanceRequired,
                    node => Connect(node, true))
                {
                    Image = Resources.Connect_16
                });

            //
            // Connection settings.
            //
            var settingsService = serviceProvider.GetService<IConnectionSettingsService>();
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectExplorerNode>(
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
                new Command<IProjectExplorerNode>(
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
                2);

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
                    pseudoContext => serviceProvider.GetService<ITunnelsWindow>().ShowWindow())
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
                    _ => DoWithActiveSession(session => session.TrySetFullscreen(FullScreenMode.SingleScreen)))
                {
                    Image = Resources.Fullscreen_16,
                    ShortcutKeys = Keys.F11
                });
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "&Full screen (multiple displays)",
                    _ => GetCommandStateWhenActiveSessionRequired(),
                    _ => DoWithActiveSession(session => session.TrySetFullscreen(FullScreenMode.AllScreens)))
                {
                    Image = Resources.Fullscreen_16,
                    ShortcutKeys = Keys.F11 | Keys.Shift
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
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "&Keyboard shortcuts",
                    _ => GetCommandStateWhenActiveSessionRequired(),
                    _ => new ShortcutsWindow().Show(this.window)));
            desktopMenu.AddSeparator();
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "Show &security screen (send Ctrl+Alt+Esc)",
                    _ => GetCommandStateWhenActiveSessionRequired(),
                    _ => DoWithActiveSession(session => session.ShowSecurityScreen())));
            desktopMenu.AddCommand(
                new Command<IMainForm>(
                    "Open &task manager (send Ctrl+Shift+Esc)",
                    _ => GetCommandStateWhenActiveSessionRequired(),
                    _ => DoWithActiveSession(session => session.ShowTaskManager())));
        }

        private void DoWithActiveSession(Action<IRemoteDesktopSession> action)
        {
            try
            {
                var session = this.serviceProvider.GetService<IRemoteDesktopSessionBroker>().ActiveSession;
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
