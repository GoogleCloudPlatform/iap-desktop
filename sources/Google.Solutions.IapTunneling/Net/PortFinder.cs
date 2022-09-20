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

using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace Google.Solutions.IapTunneling.Net
{
    /// <summary>
    /// Helper class to find unsed local TCP ports.
    /// </summary>
    public static class PortFinder
    {
        private const int MaxAttempts = 1000;
        private const int PortRangeStart = 10000;
        private const int PortRangeEnd = 49000;

        public static HashSet<int> QueryOccupiedPorts()
        {
            var occupiedServerPorts = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Select(l => l.Port).ToHashSet();

            var occupiedClientPorts = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .Select(c => c.LocalEndPoint.Port).ToHashSet();

            var allOccupiedPorts = new HashSet<int>(occupiedClientPorts);
            allOccupiedPorts.UnionWith(occupiedServerPorts);
            return allOccupiedPorts;
        }

        public static int FindFreeLocalPort()
        {
            var occupiedPorts = QueryOccupiedPorts();

            //
            // Ephemeral ports tend to start around 49000 (see 
            // https://support.microsoft.com/en-us/help/929851/the-default-dynamic-port-range-for-tcp-ip-has-changed-in-windows-vista)
            // Try to stay below
            //

            //
            // Use a random port to make port numbers less predictable.
            //
            var random = new Random(Environment.TickCount);

            //
            // Make a reasonable number of attempts without risking getting
            // stuck in an infinite loop.
            //
            for (int attempts = 0; attempts < MaxAttempts; attempts++)
            {
                var port = random.Next(PortRangeStart, PortRangeEnd);
                if (!occupiedPorts.Contains(port))
                {
                    return port;
                }
            }

            throw new SystemException("Failed to find available TCP port");
        }
    }
}
