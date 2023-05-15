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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Diagnostics;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshKeys;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Tunnels;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
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
            var connectCommands = new ConnectCommands(
                serviceProvider.GetService<UrlCommands>(),
                serviceProvider.GetService<Service<ISessionContextFactory>>(),
                serviceProvider.GetService<Service<IProjectModelService>>(),
                serviceProvider.GetService<Service<IInstanceSessionBroker>>());
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
                connectCommands.ContextMenuActivateOrConnectInstance,
                0);
            projectExplorer.ContextMenuCommands.AddCommand(
                connectCommands.ContextMenuConnectRdpAsUser,
                1);
            projectExplorer.ContextMenuCommands.AddCommand(
                connectCommands.ContextMenuConnectSshInNewTerminal,
                2);
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
