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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Net
{
    public sealed class BufferedNetworkStream : INetworkStream
    {
        private readonly INetworkStream stream;

        public BufferedNetworkStream(INetworkStream stream)
        {
            this.stream = stream;
        }

        public int MaxWriteSize => this.stream.MaxWriteSize;

        public int MinReadSize => this.stream.MinReadSize;

        public Task CloseAsync(CancellationToken cancellationToken)
            => this.stream.CloseAsync(cancellationToken);

        public void Dispose()
            => this.stream.Dispose();

        public Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
            => this.stream.WriteAsync(buffer, offset, count, cancellationToken);

        public async Task<int> ReadAsync(
            byte[] buffer, 
            int offset, 
            int count, 
            CancellationToken cancellationToken)
        {
            int totalBytesRead = 0;

            //
            // Keep reading until we have the requested amount of data.
            //
            while (totalBytesRead < count)
            {
                var bytesRead = await this.stream.ReadAsync(
                        buffer,
                        offset + totalBytesRead,
                        count - totalBytesRead,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }
                else
                {
                    totalBytesRead += bytesRead;
                }
            }

            return totalBytesRead;
        }
    }
}
