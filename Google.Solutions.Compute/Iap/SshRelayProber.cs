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

namespace Google.Solutions.Compute.Iap
{
    public class SshRelayProber
    {
        private readonly ISshRelayEndpoint endpoint;

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        public SshRelayProber(ISshRelayEndpoint endpoint)
        {
            this.endpoint = endpoint;
        }

        //---------------------------------------------------------------------
        // Publics
        //---------------------------------------------------------------------

        public async Task ProbeConnectionAsync(TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            using (var stream = new SshRelayStream(this.endpoint))
            {
                cts.CancelAfter(timeout);

                try
                {
                    // Perform a read without sending any request. If the 
                    // connection is good, the read will not return anything.
                    // If anything goes wrong, the read will fail.
                    var buffer = new byte[stream.MinReadSize];
                    await stream.ReadAsync(
                        buffer,
                        0,
                        buffer.Length,
                        cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // No response within allotted time, depending on the
                    // protocol, that might just be normal.
                    // The connection was not aborted immediately, so we 
                    // count that as a success.
                }
            }
        }
    }
}
