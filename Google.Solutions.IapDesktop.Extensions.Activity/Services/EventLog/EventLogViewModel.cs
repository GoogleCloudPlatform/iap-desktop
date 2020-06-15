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
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog
{
    internal class EventLogViewModel 
        : ModelCachingViewModelBase<IProjectExplorerVmInstanceNode, EventLogModel>
    {
        private const int ModelCacheCapacity = 5;
        public static readonly ReadOnlyCollection<Timeframe> AvailableTimeframes 
            = new ReadOnlyCollection<Timeframe>(new List<Timeframe>()
        {
            new Timeframe(TimeSpan.FromDays(7), "Last 7 days"),
            new Timeframe(TimeSpan.FromDays(14), "Last 14 days"),
            new Timeframe(TimeSpan.FromDays(30), "Last 30 days")
        });

        private readonly IServiceProvider serviceProvider;

        private int selectedTimeframeIndex = 0;
        private bool isRefreshButtonEnabled = false;
        private bool isTimeframeComboBoxEnabled = false;
        private bool includeSystemEvents = true;
        private bool includeLifecycleEvents = true;

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

        public bool IsLifecycleEventDropDownEnabled
        {
            get => true;
            set { }
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
                Invalidate();
            }
        }

        public bool IsIncludeSystemEventsButtonEnabled
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

        public bool IsIncludeLifecycleEventsButtonEnabled
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

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void Refresh() => Invalidate();

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        protected override async Task<EventLogModel> LoadModelAsync(
            IProjectExplorerVmInstanceNode node,
            CancellationToken token)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(node.InstanceName))
            {
                this.IsRefreshButtonEnabled = 
                    this.IsTimeframeComboBoxEnabled = false;
                try
                {
                    // If the user is holding down the arrow key in the project explorer,
                    // we might get a flurry of requests. To catch that, introduce a short,
                    // cancellable-delay.
                    await Task.Delay(300, token).ConfigureAwait(true);


                    var jobService = this.serviceProvider.GetService<IJobService>();
                    var auditLogAdapter = this.serviceProvider.GetService<AuditLogAdapter>();

                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    var model = new EventLogModel();
                    await jobService.RunInBackground<object>(
                        new JobDescription(
                            $"Loading logs for {node.InstanceName}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            await auditLogAdapter.ListInstanceEventsAsync(
                                new[] { node.ProjectId },
                                new[] { node.InstanceId },
                                DateTime.UtcNow.Subtract(this.SelectedTimeframe.Duration),
                                model,
                                token).ConfigureAwait(false);
                            return null;
                        }).ConfigureAwait(true);  // Back to original (UI) thread.
                    return model;
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
            this.Events.AddRange(this.Model.Events
                .Where(e => !e.LogRecord.IsActivityEvent || this.includeLifecycleEvents)
                .Where(e => !e.LogRecord.IsSystemEvent || this.includeSystemEvents));
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
