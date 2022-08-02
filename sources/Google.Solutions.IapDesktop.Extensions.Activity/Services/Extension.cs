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
using Google.Solutions.IapDesktop.Application.ObjectModel.Commands;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Activity.Properties;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Views.EventLog;
using Google.Solutions.IapDesktop.Extensions.Activity.Views.SerialOutput;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class Extension
    {
        private readonly IServiceProvider serviceProvider;

        public Extension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            //
            // Add commands to project explorer.
            //

            var reportCommand = projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Report",
                    context => context is IProjectModelProjectNode
                            || context is IProjectModelCloudNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => { }));

            reportCommand.AddCommand(
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
