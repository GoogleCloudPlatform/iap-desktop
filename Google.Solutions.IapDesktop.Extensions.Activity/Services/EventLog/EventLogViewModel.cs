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
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog
{
    internal class EventLogViewModel : ViewModelBase
    {
        public static readonly ReadOnlyCollection<Timeframe> AnalysisTimeframes 
            = new ReadOnlyCollection<Timeframe>(new List<Timeframe>()
        {
            new Timeframe(TimeSpan.FromDays(7), "Last 7 days"),
            new Timeframe(TimeSpan.FromDays(14), "Last 14 days"),
            new Timeframe(TimeSpan.FromDays(30), "Last 30 days")
        });

        private readonly IJobService jobService;
        private readonly AuditLogAdapter auditLogAdapter;
        private readonly IProjectExplorerVmInstanceNode node;

        private Timeframe selectedTimeframe = null;
        private bool includeSystemEvents = true;
        private bool includeLifecycleEvents = true;

        public EventLogViewModel(
            IJobService jobService,
            AuditLogAdapter auditLogAdapter,
            IProjectExplorerVmInstanceNode node,
            Timeframe initialTimeframe)
        {
            this.jobService = jobService;
            this.auditLogAdapter = auditLogAdapter;
            this.node = node;

            this.selectedTimeframe = initialTimeframe;

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
        // IEventProcessor.
        //---------------------------------------------------------------------


        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public async Task RefreshAsync()
        {
            var methods = new List<string>();
            if (this.includeLifecycleEvents)
            {
                methods.AddRange(EventFactory.LifecycleEventMethods);
            }

            if (this.includeLifecycleEvents)
            {
                methods.AddRange(EventFactory.SystemEventMethods);
            }

            // Instead of adding the events straight to this.Events, accumulate
            // them first. That way, we :
            // - avoid having the collection be accessed
            //   from a different thread (which in turn would cause events being
            //   fired on that thread)
            // - can update the collection in one batch, minimizing the number
            //   of events.
            var accumulator = new EventAccumulator(
                null, // All,
                methods);

            // Load data using a job so that the task is retried in case
            // of authentication issues.
            await this.jobService.RunInBackground<object>(
                new JobDescription(
                    $"Loading logs for {this.node.InstanceName}",
                    JobUserFeedbackType.BackgroundFeedback),
                async token =>
                {
                    await this.auditLogAdapter.ListInstanceEventsAsync(
                        new[] { this.node.ProjectId },
                        new[] { this.node.InstanceId },
                        DateTime.UtcNow.Subtract(this.SelectedTimeframe.Duration),
                        accumulator,
                        token).ConfigureAwait(false);
                    return null;
            }).ConfigureAwait(true);  // Back to original (UI) thread.

            this.Events.AddRange(accumulator.Events);
        }

        //---------------------------------------------------------------------

        private class EventAccumulator : IEventProcessor
        {
            private readonly List<EventBase> events = new List<EventBase>();
            public IEnumerable<EventBase> Events => this.events;
            public EventOrder ExpectedOrder => EventOrder.NewestFirst;
            public IEnumerable<string> SupportedSeverities { get; }
            public IEnumerable<string> SupportedMethods { get; }

            public EventAccumulator(
                IEnumerable<string> severities,
                IEnumerable<string> methods)
            {
                this.SupportedSeverities = severities;
                this.SupportedMethods = methods;
            }

            public void Process(EventBase e)
            {
                this.events.Add(e);
            }
        }

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
