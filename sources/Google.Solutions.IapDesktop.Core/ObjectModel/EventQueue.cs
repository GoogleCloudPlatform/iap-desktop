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
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ObjectModel
{
    public class EventQueue : IEventQueue
    {
        private readonly ISynchronizeInvoke invoker;
        private readonly object subscriptionsLock;
        private readonly IDictionary<Type, List<ISubscription>> subscriptionsByEvent;

        public EventQueue(ISynchronizeInvoke invoker)
        {
            this.invoker = invoker.ExpectNotNull(nameof(invoker));

            this.subscriptionsLock = new object();
            this.subscriptionsByEvent = new Dictionary<Type, List<ISubscription>>();
        }

        private bool Unsubscribe<TEvent>(ISubscription subscription)
        {
            lock (this.subscriptionsLock)
            {
                if (this.subscriptionsByEvent.TryGetValue(typeof(TEvent), out var subscribers))
                {
                    return subscribers.Remove(subscription);
                }

                return false;
            }
        }

        internal IEnumerable<Subscription<TEvent>> GetSubscriptions<TEvent>()
        {
            lock (this.subscriptionsLock)
            {
                if (this.subscriptionsByEvent.TryGetValue(typeof(TEvent), out var subscriptions))
                {
                    //
                    // Create a snapshot that remains valid when
                    // we leave the lock.
                    //
                    return new List<Subscription<TEvent>>(
                        subscriptions.OfType<Subscription<TEvent>>());
                }
                else
                {
                    return Enumerable.Empty<Subscription<TEvent>>();
                }
            }
        }

        //---------------------------------------------------------------------
        // IEventQueue.
        //---------------------------------------------------------------------

        public ISubscription Subscribe<TEvent>(Func<TEvent, Task> handler)
        {
            lock (this.subscriptionsLock)
            {
                if (!this.subscriptionsByEvent.TryGetValue(typeof(TEvent), out var subscriptions))
                {
                    subscriptions = new List<ISubscription>();
                    this.subscriptionsByEvent.Add(typeof(TEvent), subscriptions);
                }

                var subsciption = new Subscription<TEvent>(this, handler);
                subscriptions.Add(subsciption);

                return subsciption;
            }
        }

        public ISubscription Subscribe<TEvent>(Action<TEvent> handler)
        {
            return Subscribe<TEvent>(e =>
            {
                handler(e);
                return Task.CompletedTask;
            });
        }

        public Task PublishAsync<TEvent>(TEvent eventObject)
        {
            return this.invoker.InvokeAsync(async () =>
                {
                    //
                    // We're on the right thread now. Grab a snapshot of relevant
                    // subcriptions and invoke them.
                    //
                    // NB. It's possible that while we're doing that, a subscription
                    // is disposed. Bu the Subscription.Invoke implementation takes
                    // care of that.
                    //

                    foreach (var subscriber in GetSubscriptions<TEvent>())
                    {
                        await subscriber
                            .InvokeAsync(eventObject)
                            .ConfigureAwait(true); // Stay on thread!
                    }
                });
        }

        /// <summary>
        /// Publish an event without awaiting subcribers.
        /// </summary>
        public void Publish<TEvent>(TEvent eventObject)
        {
            _ = PublishAsync(eventObject)
                .ContinueWith(
                    t =>
                    {
                        Debug.Assert(
                            Assembly.GetEntryAssembly() == null, // Don't assert in unit tests
                            "One or more subscribers failed to handle an event: " + t.Exception);
                        CoreTraceSource.Log.TraceError(t.Exception);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        internal sealed class Subscription<TEvent> : ISubscription
        {
            private bool unsubscribed = false;
            private readonly EventQueue queue;
            private readonly Func<TEvent, Task> callback;

            public Subscription(
                EventQueue queue,
                Func<TEvent, Task> callback)
            {
                this.queue = queue.ExpectNotNull(nameof(queue));
                this.callback = callback.ExpectNotNull(nameof(callback));
            }

            public Task InvokeAsync(TEvent e)
            {
                if (this.unsubscribed)
                {
                    //
                    // Subscription is stale, ignore.
                    //
                    return Task.CompletedTask;
                }

                return this.callback.Invoke(e);
            }

            public void Dispose()
            {
                var removed = this.queue.Unsubscribe<TEvent>(this);
                Debug.Assert(removed);

                this.unsubscribed = true;
            }
        }
    }
}
