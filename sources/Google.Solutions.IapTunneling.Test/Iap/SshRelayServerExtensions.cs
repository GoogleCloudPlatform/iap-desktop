//
// Copyright 2022 Google LLC
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

using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Test.Net;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    internal static class SshRelayServerExtensions
    {
        public static async Task SendConnectSuccessSidAsync(
            this ServerWebSocketConnection server, 
            string sid)
        {
            var buffer = new byte[64];
            var bytes = SshRelayFormat.ConnectSuccessSid.Encode(buffer, sid);

            await server
                .SendBinaryFrameAsync(buffer, 0, (int)bytes)
                .ConfigureAwait(false);
        }

        public static async Task SendDataAsync(
            this ServerWebSocketConnection server,
            byte[] data)
        {
            var buffer = new byte[data.Length + 6];
            var bytes = SshRelayFormat.Data.Encode(buffer, data, 0, (uint)data.Length);

            await server
                .SendBinaryFrameAsync(buffer, 0, (int)bytes)
                .ConfigureAwait(false);
        }
    }
}
