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
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.EventLog
{
    [Service(ServiceLifetime.Singleton)]
    [SkipCodeCoverage("All logic in view model")]
    internal partial class EventLogView
        : ProjectExplorerTrackingToolWindow<EventLogViewModel>, IView<EventLogViewModel>
    {
        private Bound<EventLogViewModel> viewModel;

        public EventLogView(IServiceProvider serviceProvider)
            : base(serviceProvider, WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide)
        {
            InitializeComponent();

            this.list.AddCopyCommands();
        }

        public void Bind(
            EventLogViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.viewModel.Value = viewModel;

            this.timeFrameComboBox.Items.AddRange(EventLogViewModel.AvailableTimeframes.ToArray());

            viewModel.OnPropertyChange(
                m => m.WindowTitle,
                title =>
                {
                    // NB. Update properties separately instead of using multi-assignment,
                    // otherwise the title does not update properly.
                    this.TabText = title;
                    this.Text = title;
                },
                bindingContext);

            // Bind toolbar buttons.
            this.timeFrameComboBox.BindProperty(
                c => c.SelectedIndex,
                viewModel,
                m => m.SelectedTimeframeIndex,
                bindingContext);
            this.timeFrameComboBox.BindProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsTimeframeComboBoxEnabled,
                bindingContext);

            this.refreshButton.BindProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsRefreshButtonEnabled,
                bindingContext);

            this.includeLifecycleEventsButton.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsIncludeLifecycleEventsButtonChecked,
                bindingContext);
            this.includeSystemEventsButton.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsIncludeSystemEventsButtonChecked,
                bindingContext);
            this.includeAccessEventsButton.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsIncludeAccessEventsButtonChecked,
                bindingContext);

            this.openInCloudConsoleToolStripMenuItem.BindReadonlyProperty(
                b => b.Enabled,
                viewModel,
                m => m.IsOpenSelectedEventInCloudConsoleButtonEnabled,
                bindingContext);
            this.openLogsButton.BindReadonlyProperty(
                b => b.Enabled,
                viewModel,
                m => m.IsOpenSelectedEventInCloudConsoleButtonEnabled,
                bindingContext);

            // Bind list.
            this.list.BindProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsEventListEnabled,
                bindingContext);
            this.list.BindProperty(
                c => c.SelectedModelItem,
                viewModel,
                m => m.SelectedEvent,
                bindingContext);

            this.list.BindColumn(0, e => e.Timestamp.ToString());
            this.list.BindColumn(1, e => GetInstanceName(e) ?? string.Empty);
            this.list.BindColumn(2, e => e.Severity ?? string.Empty);
            this.list.BindColumn(3, e => e.Message);
            this.list.BindColumn(4, e => e.Principal ?? string.Empty);
            this.list.BindColumn(5, e => e.DeviceId ?? string.Empty);
            this.list.BindColumn(6, e => e.DeviceState ?? string.Empty);
            this.list.BindColumn(7, e => string.Join(", ", e.AccessLevels.Select(l => l.AccessLevel)));

            this.list.BindImageIndex(e => GetImageIndex(e));
            this.list.BindCollection(viewModel.Events);
        }

        private static int GetImageIndex(EventBase e)
        {
            return e.Severity switch
            {
                "ERROR" => 2,
                "WARNING" => 1,
                _ => 0,
            };
        }

        private static string? GetInstanceName(EventBase e)
        {
            if (e is InstanceEventBase vmEvent)
            {
                return vmEvent.Instance?.Name;
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
            await this.viewModel.Value.SwitchToModelAsync(node)
                .ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void refreshButton_Click(object sender, EventArgs e)
        {
            this.viewModel.Value.Refresh();
        }

        private void openInCloudConsoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.viewModel.Value.OpenSelectedEventInCloudConsole();
        }

        private void list_DoubleClick(object sender, EventArgs e)
        {
            this.viewModel.Value.OpenSelectedEventInCloudConsole();
        }

        private void openLogsButton_Click(object sender, EventArgs e)
        {
            this.viewModel.Value.OpenInCloudConsole();
        }
    }

    public class EventsListView : BindableListView<EventBase>
    { }
}
