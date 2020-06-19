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

using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using System;
using System.Linq;
using Google.Solutions.Common.Diagnostics;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog
{
    [Service(ServiceLifetime.Singleton)]
    [SkipCodeCoverage("All logic in view model")]
    internal partial class EventLogWindow 
        : ProjectExplorerTrackingToolWindow<EventLogViewModel>
    {
        private readonly EventLogViewModel viewModel;

        public EventLogWindow(IServiceProvider serviceProvider)
            : base(
                  serviceProvider.GetService<IMainForm>().MainPanel,
                  serviceProvider.GetService<IProjectExplorer>(),
                  serviceProvider.GetService<IEventService>(),
                  serviceProvider.GetService<IExceptionDialog>())
        {
            InitializeComponent();

            this.theme.ApplyTo(this.toolStrip);

            this.viewModel = new EventLogViewModel(this, serviceProvider);

            this.timeFrameComboBox.Items.AddRange(EventLogViewModel.AvailableTimeframes.ToArray());

            // Bind toolbar buttons.
            this.timeFrameComboBox.BindProperty(
                c => c.SelectedIndex,
                this.viewModel,
                m => m.SelectedTimeframeIndex,
                this.components);
            this.timeFrameComboBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsTimeframeComboBoxEnabled,
                this.components);

            this.refreshButton.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsRefreshButtonEnabled,
                this.components);

            this.includeLifecycleEventsButton.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsIncludeLifecycleEventsButtonChecked,
                this.components);
            this.includeSystemEventsButton.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsIncludeSystemEventsButtonChecked,
                this.components);

            // Bind list.
            this.list.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsEventListEnabled,
                this.components);
            this.list.BindColumn(0, e => e.Timestamp.ToString());
            this.list.BindColumn(1, e => GetInstanceName(e));
            this.list.BindColumn(2, e => e.Severity);
            this.list.BindColumn(3, e => e.Message);
            this.list.BindColumn(4, e => e.PrincipalEmail);
            this.list.BindImageIndex(e => GetImageIndex(e));
            this.list.BindCollection(this.viewModel.Events);
        }

        private static int GetImageIndex(EventBase e)
        {
            switch (e.Severity)
            {
                case "ERROR":
                    return 2;

                case "WARNING":
                    return 1;

                default:
                    return 0;
            }
        }

        private static string GetInstanceName(EventBase e)
        {
            if (e is VmInstanceEventBase vmEvent)
            {
                return vmEvent.InstanceReference?.Name;
            }
            else
            {
                return null;
            }
        }

        //---------------------------------------------------------------------
        // ProjectExplorerTrackingToolWindow.
        //---------------------------------------------------------------------

        protected override async Task SwitchToNodeAsync(IProjectExplorerNode node)
        {
            Debug.Assert(!InvokeRequired, "running on UI thread");
            await this.viewModel.SwitchToModelAsync(node)
                .ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void refreshButton_Click(object sender, EventArgs e)
        {
            this.viewModel.Refresh();
        }
    }

    public class EventsListView : BindableListView<EventBase>
    { }
}
