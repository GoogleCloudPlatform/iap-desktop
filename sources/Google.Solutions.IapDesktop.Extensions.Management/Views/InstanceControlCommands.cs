//
// Copyright 2023 Google LLC
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
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Management;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Management.Properties;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Google.Solutions.IapDesktop.Extensions.Management.Views
{
    [Service]
    public class InstanceControlCommands
    {
        public InstanceControlCommands(IServiceProvider serviceProvider)
        {
            this.ContextMenuStart = new ControlInstanceCommand(
                "&Start",
                InstanceControlCommand.Start,
                serviceProvider)
            {
                Image = Resources.Start_16,
                ActivityText = "Starting VM instance"
            };
            this.ContextMenuResume = new ControlInstanceCommand(
                "&Resume",
                InstanceControlCommand.Resume,
                serviceProvider)
            {
                Image = Resources.Start_16,
                ActivityText = "Resuming VM instance"
            };
            this.ContextMenuStop = new ControlInstanceCommand(
                "Sto&p",
                InstanceControlCommand.Stop,
                serviceProvider)
            {
                Image = Resources.Stop_16,
                ActivityText = "Stopping VM instance"
            };
            this.ContextMenuSuspend = new ControlInstanceCommand(
                "Suspe&nd",
                InstanceControlCommand.Suspend,
                serviceProvider)
            {
                Image = Resources.Pause_16,
                ActivityText = "Suspending VM instance"
            };
            this.ContextMenuReset = new ControlInstanceCommand(
                "Rese&t",
                InstanceControlCommand.Reset,
                serviceProvider)
            {
                Image = Resources.Reset_16,
                ActivityText = "Resetting VM instance"
            };
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> ContextMenuStart { get; }
        public IContextCommand<IProjectModelNode> ContextMenuResume { get; }
        public IContextCommand<IProjectModelNode> ContextMenuStop { get; }
        public IContextCommand<IProjectModelNode> ContextMenuSuspend { get; }
        public IContextCommand<IProjectModelNode> ContextMenuReset { get; }

        //---------------------------------------------------------------------
        // Commands classes.
        //---------------------------------------------------------------------

        internal class ControlInstanceCommand : ToolContextCommand<IProjectModelNode>
        {
            private readonly InstanceControlCommand controlCommand;
            private readonly IServiceProvider serviceProvider;

            public ControlInstanceCommand(
                string text,
                InstanceControlCommand controlCommand,
                IServiceProvider serviceProvider)
                : base(text)
            {
                this.controlCommand = controlCommand;
                this.serviceProvider = serviceProvider;
            }

            protected override bool IsAvailable(IProjectModelNode context)
            {
                return context is IProjectModelInstanceNode;
            }

            protected override bool IsEnabled(IProjectModelNode context)
            {
                var instance = (IProjectModelInstanceNode)context;
                switch (this.controlCommand)
                {
                    case InstanceControlCommand.Start:
                        return instance.CanStart;

                    case InstanceControlCommand.Stop:
                        return instance.CanStop;

                    case InstanceControlCommand.Suspend:
                        return instance.CanSuspend;

                    case InstanceControlCommand.Resume:
                        return instance.CanResume;

                    case InstanceControlCommand.Reset:
                        return instance.CanReset;

                    default:
                        Debug.Fail("Unknown InstanceControlCommand: " + this.controlCommand);
                        return false;
                }
            }

            public async override Task ExecuteAsync(IProjectModelNode context)
            {
                var instance = ((IProjectModelInstanceNode)context).Instance;
                var mainWindow = this.serviceProvider.GetService<IMainWindow>();

                if (this.serviceProvider.GetService<IConfirmationDialog>()
                    .Confirm(
                        mainWindow,
                        "Are you you sure you want to " +
                            $"{this.controlCommand.ToString().ToLower()} {instance.Name}?",
                        $"{this.controlCommand} {instance.Name}?",
                        $"{this.controlCommand} VM instance") != DialogResult.Yes)
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
                            $"{this.ActivityText} {instance.Name}...",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            await this.serviceProvider
                                .GetService<IInstanceControlService>()
                                .ControlInstanceAsync(
                                    instance,
                                    this.controlCommand,
                                    jobToken)
                                .ConfigureAwait(false);

                            return null;
                        })
                    .ConfigureAwait(true);  // Back to original (UI) thread.
            }
        }
    }
}
