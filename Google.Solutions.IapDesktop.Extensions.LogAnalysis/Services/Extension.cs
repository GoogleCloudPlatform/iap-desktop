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
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Properties;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services
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

            var builder = new AuditLogDownloader(
                this.serviceProvider.GetService<IAuthorizationAdapter>(),
                projectIds,
                dialog.SelectedStartDate);

            var view = new ReportView(
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

            // Add command to project explorer.
            serviceProvider.GetService<IProjectExplorer>()
                .AddCommand(
                    "Analyze instance and node usage...",
                    Resources.Report_16,
                    new ProjectExplorerCommand(
                        context => context is IProjectExplorerProjectNode 
                                || context is IProjectExplorerCloudNode
                            ? CommandState.Enabled
                            : CommandState.Unavailable,
                        context => CreateReport(context)));
        }
    }

}
