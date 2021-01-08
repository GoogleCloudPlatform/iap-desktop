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

using Google.Solutions.IapTunneling.Iap;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Tunnel
{
    internal class SameProcessRelayPolicy : ISshRelayPolicy
    {
        public bool IsClientAllowed(IPEndPoint remote)
        {
            //
            // NB. For connections from localhost, there are two
            // entries in the table - one tracking the outgoing
            // connection and one tracking the incoming connection.
            //
            // To find out the client process id, we need to find
            // the entry for the outgoing connection.
            //
            var tcpTableEntry = TcpTable.GetTcpTable2()
                .Where(e => e.LocalEndpoint.Equals(remote));

            //
            // Only permit access if the originating process is
            // the current process.
            //
            return remote.Address.Equals(IPAddress.Loopback) &&
                tcpTableEntry.Any() &&
                tcpTableEntry.First().ProcessId == Process.GetCurrentProcess().Id;
        }
    }
}
