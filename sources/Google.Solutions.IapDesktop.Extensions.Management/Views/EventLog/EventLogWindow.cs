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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Management.Data.Events;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Extensions.Management.Views.EventLog
{
    [Service(ServiceLifetime.Singleton)]
    [SkipCodeCoverage("All logic in view model")]
    internal partial class EventLogWindow
        : ProjectExplorerTrackingToolWindow<EventLogViewModel>
    {
        private readonly EventLogViewModel viewModel;

        public EventLogWindow(IServiceProvider serviceProvider)
            : base(serviceProvider, WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide)
        {
            InitializeComponent();

            var theme = serviceProvider.GetService<IThemeService>();
            theme.ToolWindowTheme.ApplyTheme(this.toolStrip);
            theme.ToolWindowTheme.ApplyTheme(this.list);

            this.viewModel = new EventLogViewModel(this, serviceProvider);

            this.timeFrameComboBox.Items.AddRange(EventLogViewModel.AvailableTimeframes.ToArray());

            this.components.Add(this.viewModel.OnPropertyChange(
                m => m.WindowTitle,
                title =>
                {
                    // NB. Update properties separately instead of using multi-assignment,
                    // otherwise the title does not update properly.
                    this.TabText = title;
                    this.Text = title;
                }));

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
            this.includeAccessEventsButton.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsIncludeAccessEventsButtonChecked,
                this.components);

            this.openInCloudConsoleToolStripMenuItem.BindProperty(
                b => b.Enabled,
                this.viewModel,
                m => m.IsOpenSelectedEventInCloudConsoleButtonEnabled,
                this.components);

            // Bind list.
            this.list.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsEventListEnabled,
                this.components);
            this.list.BindProperty(
                c => c.SelectedModelItem,
                this.viewModel,
                m => m.SelectedEvent,
                this.components);

            this.list.BindColumn(0, e => e.Timestamp.ToString());
            this.list.BindColumn(1, e => GetInstanceName(e));
            this.list.BindColumn(2, e => e.Severity);
            this.list.BindColumn(3, e => e.Message);
            this.list.BindColumn(4, e => e.PrincipalEmail);
            this.list.BindColumn(5, e => e.DeviceId);
            this.list.BindColumn(6, e => e.DeviceState);
            this.list.BindColumn(7, e => string.Join(", ", e.AccessLevels.Select(l => l.AccessLevel)));

            this.list.BindImageIndex(e => GetImageIndex(e));
            this.list.BindCollection(this.viewModel.Events);

            this.list.AddCopyCommands();
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
            if (e is InstanceEventBase vmEvent)
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

        protected override async Task SwitchToNodeAsync(IProjectModelNode node)
        {
            Debug.Assert(!this.InvokeRequired, "running on UI thread");
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

        private void openInCloudConsoleToolStripMenuItem_Click(object sender, EventArgs e)
            => this.viewModel.OpenSelectedEventInCloudConsole();

        private void list_DoubleClick(object sender, EventArgs e)
            => this.viewModel.OpenSelectedEventInCloudConsole();

        private void openLogsButton_Click(object sender, EventArgs e)
            => this.viewModel.OpenInCloudConsole();
    }

    public class EventsListView : BindableListView<EventBase>
    { }
}
