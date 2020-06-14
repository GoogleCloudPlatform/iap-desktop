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
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog
{
    internal class EventLogViewModel : ViewModelBase, IEventProcessor
    {
        public static readonly ReadOnlyCollection<Timeframe> AnalysisTimeframes 
            = new ReadOnlyCollection<Timeframe>(new List<Timeframe>()
        {
            new Timeframe(TimeSpan.FromDays(7), "Last 7 days"),
            new Timeframe(TimeSpan.FromDays(30), "Last 30 days"),
            new Timeframe(TimeSpan.FromDays(90), "Last 90 days")
        });

        private readonly IProjectExplorerVmInstanceNode node;

        private Timeframe selectedTimeframe = null;
        private bool includeSystemEvents = true;
        private bool includeLifecycleEvents = true;

        public EventLogViewModel(IProjectExplorerVmInstanceNode node)
        {
            this.node = node;
            this.Events = new ObservableCollection<EventBase>();
        }

        public bool IsRefreshButtonEnabled => true;
        public bool IsLifecycleEventDropDownEnabled => true;

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableCollection<EventBase> Events { get; }

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

        public EventOrder ExpectedOrder => EventOrder.NewestFirst;

        public IEnumerable<string> SupportedSeverities => null; // All.

        public IEnumerable<string> SupportedMethods
        {
            get
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

                return methods;
            }
        }

        public void Process(EventBase e)
        {
            this.Events.Add(e);
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public static async Task<EventLogViewModel> LoadAsync(
            AuditLogAdapter auditLogAdapter,
            IProjectExplorerNode node,
            CancellationToken token)
        {
            if (node is IProjectExplorerVmInstanceNode vmNode)
            {
                await Task.Delay(2000, token).ConfigureAwait(false);
                return new EventLogViewModel(vmNode);
            }
            else
            {
                // We do not care about any other nodes.
                return null;
            }
        }

        public Task RefreshAsync()
        {
            throw new NotImplementedException();
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
