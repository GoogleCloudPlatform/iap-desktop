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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.Mvvm.Commands;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Os.Properties;
using Google.Solutions.IapDesktop.Extensions.Os.Views.InstanceProperties;
using Google.Solutions.IapDesktop.Extensions.Os.Views.PackageInventory;
using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Extensions.Os.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Adapters;

namespace Google.Solutions.IapDesktop.Extensions.Os.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class Extension
    {
        private readonly IServiceProvider serviceProvider;

        private async Task JoinDomainAsync(
            IProjectModelInstanceNode instance)
        {
            var mainForm = this.serviceProvider.GetService<IMainForm>();

            // TODO: Use custom form, warn about restart
            var domain = "lab.local";
            if (this.serviceProvider.GetService<ICredentialDialog>()
                .PromptForWindowsCredentials(
                    mainForm.Window,
                    $"Join {instance.DisplayName} to domain",
                    "Enter domain credentials for performing domain-join",
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
                        $"Joining {instance.DisplayName} to {domain}...",
                        JobUserFeedbackType.BackgroundFeedback),
                    async jobToken =>
                    {
                        await this.serviceProvider
                            .GetService<IDomainJoinService>()
                            .JoinDomainAsync(
                                instance.Instance,
                                domain,
                                null, // TODO: Propmt for computer name
                                credential,
                                jobToken)
                        .ConfigureAwait(false);

                        return null;
                    })
                .ConfigureAwait(true);  // Back to original (UI) thread.
        }

        public Extension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            //
            // Add commands to project explorer.
            //
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            projectExplorer.ToolbarCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Properties",
                    InstancePropertiesInspectorViewModel.GetToolbarCommandState,
                    context => serviceProvider.GetService<InstancePropertiesInspectorWindow>().ShowWindow())
                {
                    Image = Resources.ComputerDetails_16
                },
                4);

            var osCommand = projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Soft&ware packages",
                    PackageInventoryViewModel.GetCommandState,
                    context => { }),
                10);
            osCommand.AddCommand(
                new Command<IProjectModelNode>(
                    "Show &installed packages",
                    PackageInventoryViewModel.GetCommandState,
                    context => serviceProvider.GetService<InstalledPackageInventoryWindow>().ShowWindow())
                {
                    Image = Resources.Package_16
                });
            osCommand.AddCommand(
                new Command<IProjectModelNode>(
                    "Show &available updates",
                    PackageInventoryViewModel.GetCommandState,
                    context => serviceProvider.GetService<AvailablePackageInventoryWindow>().ShowWindow())
                {
                    Image = Resources.PackageUpdate_16
                });

            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "P&roperties",
                    InstancePropertiesInspectorViewModel.GetContextMenuCommandState,
                    context => serviceProvider.GetService<InstancePropertiesInspectorWindow>().ShowWindow())
                {
                    Image = Resources.ComputerDetails_16,
                    ShortcutKeys = Keys.Alt | Keys.Enter
                },
                11);

            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "&Join to domain",
                    node => node is IProjectModelInstanceNode vmNode && 
                            vmNode.IsRunning && 
                            vmNode.OperatingSystem == OperatingSystems.Windows
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    node => JoinDomainAsync((IProjectModelInstanceNode)node))
                {
                    ActivityText = "Joining to domain"
                }); // TODO: Fix index

            //
            // Add commands to main menu.
            //
            var mainForm = serviceProvider.GetService<IMainForm>();
            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "&Instance properties",
                    _ => CommandState.Enabled,
                    _ => serviceProvider.GetService<InstancePropertiesInspectorWindow>().ShowWindow())
                {
                    Image = Resources.ComputerDetails_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.I
                },
                3);
            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "I&nstalled packages",
                    _ => CommandState.Enabled,
                    _ => serviceProvider.GetService<InstalledPackageInventoryWindow>().ShowWindow())
                {
                    Image = Resources.Package_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.P
                },
                4);
            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "&Available updates",
                    _ => CommandState.Enabled,
                    _ => serviceProvider.GetService<AvailablePackageInventoryWindow>().ShowWindow())
                {
                    Image = Resources.PackageUpdate_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.U
                },
                5);
        }
    }
}
