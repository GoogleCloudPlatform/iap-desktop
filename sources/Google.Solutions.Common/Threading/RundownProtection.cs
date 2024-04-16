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

using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Threading
{
    /// <summary>
    /// Protect a resource from being cleaned up while it's in use.
    /// </summary>
    public sealed class RundownProtection : IDisposable
    {
        private int acquisitions = 0;
        private readonly ManualResetEvent rundownEvent = new ManualResetEvent(false);

        public IDisposable Acquire()
        {
            this.rundownEvent.Reset();
            Interlocked.Increment(ref this.acquisitions);

            return Disposable.For(
                () =>
                {
                    if (Interlocked.Decrement(ref this.acquisitions) == 0)
                    {
                        this.rundownEvent.Set();
                    }
                });
        }

        /// <summary>
        /// Await safe rundown.
        /// </summary>
        /// <returns></returns>
        public Task WaitAsync()
        {
            return this.rundownEvent.WaitAsync(CancellationToken.None);
        }

        public void Dispose()
        {
            this.rundownEvent.Dispose();
        }
    }
}
