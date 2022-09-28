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

namespace Google.Solutions.IapTunneling.Net
{
    /// <summary>
    /// Interface representing a network stream.
    /// </summary>
    public interface INetworkStream : IDisposable
    {
        /// <summary>
        /// Read from stream.
        /// </summary>
        /// <returns>Bytes read, 0 if connection closed cleanly</returns>
        /// <exception cref="NetworkStreamClosedException">if stream is closed already</exception>
        Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken);

        /// <summary>
        /// Write to stream. Any data is immediately flushed.
        /// </summary>
        /// <exception cref="NetworkStreamClosedException">if stream is closed already</exception>
        Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken);

        /// <summary>
        /// Close stream.
        /// </summary>
        /// <exception cref="NetworkStreamClosedException">if stream is closed already</exception>
        Task CloseAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Base exception for scenarios where a client tries to use a stream
    /// that has alrady been closed, either by the client itself or by
    /// the server.
    /// </summary>
    public class NetworkStreamClosedException : Exception
    {
        protected NetworkStreamClosedException()
        {
        }

        public NetworkStreamClosedException(string message) : base(message)
        {
        }

        public NetworkStreamClosedException(string message, Exception e) : base(message, e)
        {
        }
    }
}
