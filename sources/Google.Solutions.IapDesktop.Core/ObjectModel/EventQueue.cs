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
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ObjectModel
{
    public class EventQueue : IEventQueue
    {
        private readonly ISynchronizeInvoke invoker;
        private readonly object subscriptionsLock;
        private readonly IDictionary<Type, List<ISubscription>> subscriptionsByEvent;

        /// <summary>
        /// Create an event queue.
        /// </summary>
        /// <param name="invoker">Invoker to use for publishing events</param>
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

        public ISubscription Subscribe<TEvent>(
            Func<TEvent, Task> handler,
            SubscriptionOptions lifecycle = SubscriptionOptions.None)
        {
            lock (this.subscriptionsLock)
            {
                if (!this.subscriptionsByEvent.TryGetValue(typeof(TEvent), out var subscriptions))
                {
                    subscriptions = new List<ISubscription>();
                    this.subscriptionsByEvent.Add(typeof(TEvent), subscriptions);
                }

                Subscription<TEvent> subsciption;
                if (lifecycle == SubscriptionOptions.WeakSubscriberReference)
                {
                    subsciption = new WeakSubscription<TEvent>(this, handler);
                }
                else
                {
                    subsciption = new StrongSubscription<TEvent>(this, handler);
                }

                subscriptions.Add(subsciption);
                return subsciption;
            }
        }

        public ISubscription Subscribe<TEvent>(
            Action<TEvent> handler,
            SubscriptionOptions lifecycle = SubscriptionOptions.None)
        {
            return Subscribe<TEvent>(e =>
            {
                handler(e);
                return Task.CompletedTask;
            },
            lifecycle);
        }

        public ISubscription Subscribe<TEvent>(
            IAsyncSubscriber<TEvent> subscriber,
            SubscriptionOptions lifecycle = SubscriptionOptions.None)
        {
            return Subscribe<TEvent>(subscriber.NotifyAsync, lifecycle);
        }

        public ISubscription Subscribe<TEvent>(
            ISubscriber<TEvent> subscriber,
            SubscriptionOptions lifecycle = SubscriptionOptions.None)
        {
            return Subscribe<TEvent>(subscriber.Notify, lifecycle);
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
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        /// <summary>
        /// Base class for subscriptions.
        /// </summary>
        internal abstract class Subscription<TEvent> : ISubscription
        {
            private bool disposed = false;
            private readonly EventQueue queue;

            protected Subscription(EventQueue queue)
            {
                this.queue = queue.ExpectNotNull(nameof(queue));
            }

            protected abstract Task InvokeCoreAsync(TEvent e);

            public Task InvokeAsync(TEvent e)
            {
                if (this.disposed)
                {
                    //
                    // Subscription is stale, ignore.
                    //
                    return Task.CompletedTask;
                }

                return InvokeCoreAsync(e);
            }

            public void Dispose()
            {
                if (!this.disposed)
                {
                    var removed = this.queue.Unsubscribe<TEvent>(this);
                    Debug.Assert(removed);

                    this.disposed = true;
                }
            }
        }

        /// <summary>
        /// Normal subscription that uses a strong reference to the subscriber.
        /// </summary>
        internal sealed class StrongSubscription<TEvent> : Subscription<TEvent>
        {
            private readonly Func<TEvent, Task> callback;

            public StrongSubscription(
                EventQueue queue,
                Func<TEvent, Task> callback)
                : base(queue)
            {
                this.callback = callback.ExpectNotNull(nameof(callback));
            }

            protected override Task InvokeCoreAsync(TEvent e)
            {
                return this.callback.Invoke(e);
            }
        }

        /// <summary>
        /// Weak subscription that avoids keeping the subscriber alive.
        /// </summary>
        internal sealed class WeakSubscription<TEvent> : Subscription<TEvent>
        {
            private readonly WeakReference<Func<TEvent, Task>> callback;

            public WeakSubscription(
                EventQueue queue, 
                Func<TEvent, Task> callback)
                : base(queue)
            {
                this.callback = new WeakReference<Func<TEvent, Task>>(
                    callback.ExpectNotNull(nameof(callback)));
            }

            protected override Task InvokeCoreAsync(TEvent e)
            {
                if (this.callback.TryGetTarget(out var target))
                {
                    return target.Invoke(e);
                }
                else
                {
                    //
                    // The subscriber is gone, remove this subscription
                    // so that the list of subscriber doesn't grow unbounded.
                    //
                    Dispose();
                    return Task.CompletedTask;
                }
            }

            /// <summary>
            /// For testing only: Simulate that the subscriber was GC'ed.
            /// </summary>
            internal void SimulateSubscriberWasGarbageCollected()
            {
                this.callback.SetTarget(null!);
            }
        }
    }
}
