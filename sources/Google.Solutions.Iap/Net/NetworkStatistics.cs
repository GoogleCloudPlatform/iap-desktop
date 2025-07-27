//
// Copyright 2020 Google LLC
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

namespace Google.Solutions.Iap.Net
{
    /// <summary>
    /// Tracks statistics about a session.
    /// </summary>
    public class NetworkStatistics
    {
        private long bytesReceived = 0;
        private long bytesTransmitted = 0;


        public void OnReceiveCompleted(int bytesReceived)
        {
            Interlocked.Add(ref this.bytesReceived, bytesReceived);
        }

        public void OnTransmitCompleted(int bytesTransmitted)
        {
            Interlocked.Add(ref this.bytesTransmitted, bytesTransmitted);
        }

        /// <summary>
        /// Total number of bytes received.
        /// </summary>
        public ulong BytesReceived
        {
            get => (ulong)Interlocked.Read(ref this.bytesReceived);
        }

        /// <summary>
        /// Total number of bytes transmitted.
        /// </summary>
        public ulong BytesTransmitted
        {
            get => (ulong)Interlocked.Read(ref this.bytesTransmitted);
        }
    }
}
