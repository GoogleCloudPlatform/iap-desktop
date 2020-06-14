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
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog
{
    [Service(ServiceLifetime.Singleton)]
    [SkipCodeCoverage("All logic in view model")]
    internal partial class EventLogWindow 
        : ProjectExplorerTrackingToolWindow<EventLogViewModel>
    {
        private const int CacheCapacity = 5;

        private readonly IServiceProvider serviceProvider;

        public EventLogWindow(IServiceProvider serviceProvider)
            : base(
                  serviceProvider.GetService<IMainForm>().MainPanel,
                  serviceProvider.GetService<IProjectExplorer>(),
                  serviceProvider.GetService<IEventService>(),
                  CacheCapacity
                  )
        {
            this.serviceProvider = serviceProvider;

            InitializeComponent();

            this.theme.ApplyTo(this.toolStrip);
            this.timeFrameComboBox.Items.AddRange(EventLogViewModel.AnalysisTimeframes.ToArray());
            this.timeFrameComboBox.SelectedIndex = 0;
        }

        //---------------------------------------------------------------------
        // ProjectExplorerTrackingToolWindow.
        //---------------------------------------------------------------------

        protected override async Task<EventLogViewModel> LoadViewModelAsync(
                IProjectExplorerNode node,
                CancellationToken token)
        {
            this.list.Enabled = this.toolStrip.Enabled = false;
             
            var model = await EventLogViewModel.LoadAsync(
                this.serviceProvider.GetService<AuditLogAdapter>(),
                node,
                (EventLogViewModel.Timeframe)this.timeFrameComboBox.SelectedItem,
                token).ConfigureAwait(true);

            this.list.Enabled = this.toolStrip.Enabled = true;

            return model;
        }

        protected override void BindViewModel(
            EventLogViewModel model, 
            IContainer bindingContainer)
        {
            // The timeframe selector should not change when we swap view models, so 
            // use a one-way binding.
            this.timeFrameComboBox.OnControlPropertyChange(
                c => c.SelectedIndex,
                index => model.SelectedTimeframe = EventLogViewModel.AnalysisTimeframes[index]);

            // Bind toolbar buttons.
            this.refreshButton.BindProperty(
                c => c.Enabled,
                model,
                m => m.IsRefreshButtonEnabled,
                bindingContainer);
            this.lifecycleEventsDropDown.BindProperty(
                c => c.Enabled,
                model,
                m => m.IsLifecycleEventDropDownEnabled,
                bindingContainer);

            this.includeLifecycleEventsButton.BindProperty(
                c => c.Checked,
                model,
                m => m.IsIncludeLifecycleEventsButtonEnabled,
                bindingContainer);
            this.includeSystemEventsButton.BindProperty(
                c => c.Checked,
                model,
                m => m.IsIncludeSystemEventsButtonEnabled,
                bindingContainer);

            // Bind list.
            this.list.BindColumn(0, e => e.Timestamp.ToString());
            this.list.BindColumn(1, e => e.Severity);
            this.list.BindColumn(2, e => e.Message);
            this.list.BindColumn(3, e => e.Status?.Message);
            this.list.BindColumn(4, e => e.PrincipalEmail);
            this.list.BindCollection(model.Events);
        }
    }

    public class EventsListView : BindableListView<EventBase>
    { }
}
