//
// Copyright 2024 Google LLC
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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Callback that is invoked after a delay has elapsed.
    /// If invoked multiple times during the delay, the callback
    /// is only executed once.
    /// 
    /// Note that this class is not thread-safe.
    /// </summary>
    public sealed class DeferredCallback : IDisposable
    {
        private readonly Timer timer;
        private TaskCompletionSource<object?> nextCompletion;

        public DeferredCallback(
            Action<IDeferredCallbackContext> callback,
            TimeSpan delay)
        {
            this.timer = new Timer()
            {
                Interval = (int)delay.TotalMilliseconds
            };

            this.nextCompletion = new TaskCompletionSource<object?>();

            this.timer.Tick += (_, __) =>
            {
                this.timer.Enabled = false;

                var context = new CallbackContext();
                callback(context);

                if (context.IsDeferralRequested)
                {
                    //
                    // Schedule again.
                    //
                    Schedule();
                }
                else
                {
                    //
                    // Done, notify waiters.
                    //
                    this.nextCompletion.SetResult(null);
                    this.nextCompletion = new TaskCompletionSource<object?>();
                }
            };

        }

        /// <summary>
        /// Schedule a callback. When a callback has been scheduled
        /// already, the callbacks are coalesced.
        /// </summary>
        public void Schedule()
        {
            //
            // Reset the timer.
            //
            this.timer.Stop();
            this.timer.Start();
        }

        public bool IsPending
        {
            get => this.timer.Enabled;
        }

        /// <summary>
        /// Wait for deferred callback to complete. For testing.
        /// </summary>
        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks")]
        public Task WaitForCompletionAsync()
        {
            if (this.IsPending)
            {
                return this.nextCompletion.Task;
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.timer.Dispose();
        }

        //---------------------------------------------------------------------
        // IDeferredCallbackContext.
        //---------------------------------------------------------------------

        private class CallbackContext : IDeferredCallbackContext
        {
            internal bool IsDeferralRequested { get; private set; } = false;

            public void Defer()
            {
                this.IsDeferralRequested = true;
            }
        }
    }

    public interface IDeferredCallbackContext
    {
        /// <summary>
        /// Defer the callback to a later point.
        /// </summary>
        void Defer();
    }
}
