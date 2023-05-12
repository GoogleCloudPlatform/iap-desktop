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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ObjectModel
{
    public interface IEventService
    {
        void BindAsyncHandler<TEvent>(Func<TEvent, Task> handler);
        void BindHandler<TEvent>(Action<TEvent> handler);


        /// <summary>
        /// Invoke all event handlers registered for the given event type.
        /// </summary>
        Task FireAsync<TEvent>(TEvent eventObject);
    }

    /// <summary>
    /// Allows events to be delivered to handlers in a multi-cast manner.
    /// 
    /// Invoker thread: Any
    /// Execution Thread: UI (ensured by EventService)
    /// Continuation thread: Same as invoker (ensured by EventService)
    /// </summary>
    public class EventService : IEventService
    {
        private static readonly Task CompletedTask = Task.FromResult(0);

        private readonly ISynchronizeInvoke invoker;
        private readonly IDictionary<Type, List<Func<object, Task>>> bindings = new Dictionary<Type, List<Func<object, Task>>>();

        public EventService(ISynchronizeInvoke invoker)
        {
            this.invoker = invoker;
        }

        public void BindAsyncHandler<TEvent>(Func<TEvent, Task> handler)
        {
            if (!this.bindings.ContainsKey(typeof(TEvent)))
            {
                this.bindings.Add(typeof(TEvent), new List<Func<object, Task>>());
            }

            this.bindings[typeof(TEvent)].Add(e => handler((TEvent)e));
        }

        public void BindHandler<TEvent>(Action<TEvent> handler)
        {
            BindAsyncHandler<TEvent>(e =>
            {
                handler(e);
                return CompletedTask;
            });
        }

        private async Task FireOnUiThread<TEvent>(TEvent eventObject)
        {
            Debug.Assert(!this.invoker.InvokeRequired, "FireOnUiThread must be called on UI thread");

            if (this.bindings.TryGetValue(typeof(TEvent), out var handlers))
            {
                // Run handlers.
                foreach (var handler in handlers)
                {
                    // Invoke handler, but make sure we return to the UI thread.
                    await handler(eventObject).ConfigureAwait(continueOnCapturedContext: true);
                }
            }
        }


        private void FireOnUiThread<TEvent>(TEvent eventObject, TaskCompletionSource<TEvent> completionSource)
        {
            var fireTask = FireOnUiThread(eventObject);
            fireTask.ContinueWith(t =>
                {
                    try
                    {
                        t.Wait();
                        completionSource.SetResult(eventObject);
                    }
                    catch (Exception e)
                    {
                        completionSource.SetException(e);
                    }
                });
        }


        /// <summary>
        /// Invoke all event handlers registered for the given event type.
        /// </summary>
        public Task FireAsync<TEvent>(TEvent eventObject)
        {
            if (this.invoker.InvokeRequired)
            {
                // We are running on some non-UI thread. Switch to UI
                // thread to handle events.

                var completionSource = new TaskCompletionSource<TEvent>();

                this.invoker.BeginInvoke(
                    (Action)(() => FireOnUiThread<TEvent>(eventObject, completionSource)), null);

                return completionSource.Task;
            }
            else
            {
                // We are on the UI thread, so we can simply dispatch the call
                // directly.
                return FireOnUiThread(eventObject);
            }
        }
    }
}
