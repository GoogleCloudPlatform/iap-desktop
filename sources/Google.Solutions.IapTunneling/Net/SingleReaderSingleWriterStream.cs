//
// Copyright 2019 Google LLC
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

using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Net
{
    /// <summary>
    /// Base class for a stream that only allows one reader and one writer
    /// at a time.
    /// </summary>
    public abstract class SingleReaderSingleWriterStream : OneTimeUseStream
    {
        private readonly SemaphoreSlim readerSemaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim writerSemaphore = new SemaphoreSlim(1);

        //---------------------------------------------------------------------
        // Methods to be overriden
        //---------------------------------------------------------------------

        protected abstract Task<int> ProtectedReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken);

        protected abstract Task ProtectedWriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken);

        public abstract Task ProtectedCloseAsync(CancellationToken cancellationToken);


        //---------------------------------------------------------------------
        // Publics
        //---------------------------------------------------------------------

        protected override async Task<int> ReadAsyncWithCloseProtection(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            try
            {
                //
                // Acquire semaphore to ensure that only a single
                // read operation is in flight at a time.
                //
                await this.readerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                return await ProtectedReadAsync(
                    buffer,
                    offset,
                    count,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                this.readerSemaphore.Release();
            }
        }

        protected override async Task WriteAsyncWithCloseProtection(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                //
                // Acquire semaphore to ensure that only a single
                // write/close operation is in flight at a time.
                //
                await this.writerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                await ProtectedWriteAsync(
                    buffer,
                    offset,
                    count,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                this.writerSemaphore.Release();
            }
        }

        protected override async Task CloseAsyncWithCloseProtection(CancellationToken cancellationToken)
        {
            try
            {
                //
                // Acquire semaphore to ensure that only a single
                // write/close operation is in flight at a time.
                //
                await this.writerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                await ProtectedCloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                this.writerSemaphore.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.readerSemaphore.Dispose();
                this.writerSemaphore.Dispose();
            }
        }
    }
}
