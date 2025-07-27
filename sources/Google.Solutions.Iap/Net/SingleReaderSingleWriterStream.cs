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

using Google.Solutions.Common.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Net
{
    /// <summary>
    /// Base class for a stream that only allows one reader and one writer
    /// at a time.
    /// </summary>
    public abstract class SingleReaderSingleWriterStream : INetworkStream
    {
        private readonly SemaphoreSlim readerSemaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim writerSemaphore = new SemaphoreSlim(1);

        private volatile bool closed;
        private readonly CancellationTokenSource forceCloseSource 
            = new CancellationTokenSource();

#if DEBUG
        //
        // Assign an ID to aid debugging.
        //
        private static int lastStreamId = 0;
        private int streamId;
#endif

        protected SingleReaderSingleWriterStream()
        {
#if DEBUG
            this.streamId = Interlocked.Increment(ref lastStreamId);
#endif
        }

        private void VerifyStreamNotClosed()
        {
            if (this.closed)
            {
                throw new NetworkStreamClosedException("Stream is closed");
            }
        }

        //---------------------------------------------------------------------
        // Abstract methods
        //---------------------------------------------------------------------

        /// <summary>
        /// Read a sequence of bytes.
        /// </summary>
        /// <remarks>
        /// The method is called with a semaphore held, ensuring that only
        /// one read-operation can be in progress at a time.
        /// </remarks>
        protected abstract Task<int> ReadCoreAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken);

        /// <summary>
        /// Write a sequence of bytes.
        /// </summary>
        /// <remarks>
        /// The method is called with a semaphore held, ensuring that only
        /// one write-operation can be in progress at a time.
        /// </remarks>
        protected abstract Task WriteCoreAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken);

        /// <summary>
        /// Close the stream. The method is called with semaphores held,
        /// ensuring that no further read- or write can be in progress.
        /// </summary>
        public abstract Task CloseCoreAsync(
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // INetworkStream.
        //---------------------------------------------------------------------

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current stream and 
        /// advances the position within the stream by the number of bytes read.
        /// </summary>
        public async Task<int> ReadAsync(
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
                await this.readerSemaphore
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                //
                // Check flag while we own the semaphore.
                //
                VerifyStreamNotClosed();

                //
                // Cancel operation when the caller requests it or
                // when the stream is closed.
                //
                using (var cancellationSource =
                    cancellationToken.Combine(this.forceCloseSource.Token))
                {
                    var bytesRead = await
                        ReadCoreAsync(
                            buffer,
                            offset,
                            count,
                            cancellationSource.Token)
                        .ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        this.closed = true;
                    }

                    return bytesRead;
                }
            }
            catch (NetworkStreamClosedException)
            {
                this.closed = true;
                throw;
            }
            finally
            {
                this.readerSemaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronously writes a sequence of bytes to the current stream and 
        /// advances the current position within this stream by the number of 
        /// bytes written.
        /// </summary>
        public async Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            try
            {
                //
                // Acquire semaphore to ensure that only a single
                // write/close operation is in flight at a time.
                //
                await this.writerSemaphore
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                //
                // Check flag while we own the semaphore.
                //
                VerifyStreamNotClosed();

                //
                // Cancel operation when the caller requests it or
                // when the stream is closed.
                //
                using (var cancellationSource =
                    cancellationToken.Combine(this.forceCloseSource.Token))
                {
                    await WriteCoreAsync(
                        buffer,
                        offset,
                        count,
                        cancellationSource.Token)
                    .ConfigureAwait(false);
                }
            }
            catch (NetworkStreamClosedException)
            {
                this.closed = true;
                throw;
            }
            finally
            {
                this.writerSemaphore.Release();
            }
        }

        /// <summary>
        /// Cancel any outstanding read- or write operations and
        /// close the stream.
        /// </summary>
        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                //
                // Cancel pending operations.
                //
                this.forceCloseSource.Cancel();

                //
                // Acquire semaphore to ensure that only a single
                // write/close operation is in flight at a time.
                //
                await this.readerSemaphore
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
                await this.writerSemaphore
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                await CloseCoreAsync(cancellationToken)
                    .ConfigureAwait(false);

                this.closed = true;
            }
            finally
            {
                this.readerSemaphore.Release();
                this.writerSemaphore.Release();
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.readerSemaphore.Dispose();
                this.writerSemaphore.Dispose();
                this.forceCloseSource.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
