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
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Callback that is invoked after a delay has elapsed.
    /// If invoked multiple times during the delay, the callback
    /// is only executed once.
    /// </summary>
    public sealed class DeferredCallback : IDisposable
    {
        private readonly Timer timer;

        public DeferredCallback(
            Action<DeferredCallback> callback,
            TimeSpan delay)
        {
            this.timer = new Timer()
            {
                Interval = delay.Milliseconds
            };

            this.timer.Tick += (_, __) =>
            {
                this.timer.Enabled = false;
                callback(this);
            };
        }

        public void Invoke()
        {
            //
            // Reset the timer.
            //
            this.timer.Stop();
            this.timer.Start();
        }

        public void Dispose()
        {
            this.timer.Dispose();
        }

        public bool IsCallbackPending
        {
            get => this.timer.Enabled;
        }
    }
}
