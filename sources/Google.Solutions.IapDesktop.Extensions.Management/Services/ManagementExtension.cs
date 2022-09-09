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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Management;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Management.Properties;
using Google.Solutions.IapDesktop.Extensions.Management.Services.ActiveDirectory;
using Google.Solutions.IapDesktop.Extensions.Management.Views.ActiveDirectory;
using Google.Solutions.IapDesktop.Extensions.Management.Views.EventLog;
using Google.Solutions.IapDesktop.Extensions.Management.Views.InstanceProperties;
using Google.Solutions.IapDesktop.Extensions.Management.Views.PackageInventory;
using Google.Solutions.IapDesktop.Extensions.Management.Views.SerialOutput;
using Google.Solutions.Mvvm.Commands;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class ManagementExtension
    {
        private readonly IServiceProvider serviceProvider;

        private async Task ControlInstanceAsync(
            InstanceLocator instance,
            string action,
            InstanceControlCommand command)
        {
            var mainForm = this.serviceProvider.GetService<IMainForm>();

            if (this.serviceProvider.GetService<IConfirmationDialog>()
                .Confirm(
                    mainForm.Window,
                    "Are you you sure you want to " +
                        $"{command.ToString().ToLower()} {instance.Name}?",
                    $"{command} {instance.Name}?",
                    $"{command} VM instance") != DialogResult.Yes)
            {
                return;
            }

            //
            // Load data using a job so that the task is retried in case
            // of authentication issues.
            //
            await this.serviceProvider
                .GetService<IJobService>()
                .RunInBackground<object>(
                    new JobDescription(
                        $"{action} {instance.Name}...",
                        JobUserFeedbackType.BackgroundFeedback),
                    async jobToken =>
                    {
                        using (var service = this.serviceProvider
                            .GetService<IInstanceControlService>())
                        {
                            await service.ControlInstanceAsync(
                                    instance,
                                    command,
                                    jobToken)
                            .ConfigureAwait(false);
                        }

                        return null;
                    })
                .ConfigureAwait(true);  // Back to original (UI) thread.
        }

        private async Task JoinDomainAsync(
            IProjectModelInstanceNode instance)
        {
            var mainForm = this.serviceProvider.GetService<IMainForm>();

            string domainName;
            string newComputerName;
            using (var dialog = new JoinDialog())
            {
                //
                // Prompt for domain name, computer name.
                //
                dialog.ComputerName.Value = instance.DisplayName;
                if (dialog.ShowDialog(mainForm.Window) != DialogResult.OK)
                {
                    return;
                }

                domainName = dialog.DomainName.Value.Trim();
                var computerName = dialog.ComputerName.Value.Trim();

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
                    mainForm.Window,
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
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            //
            // Add commands to project explorer tool bar.
            //

            projectExplorer.ToolbarCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Properties",
                    InstancePropertiesInspectorViewModel.GetToolbarCommandState,
                    context => serviceProvider.GetService<InstancePropertiesInspectorWindow>().ShowWindow())
                {
                    Image = Resources.ComputerDetails_16
                },
                4);

            //
            // Add commands to project explorer context menu.
            //
            var reportContainer = projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Report",
                    context => context is IProjectModelProjectNode
                            || context is IProjectModelCloudNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => { }));
            reportContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "Analyze VM and sole-tenant node usage...",
                    context => CommandState.Enabled,
                    context => this.serviceProvider
                        .GetService<HelpService>()
                        .OpenTopic(HelpTopics.NodeUsageReporting))
                {
                    Image = Resources.Report_16
                });

            var controlContainer = projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Contro&l",
                    node => node is IProjectModelInstanceNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => { }),
                7);
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "&Start",
                    node => node is IProjectModelInstanceNode vmNode && vmNode.CanStart
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    node => ControlInstanceAsync(
                        ((IProjectModelInstanceNode)node).Instance,
                        "Starting",
                        InstanceControlCommand.Start))
                {
                    Image = Resources.Start_16,
                    ActivityText = "Starting VM instance"
                });
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "&Resume",
                    node => node is IProjectModelInstanceNode vmNode && vmNode.CanResume
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    node => ControlInstanceAsync(
                        ((IProjectModelInstanceNode)node).Instance,
                        "Resuming",
                        InstanceControlCommand.Resume))
                {
                    Image = Resources.Start_16,
                    ActivityText = "Resuming VM instance"
                });
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "Sto&p",
                    node => node is IProjectModelInstanceNode vmNode && vmNode.CanStop
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    node => ControlInstanceAsync(
                        ((IProjectModelInstanceNode)node).Instance,
                        "Stopping",
                        InstanceControlCommand.Stop))
                {
                    Image = Resources.Stop_16,
                    ActivityText = "Stopping VM instance"
                });
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "Suspe&nd",
                    node => node is IProjectModelInstanceNode vmNode && vmNode.CanSuspend
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    node => ControlInstanceAsync(
                        ((IProjectModelInstanceNode)node).Instance,
                        "Suspending",
                        InstanceControlCommand.Suspend))
                {
                    Image = Resources.Pause_16,
                    ActivityText = "Suspending VM instance"
                });
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "Rese&t",
                    node => node is IProjectModelInstanceNode vmNode && vmNode.CanReset
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    node => ControlInstanceAsync(
                        ((IProjectModelInstanceNode)node).Instance,
                        "Resetting",
                        InstanceControlCommand.Reset))
                {
                    Image = Resources.Reset_16,
                    ActivityText = "Resetting VM instance"
                });

            controlContainer.AddSeparator();
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
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
                new Command<IProjectModelNode>(
                    "Show serial port &output (COM1)",
                    SerialOutputViewModel.GetCommandState,
                    context => this.serviceProvider.GetService<SerialOutputWindowCom1>().ShowWindow())
                {
                    Image = Resources.Log_16
                },
                9);
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Show &event log",
                    EventLogViewModel.GetCommandState,
                    context => this.serviceProvider.GetService<EventLogWindow>().ShowWindow())
                {
                    Image = Resources.EventLog_16
                },
                10);

            var osCommand = projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Soft&ware packages",
                    PackageInventoryViewModel.GetCommandState,
                    context => { }),
                11);
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
                12);

            //
            // Add commands to main menu.
            //
            var mainForm = serviceProvider.GetService<IMainForm>();
            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "&Event log",
                    pseudoContext => CommandState.Enabled,
                    pseudoContext => this.serviceProvider.GetService<EventLogWindow>().ShowWindow())
                {
                    Image = Resources.EventLog_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.E
                });

            var serialPortMenu = mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "Serial port &output",
                    pseudoContext => CommandState.Enabled,
                    pseudoContext => { })
                {
                    Image = Resources.Log_16,
                });
            serialPortMenu.AddCommand(
                new Command<IMainForm>(
                    "COM&1 (log)",
                    pseudoContext => CommandState.Enabled,
                    pseudoContext => this.serviceProvider.GetService<SerialOutputWindowCom1>().ShowWindow())
                {
                    Image = Resources.Log_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.O
                });
            serialPortMenu.AddCommand(
                new Command<IMainForm>(
                    "COM&3 (setup log)",
                    pseudoContext => CommandState.Enabled,
                    pseudoContext => this.serviceProvider.GetService<SerialOutputWindowCom3>().ShowWindow())
                {
                    Image = Resources.Log_16,
                });
            serialPortMenu.AddCommand(
                new Command<IMainForm>(
                    "COM&4 (agent)",
                    pseudoContext => CommandState.Enabled,
                    pseudoContext => this.serviceProvider.GetService<SerialOutputWindowCom4>().ShowWindow())
                {
                    Image = Resources.Log_16,
                });


            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "&Instance properties",
                    _ => CommandState.Enabled,
                    _ => serviceProvider.GetService<InstancePropertiesInspectorWindow>().ShowWindow())
                {
                    Image = Resources.ComputerDetails_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.I
                });
            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "I&nstalled packages",
                    _ => CommandState.Enabled,
                    _ => serviceProvider.GetService<InstalledPackageInventoryWindow>().ShowWindow())
                {
                    Image = Resources.Package_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.P
                });
            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "&Available updates",
                    _ => CommandState.Enabled,
                    _ => serviceProvider.GetService<AvailablePackageInventoryWindow>().ShowWindow())
                {
                    Image = Resources.PackageUpdate_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.U
                });
        }
    }
}
