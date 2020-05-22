﻿//
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

using Google.Solutions.Common.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Net
{
    /// <summary>
    /// Extension methods for INetworkStream.
    /// </summary>
    public static class NetworkStream
    {
        private const int MaxBufferSize = 64 * 1024;

        /// <summary>
        /// Relay all received by one stream to another stream.
        /// </summary>
        public static Task RelayToAsync(
            this INetworkStream readStream,
            INetworkStream writeStream,
            CancellationToken token)
        {
            return Task.Run(async () =>
            {
                // Use a buffer that is as large as possible, but does not exceed
                // any of the two stream's capabilities.
                int bufferSize = Math.Min(
                    writeStream.MaxWriteSize,
                    Math.Max(
                        MaxBufferSize,
                        readStream.MinReadSize));

                var buffer = new byte[bufferSize];

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        TraceSources.Compute.TraceVerbose(
                            "NetworkStream [{0} > {1}]: Reading...", 
                            readStream, 
                            writeStream);

                        int bytesRead = await readStream.ReadAsync(
                            buffer,
                            0,
                            buffer.Length,
                            token).ConfigureAwait(false);

                        if (bytesRead > 0)
                        {
                            TraceSources.Compute.TraceVerbose(
                                "NetworkStream [{0} > {1}]: Read {2} bytes",
                                readStream,
                                writeStream,
                                bytesRead);

                            await writeStream.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                        }
                        else
                        {
                            TraceSources.Compute.TraceVerbose(
                                "NetworkStream [{0} > {1}]: gracefully closed connection",
                                readStream,
                                writeStream);

                            // Propagate.
                            await writeStream.CloseAsync(token).ConfigureAwait(false);

                            break;
                        }
                    }
                    catch (NetworkStreamClosedException e)
                    {
                        TraceSources.Compute.TraceWarning(
                            "NetworkStream [{0} > {1}]: forcefully closed connection: {2}",
                            readStream,
                            writeStream,
                            e.Message);

                        // Propagate.
                        await writeStream.CloseAsync(token).ConfigureAwait(false);

                        break;
                    }
                    catch (Exception e)
                    {
                        TraceSources.Compute.TraceWarning(
                            "NetworkStream [{0} > {1}]: Caught unhandled exception: {2} {3}",
                            readStream,
                            writeStream,
                            e.Message,
                            e.StackTrace);

                        throw;
                    }
                }
            });
        }
    }
}
