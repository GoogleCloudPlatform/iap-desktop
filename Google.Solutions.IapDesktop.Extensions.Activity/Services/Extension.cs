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
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Activity.Properties;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.SchedulingReport;
using System;
using System.Linq;
using System.Windows.Forms;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.SerialOutput;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class Extension
    {
        private readonly IServiceProvider serviceProvider;

        private void CreateReport(IProjectExplorerNode contextNode)
        {
            var dialog = this.serviceProvider.GetService<CreateReportDialog>();
            if (contextNode is IProjectExplorerProjectNode projectNode)
            {
                dialog.SelectProjectId(projectNode.ProjectId);
            }

            var mainForm = this.serviceProvider.GetService<IMainForm>();
            if (dialog.ShowDialog(mainForm.Window) == DialogResult.Cancel ||
                !dialog.SelectedProjectIds.Any())
            {
                return;
            }

            var projectIds = dialog.SelectedProjectIds;

            var builder = new AuditLogReportBuilder(
                this.serviceProvider.GetService<IAuditLogAdapter>(),
                this.serviceProvider.GetService<IComputeEngineAdapter>(),
                projectIds,
                dialog.SelectedStartDate);

            var view = new ReportPaneView(
                ReportViewModel.CreateReportName(projectIds),
                builder,
                serviceProvider);
            view.ShowOrActivate(
                mainForm.MainPanel,
                WeifenLuo.WinFormsUI.Docking.DockState.Document);
        }

        public Extension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            //
            // Add commands to project explorer.
            //

            var previewFeaturesEnabled = 
                this.serviceProvider.GetService<ApplicationSettingsRepository>()
                    .GetSettings()
                    .IsPreviewFeatureSetEnabled;
            if (previewFeaturesEnabled)
            {
                var reportCommand = projectExplorer.Commands.AddCommand(
                    new Command<IProjectExplorerNode>(
                        "Report",
                        context => context is IProjectExplorerProjectNode
                                || context is IProjectExplorerCloudNode
                            ? CommandState.Enabled
                            : CommandState.Unavailable,
                        context => { }));

                reportCommand.AddCommand(
                    new Command<IProjectExplorerNode>(
                        "Instance and node usage...",
                        context => CommandState.Enabled,
                        context => CreateReport(context))
                    {
                        Image = Resources.Report_16
                    });
            }

            projectExplorer.Commands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Show serial port &output",
                    context => EventLogViewModel.IsNodeSupported(context)
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => this.serviceProvider.GetService<SerialOutputWindow>().ShowWindow())
                {
                    Image = Resources.Log_16
                },
                5);
            projectExplorer.Commands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Show &event log",
                    context => EventLogViewModel.IsNodeSupported(context)
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => this.serviceProvider.GetService<EventLogWindow>().ShowWindow())
                {
                    Image = Resources.EventLog_16
                },
                6);


            //
            // Add commands to main menu.
            //
            var mainForm = serviceProvider.GetService<IMainForm>();
            mainForm.ViewCommands.AddCommand(
                new Command<IMainForm>(
                    "Serial port &output",
                    pseudoContext => CommandState.Enabled,
                    pseudoContext => this.serviceProvider.GetService<SerialOutputWindow>().ShowWindow())
                {
                    Image = Resources.Log_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.O
                });
            mainForm.ViewCommands.AddCommand(
                new Command<IMainForm>(
                    "&Event log window",
                    pseudoContext => CommandState.Enabled,
                    pseudoContext => this.serviceProvider.GetService<EventLogWindow>().ShowWindow())
                {
                    Image = Resources.EventLog_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.E
                });
        }
    }
}
