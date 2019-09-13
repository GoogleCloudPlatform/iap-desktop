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

namespace Google.Solutions.Compute.Net
{
    /// <summary>
    /// Adapter that connects two different NetworkStreams with incompatible
    /// min/max buffer size demands.
    /// </summary>
    public class FragmentingStream : SingleReaderSingleWriterStream
    {
        private readonly INetworkStream stream;

        private byte[] readBuffer = null;
        private int readBufferOffset = 0;
        private int readBufferCount = 0;

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        public FragmentingStream(INetworkStream stream)
        {
            this.stream = stream;
        }

        //---------------------------------------------------------------------
        // SingleReaderSingleWriterStream overrides
        //---------------------------------------------------------------------

        public override int MaxWriteSize => int.MaxValue;
        public override int MinReadSize => this.stream.MinReadSize;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.stream.Dispose();
            }
        }

        public override Task ProtectedCloseAsync(CancellationToken cancellationToken)
        {
            return this.stream.CloseAsync(cancellationToken);
        }

        protected override async Task<int> ProtectedReadAsync(
            byte[] buffer, 
            int offset, 
            int count, 
            CancellationToken cancellationToken)
        {
            while (true)
            { 
                var bytesLeftInBuffer = this.readBufferCount - this.readBufferOffset;
                if (bytesLeftInBuffer > 0)
                {
                    // There is buffered data left.
                    var bytesToRead = Math.Min(count, bytesLeftInBuffer);
                    Array.Copy(
                        this.readBuffer,
                        this.readBufferOffset,
                        buffer,
                        offset,
                        bytesToRead);

                    this.readBufferOffset += bytesToRead;
                    return bytesToRead;
                }
                else if (count >= this.stream.MinReadSize)
                {
                    // No data in buffer and supplied buffer is large enough.
                    return await this.stream.ReadAsync(
                        buffer,
                        offset,
                        count,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // No data in buffer and supplied buffer is too small.
                    if (this.readBuffer == null)
                    {
                        // Use a buffer that is large enough, but use a safety
                        // upper bound of 1 MB.
                        this.readBuffer = new byte[Math.Min(this.stream.MinReadSize, 1024 * 1024)];
                    }

                    this.readBufferOffset = 0;
                    this.readBufferCount = await this.stream.ReadAsync(
                        this.readBuffer,
                        this.readBufferOffset,
                        this.readBuffer.Length,
                        cancellationToken).ConfigureAwait(false);

                    if (this.readBufferCount == 0)
                    {
                        // Stream closed..
                        return 0;
                    }
                }
            }
        }

        protected override async Task ProtectedWriteAsync(
            byte[] buffer, 
            int offset, 
            int count, 
            CancellationToken cancellationToken)
        {
            var windowSize = this.stream.MaxWriteSize;
            for (int windowOffset = 0; 
                 windowOffset < buffer.Length; 
                 windowOffset += windowSize)
            {
                await this.stream.WriteAsync(
                    buffer,
                    offset + windowOffset,
                    Math.Min(windowSize, count - windowOffset),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
