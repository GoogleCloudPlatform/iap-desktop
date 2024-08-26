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

using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events;
using Google.Solutions.IapDesktop.Extensions.Management.History;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.EventLog
{
    public class EventLogModel : IEventProcessor
    {
        private readonly List<EventBase> events = new List<EventBase>();
        public IEnumerable<EventBase> Events => this.events;
        public EventOrder ExpectedOrder => EventOrder.NewestFirst;

        public IEnumerable<string>? SupportedSeverities => null; // All
        public IEnumerable<string> SupportedMethods =>
            EventFactory.LifecycleEventMethods
                .Concat(EventFactory.SystemEventMethods)
                .Concat(EventFactory.AccessEventMethods);

        public string DisplayName { get; }

        public EventLogModel(string displayName)
        {
            this.DisplayName = displayName;
        }

        public void Process(EventBase e)
        {
            this.events.Add(e);
        }
    }
}
