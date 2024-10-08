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

using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ObjectModel
{
    /// <summary>
    /// Publishes events to subscribers.
    /// 
    /// There are multiple differences to regular C#/CLR events:
    /// 
    /// * Events can be emitted from any thread, but subscribers
    ///   are guaranteed to be invoked on a specific thread, 
    ///   effectively resulting in queueing semantics.
    /// * Event handlers can be asynchronous.
    /// * Subscribing to an event doesn't keep the source alive.
    /// 
    /// </summary>
    public interface IEventQueue
    {
        /// <summary>
        /// Subscribe to an event using an asynchronous handler
        /// until the subscription is disposed.
        /// </summary>
        ISubscription Subscribe<TEvent>(Func<TEvent, Task> handler);

        /// <summary>
        /// Subscribe to an event using an synchronous handler
        /// until the subscription is disposed.
        /// </summary>
        ISubscription Subscribe<TEvent>(Action<TEvent> handler);

        /// <summary>
        /// Publish an event and wait for all subscribers to handle the event.
        /// </summary>
        Task PublishAsync<TEvent>(TEvent eventObject);

        /// <summary>
        /// Publish an event without awaiting subcribers.
        /// </summary>
        void Publish<TEvent>(TEvent eventObject);
    }

    public interface ISubscription : IDisposable
    { }
}
