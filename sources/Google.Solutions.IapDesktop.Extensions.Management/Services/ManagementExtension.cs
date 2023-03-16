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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Management;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Management.Properties;
using Google.Solutions.IapDesktop.Extensions.Management.Services.ActiveDirectory;
using Google.Solutions.IapDesktop.Extensions.Management.Views;
using Google.Solutions.IapDesktop.Extensions.Management.Views.ActiveDirectory;
using Google.Solutions.IapDesktop.Extensions.Management.Views.EventLog;
using Google.Solutions.IapDesktop.Extensions.Management.Views.InstanceProperties;
using Google.Solutions.IapDesktop.Extensions.Management.Views.PackageInventory;
using Google.Solutions.IapDesktop.Extensions.Management.Views.SerialOutput;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton, DelayCreation = false)]
    public class ManagementExtension
    {
        private readonly IServiceProvider serviceProvider;

        private readonly ViewFactory<JoinView, JoinViewModel> joinDialogFactory;

        private async Task JoinDomainAsync(
            IProjectModelInstanceNode instance)
        {
            var mainWindow = this.serviceProvider.GetService<IMainWindow>();

            string domainName;
            string newComputerName;

            using (var dialog = this.joinDialogFactory.CreateDialog())
            {
                dialog.ViewModel.ComputerName.Value = instance.DisplayName;

                if (dialog.ShowDialog(mainWindow) != DialogResult.OK)
                {
                    return;
                }

                domainName = dialog.ViewModel.DomainName.Value.Trim();
                var computerName = dialog.ViewModel.ComputerName.Value.Trim();

                //
                // Only specify a "new" computer name if it's different.
                //
                newComputerName = computerName
                    .Equals(instance.DisplayName, StringComparison.OrdinalIgnoreCase)
                        ? null
                        : computerName;
            }

            //
            // Prompt for credentials.
            //
            if (this.serviceProvider.GetService<ICredentialDialog>()
                .PromptForWindowsCredentials(
                    mainWindow,
                    $"Join {instance.DisplayName} to domain",
                    $"Enter Active Directory credentials for {domainName}.\n\n" +
                        "The credentials will be used to join the computer to the " +
                        "domain and will not be saved.",
                    AuthenticationPackage.Kerberos,
                    out var credential) != DialogResult.OK)
            {
                return;
            }

            //
            // Perform join in background job.
            //
            await this.serviceProvider
                .GetService<IJobService>()
                .RunInBackground<object>(
                    new JobDescription(
                        $"Joining {instance.DisplayName} to {domainName}...",
                        JobUserFeedbackType.BackgroundFeedback),
                    async jobToken =>
                    {
                        await this.serviceProvider
                            .GetService<IDomainJoinService>()
                            .JoinDomainAsync(
                                instance.Instance,
                                domainName,
                                newComputerName,
                                credential,
                                jobToken)
                        .ConfigureAwait(false);

                        return null;
                    })
                .ConfigureAwait(true);  // Back to original (UI) thread.
        }

        public ManagementExtension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.joinDialogFactory = this.serviceProvider.GetViewFactory<JoinView, JoinViewModel>();
            this.joinDialogFactory.Theme = this.serviceProvider.GetService<IThemeService>().DialogTheme;

            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            var packageInventoryCommands = serviceProvider.GetService<PackageInventoryCommands>();
            var eventLogCommands = serviceProvider.GetService<EventLogCommands>();
            var instancePropertiesCommands = serviceProvider.GetService<InstancePropertiesInspectorCommands>();
            var serialOutputCommands = serviceProvider.GetService<SerialOutputCommands>();
            var instanceControlCommands = serviceProvider.GetService<InstanceControlCommands>();
            //
            // Add commands to project explorer tool bar.
            //

            projectExplorer.ToolbarCommands.AddCommand(
                instancePropertiesCommands.ToolbarOpen,
                4);

            //
            // Add commands to project explorer context menu.
            //
            var reportContainer = projectExplorer.ContextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Report",
                    context => context is IProjectModelProjectNode
                            || context is IProjectModelCloudNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => { }));
            reportContainer.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Analyze VM and sole-tenant node usage...",
                    context => CommandState.Enabled,
                    context => this.serviceProvider
                        .GetService<HelpAdapter>()
                        .OpenTopic(HelpTopics.NodeUsageReporting))
                {
                    Image = Resources.Report_16
                });

            var controlContainer = projectExplorer.ContextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Contro&l",
                    node => node is IProjectModelInstanceNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => { }),
                7);
            controlContainer.AddCommand(instanceControlCommands.ContextMenuStart);
            controlContainer.AddCommand(instanceControlCommands.ContextMenuResume);
            controlContainer.AddCommand(instanceControlCommands.ContextMenuStop);
            controlContainer.AddCommand(instanceControlCommands.ContextMenuSuspend);
            controlContainer.AddCommand(instanceControlCommands.ContextMenuReset);

            controlContainer.AddSeparator();
            controlContainer.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "&Join to Active Directory",
                    node => node is IProjectModelInstanceNode vmNode &&
                            vmNode.OperatingSystem == OperatingSystems.Windows
                        ? (vmNode.IsRunning ? CommandState.Enabled : CommandState.Disabled)
                        : CommandState.Unavailable,
                    node => JoinDomainAsync((IProjectModelInstanceNode)node))
                {
                    ActivityText = "Joining to Active Directory"
                });

            projectExplorer.ContextMenuCommands.AddCommand(
                serialOutputCommands.ContextMenuOpenCom1,
                9);
            projectExplorer.ContextMenuCommands.AddCommand(
                eventLogCommands.ContextMenuOpen,
                10);

            var osCommand = projectExplorer.ContextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Soft&ware packages",
                    packageInventoryCommands.ContextMenuOpenInstalledPackages.QueryState,
                    context => { }),
                11);
            osCommand.AddCommand(packageInventoryCommands.ContextMenuOpenInstalledPackages);
            osCommand.AddCommand(packageInventoryCommands.ContextMenuOpenAvailablePackages);

            projectExplorer.ContextMenuCommands.AddCommand(
                instancePropertiesCommands.ContextMenuOpen,
                12);

            //
            // Add commands to main menu.
            //
            var mainForm = serviceProvider.GetService<IMainWindow>();
            
            mainForm.ViewMenu.AddCommand(eventLogCommands.WindowMenuOpen);

            var serialPortMenu = mainForm.ViewMenu.AddCommand(
                new ContextCommand<IMainWindow>(
                    "Serial port &output",
                    pseudoContext => CommandState.Enabled,
                    pseudoContext => { })
                {
                    Image = Resources.Log_16,
                });
            serialPortMenu.AddCommand(serialOutputCommands.WindowMenuOpenCom1);
            serialPortMenu.AddCommand(serialOutputCommands.WindowMenuOpenCom3);
            serialPortMenu.AddCommand(serialOutputCommands.WindowMenuOpenCom4);

            mainForm.ViewMenu.AddCommand(instancePropertiesCommands.WindowMenuOpen);
            mainForm.ViewMenu.AddCommand(packageInventoryCommands.WindowMenuOpenInstalledPackages);
            mainForm.ViewMenu.AddCommand(packageInventoryCommands.WindowMenuOpenAvailablePackages);
        }
    }
}
