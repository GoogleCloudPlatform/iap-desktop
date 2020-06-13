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
using Google.Solutions.IapDesktop.Extensions.Activity.Services.ActivityLog;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.SchedulingReport;
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
                this.serviceProvider.GetService<AuditLogAdapter>(),
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

        private void ShowActivityLogs(IProjectExplorerNode contextNode)
        {
            this.serviceProvider.GetService<ActivityLogWindow>().ShowWindow();
        }

        public Extension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            var previewFeaturesEnabled = 
                this.serviceProvider.GetService<ApplicationSettingsRepository>()
                    .GetSettings()
                    .IsPreviewFeatureSetEnabled;
            if (!previewFeaturesEnabled)
            {
                // Do not register commands, making this extension
                // invisible.
                return;
            }

            // Add command to project explorer.
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();
            
            projectExplorer.AddCommand(
                "Analyze instance and node usage...",
                Resources.Report_16,
                new ProjectExplorerCommand(
                    context => context is IProjectExplorerProjectNode 
                            || context is IProjectExplorerCloudNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => CreateReport(context)));

            projectExplorer.AddCommand(
                "Activity logs",
                null,
                new ProjectExplorerCommand(
                    context => context is IProjectExplorerVmInstanceNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    context => ShowActivityLogs(context)));
        }
    }
}
