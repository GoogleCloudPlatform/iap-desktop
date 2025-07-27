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

using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Test.Protocol
{
    internal class MockStream : INetworkStream
    {
        public int WriteCount { get; private set; } = 0;
        public int ReadCount { get; private set; } = 0;
        public int CloseCount { get; private set; } = 0;

        public byte[][]? ExpectedReadData { get; set; }
        public byte[][]? ExpectedWriteData { get; set; }
        public WebSocketCloseStatus? ExpectServerCloseCodeOnRead { get; set; }
        public WebSocketCloseStatus? ExpectServerCloseCodeOnWrite { get; set; }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            this.CloseCount++;
            return Task.Delay(0, CancellationToken.None);
        }

        public Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (this.CloseCount > 0)
            {
                throw new WebSocketStreamClosedByClientException();
            }
            else if (this.ExpectedReadData != null &&
                this.ExpectedReadData.Length > this.ReadCount &&
                this.ExpectedReadData[this.ReadCount] != null)
            {
                if (count < this.ExpectedReadData[this.ReadCount].Length)
                {
                    throw new OverflowException();
                }

                Array.Copy(
                    this.ExpectedReadData[this.ReadCount],
                    buffer,
                    this.ExpectedReadData[this.ReadCount].Length);
                var result = Task.FromResult(this.ExpectedReadData[this.ReadCount].Length);

                this.ReadCount++;

                return result;
            }
            else if (this.ExpectServerCloseCodeOnRead.HasValue)
            {
                throw new WebSocketStreamClosedByServerException(
                    this.ExpectServerCloseCodeOnRead.Value,
                    "Mock");
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (this.CloseCount > 0)
            {
                throw new WebSocketStreamClosedByClientException();
            }
            else if (this.ExpectedWriteData != null &&
                this.ExpectedWriteData.Length > this.WriteCount &&
                this.ExpectedWriteData[this.WriteCount] != null)
            {
                var data = new byte[count];
                Array.Copy(buffer, offset, data, 0, count);
                Assert.AreEqual(this.ExpectedWriteData[this.WriteCount], data);

                this.WriteCount++;
                return Task.Delay(0, CancellationToken.None);
            }
            else if (this.ExpectServerCloseCodeOnWrite.HasValue)
            {
                throw new WebSocketStreamClosedByServerException(
                    this.ExpectServerCloseCodeOnWrite.Value,
                    "Mock");
            }
            else
            {
                this.WriteCount++;
                return Task.Delay(0, CancellationToken.None);
            }
        }

        public void Dispose()
        {
        }
    }

    internal class MockSshRelayEndpoint : ISshRelayTarget
    {
        public IList<INetworkStream> ExpectedStreams { get; set; } 
            = new List<INetworkStream>();

        public INetworkStream ExpectedStream
        {
            get => this.ExpectedStreams[0];
            set => this.ExpectedStreams = new INetworkStream[] { value };
        }

        public int ConnectCount { get; private set; } = 0;
        public int ReconnectCount { get; private set; } = 0;

        public bool IsMutualTlsEnabled => false;

        public Task<INetworkStream> ConnectAsync(CancellationToken token)
        {
            var result = Task.FromResult(
                this.ExpectedStreams[this.ConnectCount + this.ReconnectCount]);
            this.ConnectCount++;
            return result;
        }

        public Task<INetworkStream> ReconnectAsync(
            string sid,
            ulong lastByteConsumedByClient,
            CancellationToken token)
        {
            var result = Task.FromResult(
                this.ExpectedStreams[this.ConnectCount + this.ReconnectCount]);
            this.ReconnectCount++;
            return result;
        }
    }
}
