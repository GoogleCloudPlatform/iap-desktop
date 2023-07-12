﻿//
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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Diagnostics;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.SshKeys;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Tunnels;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton, DelayCreation = false)]
    public class InitializeSessionExtension
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IWin32Window window;

        private static CommandState GetToolbarCommandStateWhenRunningWindowsInstanceRequired(
            IProjectModelNode node)
        {
            return node is IProjectModelInstanceNode vmNode &&
                        vmNode.IsRunning &&
                        vmNode.IsWindowsInstance()
                ? CommandState.Enabled
                : CommandState.Disabled;
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

        /// <summary>
        /// Load the default app protocols embedded into the assembly.
        /// </summary>
        internal static async Task LoadAndRegisterDefaultAppProtocolsAsync(
            ProtocolRegistry protocolRegistry)
        {
            var assembly = typeof(InitializeSessionExtension).Assembly;
            var loadTasks = assembly
                .GetManifestResourceNames()
                .Where(s => s.EndsWith(AppProtocolConfigurationFile.FileExtension))
                .Select(async resourceName =>
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        var protocol = await AppProtocolConfigurationFile
                            .ReadStreamAsync(stream)
                            .ConfigureAwait(false);

                        protocolRegistry.RegisterProtocol(protocol);
                    }
                })
                .ToList();

            await Task
                .WhenAll(loadTasks)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Load user-defined app protocols from the file system.
        /// </summary>
        internal static async Task LoadAndRegisterCustomAppProtocolsAsync(
            string protocolsPath,
            ProtocolRegistry protocolRegistry)
        {
            if (!Directory.Exists(protocolsPath))
            {
                return;
            }

            //
            // Load and register custom app protocols in parallel.
            //
            var loadTasks = new DirectoryInfo(protocolsPath)
                .GetFiles($"*{AppProtocolConfigurationFile.FileExtension}")
                .EnsureNotNull()
                .Select(async file =>
                {
                    try
                    {
                        ApplicationTraceSources.Default.TraceInformation(
                            "Loading protocol configuration from {0}...", file.Name);

                        var protocol = await AppProtocolConfigurationFile
                            .ReadFileAsync(file.FullName)
                            .ConfigureAwait(false);

                        protocolRegistry.RegisterProtocol(protocol);
                    }
                    catch (Exception e)
                    {
                        ApplicationTraceSources.Default.TraceError(
                            "Loading protocol configuration from {0} failed", file.Name);
                        ApplicationTraceSources.Default.TraceError(e);

                        throw;
                    }
                })
                .ToList();

            await Task
                .WhenAll(loadTasks)
                .ConfigureAwait(false);
        }

        private async Task LoadAndRegisterAppProtocolsAsync(
            IWin32Window window,
            ProtocolRegistry protocolRegistry)
        {
            try
            {
                var protocolsPath = Path.Combine(
                    this.serviceProvider.GetService<IInstall>().BaseDirectory,
                    "Config");

                await Task
                    .WhenAll(
                        LoadAndRegisterDefaultAppProtocolsAsync(protocolRegistry),
                        LoadAndRegisterCustomAppProtocolsAsync(
                            protocolsPath,
                            protocolRegistry))
                    .ConfigureAwait(true); // Back to UI thread (for exception dialog).
            }
            catch (Exception e)
            {
                //
                // Show error message, but resume startup.
                //
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(window, "Invalid protocol configuration", e);
            }
        }

        //---------------------------------------------------------------------
        // Setup
        //---------------------------------------------------------------------

        public InitializeSessionExtension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            var mainForm = serviceProvider.GetService<IMainWindow>();

            //
            // Register protocols.
            //
            var protocolRegistry = serviceProvider.GetService<ProtocolRegistry>();

            protocolRegistry.RegisterProtocol(
                new AppProtocol(
                    "SQL Server Management Studio",
                    Enumerable.Empty<ITrait>(),
                    Ssms.DefaultServerPort,
                    null,
                    new SsmsClient()));

            _ = LoadAndRegisterAppProtocolsAsync(mainForm, protocolRegistry);

            //
            // Let this extension handle all URL activations.
            //
            var connectCommands = new ConnectCommands(
                serviceProvider.GetService<UrlCommands>(),
                serviceProvider.GetService<ISessionContextFactory>(),
                serviceProvider.GetService<IProjectWorkspace>(),
                serviceProvider.GetService<IInstanceSessionBroker>());
            Debug.Assert(serviceProvider
                .GetService<UrlCommands>()
                .LaunchRdpUrl.QueryState(new IapRdpUrl(
                    new InstanceLocator("project", "zone", "name"),
                    new NameValueCollection())) == CommandState.Enabled,
                "URL command installed");

            this.window = mainForm;

            //
            // Connect.
            //
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            projectExplorer.ContextMenuCommands.AddCommand(
                connectCommands.ContextMenuActivateOrConnectInstance,
                0);
            projectExplorer.ContextMenuCommands.AddCommand(
                connectCommands.ContextMenuConnectRdpAsUser,
                1);
            projectExplorer.ContextMenuCommands.AddCommand(
                connectCommands.ContextMenuConnectSshInNewTerminal,
                2);

            var appCommands = serviceProvider.GetService<AppCommands>();

            var openWithCommands = projectExplorer.ContextMenuCommands.AddCommand(
                appCommands.ConnectWithContextCommand,
                3);
            foreach (var appCommand in appCommands
                .ConnectWithAppCommands
                .OrderBy(c => c.Text))
            {
                openWithCommands.AddCommand(appCommand);
            }


            projectExplorer.ToolbarCommands.AddCommand(
                connectCommands.ToolbarActivateOrConnectInstance);

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
                4);

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
                5);

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
            var sessionCommands = new SessionCommands();
            var menu = serviceProvider.GetService<IInstanceSessionBroker>().SessionMenu;
            menu.AddCommand(sessionCommands.EnterFullScreenOnSingleScreen);
            menu.AddCommand(sessionCommands.EnterFullScreenOnAllScreens);
            menu.AddCommand(connectCommands.DuplicateSession);
            menu.AddCommand(sessionCommands.Disconnect);
            menu.AddSeparator();
            menu.AddCommand(sessionCommands.DownloadFiles);
            menu.AddCommand(sessionCommands.ShowSecurityScreen);
            menu.AddCommand(sessionCommands.ShowTaskManager);
        }
    }
}
