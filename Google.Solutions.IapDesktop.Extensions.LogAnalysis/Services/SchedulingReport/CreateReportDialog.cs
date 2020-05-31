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
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{
    [Service(ServiceLifetime.Transient)]
    public partial class CreateReportDialog : Form
    {
        private class TimeFrameItem
        {
            public DateTime StartDate { get; }
            public string Text { get; }
            
            public TimeFrameItem(string text, DateTime startDate)
            {
                this.Text = text;
                this.StartDate = startDate;
            }

            public override string ToString() => this.Text;
        }

        public CreateReportDialog(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // Populate list of projects.
            var projectIds = serviceProvider.GetService<ConnectionSettingsRepository>()
                .ListProjectSettings()
                .Select(p => p.ProjectId);

            this.projectsList.Items.AddRange(projectIds
                .OrderBy(id => id)
                .ToArray());

            // Populate list of time frames.
            this.timeFrameList.Items.AddRange(new[]
            {
#if DEBUG
                new TimeFrameItem(
                    "DEBUG: Last week",
                    DateTime.UtcNow.Date.AddDays(-7)),
#endif
                new TimeFrameItem(
                    "Last month",
                    DateTime.UtcNow.Date.AddMonths(-1)),
                new TimeFrameItem(
                    "Last 3 months",
                    DateTime.UtcNow.Date.AddMonths(-3)),
                new TimeFrameItem(
                    "Last 6 months",
                    DateTime.UtcNow.Date.AddMonths(-6)),
                new TimeFrameItem(
                    "Last 12 months",
                    DateTime.UtcNow.Date.AddMonths(-12))
            });
            this.timeFrameList.SelectedIndex = 1;
        }

        public DateTime SelectedStartDate
            => ((TimeFrameItem)this.timeFrameList.SelectedItem).StartDate;

        public IEnumerable<string> SelectedProjectIds => this.projectsList.CheckedItems.Cast<string>();

        public void SelectProjectId(string projectId)
        {
            var index = this.projectsList.Items.IndexOf(projectId);
            if (index >= 0)
            {
                this.projectsList.SetItemChecked(index, true);
            }
        }

        private void projectsList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // NB. The event fires before the CheckedIndices property
            // is updated.
            this.okButton.Enabled = this.projectsList.CheckedIndices.Count > 1 ||
                e.NewValue == CheckState.Checked;
        }
    }
}
