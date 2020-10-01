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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Views.EventLog
{
    internal class EventLogViewModel
        : ModelCachingViewModelBase<IProjectExplorerNode, EventLogModel>
    {
        private const int ModelCacheCapacity = 5;
        internal const string DefaultWindowTitle = "Event log";

        public static readonly ReadOnlyCollection<Timeframe> AvailableTimeframes
            = new ReadOnlyCollection<Timeframe>(new List<Timeframe>()
        {
            new Timeframe(TimeSpan.FromDays(7), "Last 7 days"),
            new Timeframe(TimeSpan.FromDays(14), "Last 14 days"),
            new Timeframe(TimeSpan.FromDays(30), "Last 30 days")
        });

        private readonly IServiceProvider serviceProvider;

        private EventBase selectedEvent;
        private int selectedTimeframeIndex = 0;
        private bool isEventListEnabled = false;
        private bool isRefreshButtonEnabled = false;
        private bool isTimeframeComboBoxEnabled = false;

        private bool includeSystemEvents = true;
        private bool includeLifecycleEvents = true;
        private bool includeAccessEvents = true;

        private string windowTitle = DefaultWindowTitle;

        private Timeframe SelectedTimeframe => AvailableTimeframes[this.selectedTimeframeIndex];

        public EventLogViewModel(
            IWin32Window view,
            IServiceProvider serviceProvider)
            : base(ModelCacheCapacity)
        {
            this.View = view;
            this.serviceProvider = serviceProvider;

            this.Events = new RangeObservableCollection<EventBase>();
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public EventBase SelectedEvent
        {
            get => this.selectedEvent;
            set
            {
                this.selectedEvent = value;

                RaisePropertyChange();
                RaisePropertyChange((EventLogViewModel m) => m.IsOpenSelectedEventInCloudConsoleButtonEnabled);
            }
        }

        public bool IsOpenSelectedEventInCloudConsoleButtonEnabled
        {
            get => this.selectedEvent != null;
        }

        public bool IsEventListEnabled
        {
            get => this.isEventListEnabled;
            set
            {
                this.isEventListEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsRefreshButtonEnabled
        {
            get => this.isRefreshButtonEnabled;
            set
            {
                this.isRefreshButtonEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsTimeframeComboBoxEnabled
        {
            get => this.isTimeframeComboBoxEnabled;
            set
            {
                this.isTimeframeComboBoxEnabled = value;
                RaisePropertyChange();
            }
        }

        public RangeObservableCollection<EventBase> Events { get; }

        public int SelectedTimeframeIndex
        {
            get => this.selectedTimeframeIndex;
            set
            {
                this.selectedTimeframeIndex = value;
                RaisePropertyChange();

                // Reload from backend.
                InvalidateAsync().ConfigureAwait(true);
            }
        }

        public bool IsIncludeSystemEventsButtonChecked
        {
            get => this.includeSystemEvents;
            set
            {
                this.includeSystemEvents = value;
                RaisePropertyChange();

                // Reapply filters.
                ApplyModel(true);
            }
        }

        public bool IsIncludeLifecycleEventsButtonChecked
        {
            get => this.includeLifecycleEvents;
            set
            {
                this.includeLifecycleEvents = value;
                RaisePropertyChange();

                // Reapply filters.
                ApplyModel(true);
            }
        }

        public bool IsIncludeAccessEventsButtonChecked
        {
            get => this.includeAccessEvents;
            set
            {
                this.includeAccessEvents = value;
                RaisePropertyChange();

                // Reapply filters.
                ApplyModel(true);
            }
        }

        public string WindowTitle
        {
            get => this.windowTitle;
            set
            {
                this.windowTitle = value;
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void Refresh() => InvalidateAsync().ConfigureAwait(true);

        public void OpenSelectedEventInCloudConsole()
        {
            if (this.SelectedEvent != null)
            {
                this.serviceProvider.GetService<CloudConsoleService>().OpenVmInstanceLogDetails(
                    this.SelectedEvent.LogRecord.ProjectId,
                    this.SelectedEvent.LogRecord.InsertId,
                    this.SelectedEvent.Timestamp);
            }
        }

        public void OpenInCloudConsole()
        {
            Debug.Assert(!(this.ModelKey is IProjectExplorerCloudNode));
            this.serviceProvider.GetService<CloudConsoleService>().OpenLogs(this.ModelKey);
        }

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        public static CommandState GetCommandState(IProjectExplorerNode node)
        {
            if (node is IProjectExplorerProjectNode
                || node is IProjectExplorerZoneNode
                || node is IProjectExplorerVmInstanceNode)
            {
                return CommandState.Enabled;
            }
            else
            {
                return CommandState.Unavailable;
            }
        }

        protected override async Task<EventLogModel> LoadModelAsync(
            IProjectExplorerNode node,
            CancellationToken token)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(node))
            {
                IEnumerable<ulong> instanceIdFilter;
                IEnumerable<string> zonesFilter;
                string projectIdFilter;
                string displayName;

                if (node is IProjectExplorerVmInstanceNode vmNode)
                {
                    displayName = vmNode.InstanceName;
                    instanceIdFilter = new[] { vmNode.InstanceId };
                    zonesFilter = null;
                    projectIdFilter = vmNode.ProjectId;
                }
                else if (node is IProjectExplorerZoneNode zoneNode)
                {
                    displayName = zoneNode.ZoneId;
                    instanceIdFilter = null;
                    zonesFilter = new[] { zoneNode.ZoneId };
                    projectIdFilter = zoneNode.ProjectId;
                }
                else if (node is IProjectExplorerProjectNode projectNode)
                {
                    displayName = projectNode.ProjectId;
                    instanceIdFilter = null;
                    zonesFilter = null;
                    projectIdFilter = projectNode.ProjectId;
                }
                else
                {
                    // Unknown/unsupported node.
                    return null;
                }

                this.IsRefreshButtonEnabled =
                    this.IsTimeframeComboBoxEnabled = false;
                try
                {
                    var jobService = this.serviceProvider.GetService<IJobService>();
                    var auditLogAdapter = this.serviceProvider.GetService<IAuditLogAdapter>();

                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    return await jobService.RunInBackground(
                        new JobDescription(
                            $"Loading logs for {displayName}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            using (var combinedTokenSource = jobToken.Combine(token))
                            {
                                var model = new EventLogModel(displayName);
                                await auditLogAdapter.ProcessInstanceEventsAsync(
                                    new[] { projectIdFilter },
                                    zonesFilter,
                                    instanceIdFilter,
                                    DateTime.UtcNow.Subtract(this.SelectedTimeframe.Duration),
                                    model,
                                    combinedTokenSource.Token).ConfigureAwait(false);
                                return model;
                            }
                        }).ConfigureAwait(true);  // Back to original (UI) thread.
                }
                finally
                {
                    this.IsRefreshButtonEnabled =
                        this.IsTimeframeComboBoxEnabled = true;
                }
            }
        }

        protected override void ApplyModel(bool cached)
        {
            this.Events.Clear();

            if (this.Model == null)
            {
                // Unsupported node.
                this.IsEventListEnabled = false;
                this.WindowTitle = DefaultWindowTitle;
            }
            else
            {
                this.IsEventListEnabled = true;
                this.WindowTitle = DefaultWindowTitle + $": {this.Model.DisplayName}";

                this.Events.AddRange(this.Model.Events
                    .Where(e => !e.LogRecord.IsActivityEvent || this.includeLifecycleEvents)
                    .Where(e => !e.LogRecord.IsSystemEvent || this.includeSystemEvents)
                    .Where(e => !e.LogRecord.IsDataAccessEvent || this.includeAccessEvents));
            }
        }

        //---------------------------------------------------------------------

        public class Timeframe
        {
            public TimeSpan Duration { get; }

            public string Description { get; }

            public Timeframe(TimeSpan duration, string description)
            {
                this.Duration = duration;
                this.Description = description;
            }

            public override string ToString()
            {
                return this.Description;
            }
        }
    }
}
