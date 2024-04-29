//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Google.Solutions.Common.Threading
{
    /// <summary>
    /// Execution context that invokes callbacks on a single,
    /// designated thread.
    /// </summary>
    public class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private Thread? designatedThread;
        private readonly Queue<QueuedCallback> backlog = new Queue<QueuedCallback>();

        public SingleThreadSynchronizationContext()
        {
        }

        //---------------------------------------------------------------------
        // SynchronizationContext overrides.
        //---------------------------------------------------------------------

        public override SynchronizationContext CreateCopy()
        {
            throw new NotImplementedException(
                "Context does not support copying");
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            throw new NotImplementedException(
                "Context does not support synchronous execution");
        }

        public override void Post(SendOrPostCallback callback, object? state)
        {
            lock (this.backlog)
            {
                //
                // Enqueue callback and signal the thread.
                //
                this.backlog.Enqueue(new QueuedCallback(
                    ExecutionContext.Capture(),
                    callback,
                    state));
                Monitor.Pulse(this.backlog);
            }
        }

        /// <summary>
        /// Pump and run callbacks until cancelled.
        /// This method must be called on the designated thread.
        /// </summary>
        public void Pump(CancellationToken token)
        {
            if (this.designatedThread == null)
            {
                this.designatedThread = Thread.CurrentThread;
            }
            else if (Thread.CurrentThread.ManagedThreadId !=
                this.designatedThread.ManagedThreadId)
            {
                throw new InvalidOperationException(
                    "Only the designated thread can be used for pumping");
            }

            //
            // Interrupt wait up on cancellation.
            //
            token.Register(() =>
            {
                lock (this.backlog)
                {
                    Monitor.PulseAll(this.backlog);
                }
            });

            while (true)
            {
                QueuedCallback nextCallback;
                lock (this.backlog)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (!this.backlog.Any())
                    {
                        Monitor.Wait(this.backlog);
                    }

                    Debug.Assert(this.backlog.Any());

                    //
                    // We might have been interrupted because of cancellation.
                    //
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    //
                    // Get next callback and leave lock so that new callbacks
                    // can be enqueued while we're executing this one.
                    //
                    nextCallback = this.backlog.Dequeue();
                }

                try
                {
                    nextCallback.Invoke();
                }
                catch (Exception e)
                {
                    //
                    // Callbacks shouldn't throw exceptions, but it
                    // might happen anyway.
                    //
                    throw new TargetInvocationException(
                        "Target threw exception, stopping pump",
                        e);
                }
            }
        }

        //---------------------------------------------------------------------
        // Inner clases.
        //---------------------------------------------------------------------

        /// <summary>
        /// A queued callback that is scheduled to be executed within
        /// the synchronization context, on the single theread.
        /// </summary>
        private readonly struct QueuedCallback
        {
            private readonly ExecutionContext executionContext;
            private readonly SendOrPostCallback callback;
            private readonly object? state;

            public QueuedCallback(
                ExecutionContext executionContext,
                SendOrPostCallback callback,
                object? state)
            {
                this.executionContext = executionContext.ExpectNotNull(nameof(executionContext));
                this.callback = callback.ExpectNotNull(nameof(callback));
                this.state = state;
            }

            internal void Invoke()
            {
                //
                // Invoke callback on in the original
                // execution context.
                //
                var cb = this.callback;
                ExecutionContext.Run(
                    this.executionContext,
                    s => cb(s),
                    this.state);
            }
        }
    }
}
