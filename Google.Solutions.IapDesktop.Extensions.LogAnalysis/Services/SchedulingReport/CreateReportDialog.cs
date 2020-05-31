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
using Google.Solutions.IapDesktop.Application.Services.Integration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            serviceProvider.GetService<ProjectInventoryService>()
                .ListProjectsAsync()
                .ContinueWith(
                    t =>
                    {
                        var projectIds = t.Result
                            .Select(p => p.Name)
                            .OrderBy(id => id)
                            .ToArray();
                        this.projectsList.Items.AddRange(projectIds);
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.FromCurrentSynchronizationContext());

            // Populate list of time frames.
            this.timeFrameList.Items.AddRange(new[]
            {
                new TimeFrameItem(
                    "Last month",
                    DateTime.Now.Date.AddMonths(-1)),
                new TimeFrameItem(
                    "Last 3 months",
                    DateTime.Now.Date.AddMonths(-3)),
                new TimeFrameItem(
                    "Last 6 months",
                    DateTime.Now.Date.AddMonths(-6)),
                new TimeFrameItem(
                    "Last 12 months",
                    DateTime.Now.Date.AddMonths(-12))
            });
            this.timeFrameList.SelectedIndex = 1;
        }

        public DateTime SelectedStartDate
            => ((TimeFrameItem)this.timeFrameList.SelectedItem).StartDate;

        public string SelectedProject
        {
            get => (string)this.projectsList.SelectedItem;
            set => this.projectsList.SelectedItem = value;
        }
    }
}
