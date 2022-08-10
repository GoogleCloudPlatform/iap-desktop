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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.Mvvm.Commands;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Activity.Properties;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Views.EventLog;
using Google.Solutions.IapDesktop.Extensions.Activity.Views.SerialOutput;
using System;
using System.Linq;
using System.Windows.Forms;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Management;
using Google.Solutions.IapDesktop.Application.Views.Dialog;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class Extension
    {
        private readonly IServiceProvider serviceProvider;

        private async void ControlInstance(
            InstanceLocator instance,
            string action,
            InstanceControlCommand command)
        {
            var jobService = this.serviceProvider.GetService<IJobService>();

            // TODO: Ask for confirmation.

            //
            // Load data using a job so that the task is retried in case
            // of authentication issues.
            //
            try
            {
                await jobService.RunInBackground<object>(
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
                    }).ConfigureAwait(true);  // Back to original (UI) thread.
            }
            catch (Exception e)
            {
                var mainForm = this.serviceProvider
                    .GetService<IMainForm>();

                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(
                        mainForm.Window,
                        $"{action} {instance.Name} failed",
                        e);
            }
        }

        public Extension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            //
            // Add commands to project explorer.
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

            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Show serial port &output (COM1)",
                    SerialOutputViewModel.GetCommandState,
                    context => this.serviceProvider.GetService<SerialOutputWindowCom1>().ShowWindow())
                {
                    Image = Resources.Log_16
                },
                7);
            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Show &event log",
                    EventLogViewModel.GetCommandState,
                    context => this.serviceProvider.GetService<EventLogWindow>().ShowWindow())
                {
                    Image = Resources.EventLog_16
                },
                8);

            var controlContainer = projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Contro&l",
                    node => node is IProjectModelInstanceNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => { }),
                9); // TODO: Fix index
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "&Start",
                    node => node is IProjectModelInstanceNode vmNode && vmNode.IsRunning
                        ? CommandState.Disabled
                        : CommandState.Enabled,
                    node => ControlInstance(
                        ((IProjectModelInstanceNode)node).Instance,
                        "Starting",
                        InstanceControlCommand.Start))
                {
                    Image = Resources.Start_16
                });
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "&Resume",
                    node => node is IProjectModelInstanceNode vmNode && vmNode.IsRunning // TODO: Check suspended state
                        ? CommandState.Disabled
                        : CommandState.Enabled,
                    node => ControlInstance(
                        ((IProjectModelInstanceNode)node).Instance,
                        "Resuming",
                        InstanceControlCommand.Resume))
                {
                    Image = Resources.Start_16
                });
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "Sto&p",
                    node => node is IProjectModelInstanceNode vmNode && vmNode.IsRunning
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    node => ControlInstance(
                        ((IProjectModelInstanceNode)node).Instance,
                        "Stopping",
                        InstanceControlCommand.Stop))
                {
                    Image = Resources.Stop_16
                });
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "Suspe&nd",
                    node => node is IProjectModelInstanceNode vmNode && vmNode.IsRunning
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    node => ControlInstance(
                        ((IProjectModelInstanceNode)node).Instance,
                        "Suspending",
                        InstanceControlCommand.Suspend))
                {
                    Image = Resources.Pause_16
                });
            controlContainer.AddCommand(
                new Command<IProjectModelNode>(
                    "Rese&t",
                    node => node is IProjectModelInstanceNode vmNode && vmNode.IsRunning
                        ? CommandState.Enabled
                        : CommandState.Disabled,
                    node => ControlInstance(
                        ((IProjectModelInstanceNode)node).Instance,
                        "Resetting",
                        InstanceControlCommand.Reset))
                {
                    Image = Resources.Reset_16
                });

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
        }
    }
}
