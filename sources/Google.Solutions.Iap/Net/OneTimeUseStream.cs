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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Net
{
    /// <summary>
    ///  Base class protecting against usage after
    ///  the stream has been closed or has failed.
    /// </summary>
    public abstract class OneTimeUseStream : INetworkStream
    {
        private volatile bool closed;

        private void VerifyStreamNotClosed()
        {
            if (this.closed)
            {
                throw new NetworkStreamClosedException("Stream is closed");
            }
        }

        //---------------------------------------------------------------------
        // Methods to be overridden
        //---------------------------------------------------------------------

        protected abstract Task CloseAsyncWithCloseProtection(CancellationToken cancellationToken);

        protected abstract Task<int> ReadAsyncWithCloseProtection(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken);

        protected abstract Task WriteAsyncWithCloseProtection(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Publics
        //---------------------------------------------------------------------

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            //
            // Calling CloseAsync on a closed connection is acceptable
            // since the server might have closed first.
            //

            await CloseAsyncWithCloseProtection(cancellationToken).ConfigureAwait(false);
            this.closed = true;
        }

        public async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            VerifyStreamNotClosed();

            try
            {
                int bytesRead = await ReadAsyncWithCloseProtection(
                    buffer,
                    offset,
                    count,
                    cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    this.closed = true;
                }

                return bytesRead;
            }
            catch (NetworkStreamClosedException)
            {
                this.closed = true;
                throw;
            }
        }

        public async Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            VerifyStreamNotClosed();

            try
            {
                await WriteAsyncWithCloseProtection(
                    buffer,
                    offset,
                    count,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (NetworkStreamClosedException)
            {
                this.closed = true;
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
