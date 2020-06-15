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
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog
{
    internal class EventLogViewModel 
        : ModelCachingViewModelBase<IProjectExplorerVmInstanceNode, EventLogModel>
    {
        private const int ModelCacheCapacity = 5;
        public static readonly ReadOnlyCollection<Timeframe> AnalysisTimeframes 
            = new ReadOnlyCollection<Timeframe>(new List<Timeframe>()
        {
            new Timeframe(TimeSpan.FromDays(7), "Last 7 days"),
            new Timeframe(TimeSpan.FromDays(14), "Last 14 days"),
            new Timeframe(TimeSpan.FromDays(30), "Last 30 days")
        });

        private readonly IServiceProvider serviceProvider;

        private Timeframe selectedTimeframe = AnalysisTimeframes[0];
        private bool includeSystemEvents = true;
        private bool includeLifecycleEvents = true;

        public EventLogViewModel(IServiceProvider serviceProvider)
            : base(ModelCacheCapacity)
        {
            this.serviceProvider = serviceProvider;

            this.Events = new RangeObservableCollection<EventBase>();
        }

        public bool IsRefreshButtonEnabled
        {
            get => true;
            set { }
        }

        public bool IsLifecycleEventDropDownEnabled
        {
            get => true;
            set { }
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public RangeObservableCollection<EventBase> Events { get; }

        public Timeframe SelectedTimeframe
        {
            get => this.selectedTimeframe;
            set
            {
                this.selectedTimeframe = value;
                RaisePropertyChange();
            }
        }

        public bool IsIncludeSystemEventsButtonEnabled
        {
            get => this.includeSystemEvents;
            set
            {
                this.includeSystemEvents = value;
                RaisePropertyChange();
            }
        }

        public bool IsIncludeLifecycleEventsButtonEnabled
        {
            get => this.includeLifecycleEvents;
            set
            {
                this.includeLifecycleEvents = value;
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        //public async Task RefreshAsync()
        //{
            
        //}

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        protected override async Task<EventLogModel> LoadModelAsync(
            IProjectExplorerVmInstanceNode node,
            CancellationToken token)
        {
            // If the user is holding down the arrow key in the project explorer,
            // we might get a flurry of requests. To catch that, introduce a short,
            // cancellable-delay.
            await Task.Delay(300, token).ConfigureAwait(true);

            var methods = new List<string>();
            if (this.includeLifecycleEvents)
            {
                methods.AddRange(EventFactory.LifecycleEventMethods);
            }

            if (this.includeLifecycleEvents)
            {
                methods.AddRange(EventFactory.SystemEventMethods);
            }

            var model = new EventLogModel(
                null, // All,
                methods);

            var jobService = this.serviceProvider.GetService<IJobService>();
            var auditLogAdapter = this.serviceProvider.GetService<AuditLogAdapter>();

            // Load data using a job so that the task is retried in case
            // of authentication issues.
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

        protected override void ApplyModel(
            EventLogModel model,
            bool cached)
        {
            this.Events.Clear();
            this.Events.AddRange(model.Events);
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
