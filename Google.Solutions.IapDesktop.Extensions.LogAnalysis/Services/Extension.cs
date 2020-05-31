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
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport;
using System;
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

        private void CreateReport(string projectId)
        {
            var dialog = this.serviceProvider.GetService<CreateReportDialog>();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                MessageBox.Show(dialog.SelectedProject);
            }
        }

        public Extension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            // Add command to project explorer.
            serviceProvider.GetService<IProjectExplorer>()
                .AddCommand(
                    "Instances/nodes usage report...",
                    null,
                    new ProjectExplorerCommand(
                        context => context is IProjectExplorerProjectNode
                            ? CommandState.Enabled
                            : CommandState.Unavailable,
                        context => CreateReport(((IProjectExplorerProjectNode)context).ProjectId)));
        }
    }

}
