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
using Google.Solutions.Common.Threading;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Cache;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.EventLog
{
    [Service]
    public class EventLogViewModel
        : ModelCachingViewModelBase<IProjectModelNode, EventLogModel>
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

        private readonly ICloudConsoleClient cloudConsoleAdapter;
        private readonly IJobService jobService;
        private readonly Service<IAuditLogClient> auditLogAdapter;

        private EventBase? selectedEvent;
        private int selectedTimeframeIndex = 0;
        private bool isEventListEnabled = false;
        private bool isRefreshButtonEnabled = false;
        private bool isTimeframeComboBoxEnabled = false;

        private bool includeSystemEvents = true;
        private bool includeLifecycleEvents = true;
        private bool includeAccessEvents = true;

        private string windowTitle = DefaultWindowTitle;

        private Timeframe SelectedTimeframe => AvailableTimeframes[this.selectedTimeframeIndex];

        public EventLogViewModel(IServiceProvider serviceProvider)
            : base(ModelCacheCapacity)
        {
            this.cloudConsoleAdapter = serviceProvider.GetService<ICloudConsoleClient>();
            this.jobService = serviceProvider.GetService<IJobService>();
            this.auditLogAdapter = serviceProvider.GetService<Service<IAuditLogClient>>();

            this.Events = new RangeObservableCollection<EventBase>();
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public EventBase? SelectedEvent
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
                _ = InvalidateAsync().ConfigureAwait(true);
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

        public void Refresh()
        {
            _ = InvalidateAsync().ConfigureAwait(true);
        }

        public void OpenSelectedEventInCloudConsole()
        {
            if (this.SelectedEvent != null)
            {
                this.cloudConsoleAdapter.OpenVmInstanceLogDetails(
                    this.SelectedEvent.LogRecord.ProjectId,
                    this.SelectedEvent.LogRecord.InsertId,
                    this.SelectedEvent.Timestamp);
            }
        }

        public void OpenInCloudConsole()
        {
            Debug.Assert(!(this.ModelKey is IProjectModelCloudNode));
            this.cloudConsoleAdapter.OpenLogs(this.ModelKey);
        }

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        protected override async Task<EventLogModel?> LoadModelAsync(
            IProjectModelNode node,
            CancellationToken token)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(node))
            {
                IEnumerable<ulong>? instanceIdFilter;
                IEnumerable<string>? zonesFilter;
                string projectIdFilter;
                string displayName;

                if (node is IProjectModelInstanceNode vmNode)
                {
                    displayName = vmNode.Instance.Name;
                    instanceIdFilter = new[] { vmNode.InstanceId };
                    zonesFilter = null;
                    projectIdFilter = vmNode.Instance.ProjectId;
                }
                else if (node is IProjectModelZoneNode zoneNode)
                {
                    displayName = zoneNode.Zone.Name;
                    instanceIdFilter = null;
                    zonesFilter = new[] { zoneNode.Zone.Name };
                    projectIdFilter = zoneNode.Zone.ProjectId;
                }
                else if (node is IProjectModelProjectNode projectNode)
                {
                    displayName = projectNode.Project.ProjectId;
                    instanceIdFilter = null;
                    zonesFilter = null;
                    projectIdFilter = projectNode.Project.ProjectId;
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
                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    return await this.jobService.RunAsync(
                        new JobDescription(
                            $"Loading logs for {displayName}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            using (var combinedTokenSource = jobToken.Combine(token))
                            {
                                var model = new EventLogModel(displayName);
                                await this.auditLogAdapter
                                    .Activate()
                                    .ProcessInstanceEventsAsync(
                                        new[] { projectIdFilter },
                                        zonesFilter,
                                        instanceIdFilter,
                                        DateTime.UtcNow.Subtract(this.SelectedTimeframe.Duration),
                                        model,
                                        combinedTokenSource.Token)
                                    .ConfigureAwait(false);

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
            this.SelectedEvent = null;

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
                    .Where(e => e.Category != EventCategory.Lifecycle || this.includeLifecycleEvents)
                    .Where(e => e.Category != EventCategory.System || this.includeSystemEvents)
                    .Where(e => e.Category != EventCategory.Access || this.includeAccessEvents));
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
