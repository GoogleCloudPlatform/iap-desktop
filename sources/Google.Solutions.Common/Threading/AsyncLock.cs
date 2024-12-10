//
// Copyright 2021 Google LLC
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

using Google.Solutions.Common.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Threading
{
    /// <summary>
    /// Async lock, based on a <c>SemaphoreSlim</c>.
    /// </summary>
    public sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        /// Acquire the lock asynchronously.
        /// </summary>
        /// <returns>Acquisition, dispose to release the lock.</returns>
        public async Task<IDisposable> AcquireAsync(CancellationToken token)
        {
            await this.semaphore
                .WaitAsync(token)
                .ConfigureAwait(false);

            return Disposable.Create(() => this.semaphore.Release());
        }

        /// <summary>
        /// Acquire the lock synchronously.
        /// </summary>
        /// <returns>Acquisition, dispose to release the lock.</returns>
        public IDisposable Acquire()
        {
            this.semaphore.Wait();

            return Disposable.Create(() => this.semaphore.Release());
        }

        //----------------------------------------------------------------------
        // IDisposable.
        //----------------------------------------------------------------------

        public void Dispose()
        {
            this.semaphore.Dispose();
        }
    }
}
