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
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace Google.Solutions.CloudIap.Plugin.Integration
{
    /// <summary>
    /// Helper class to find unsed local TCP ports.
    /// </summary>
    internal static class PortFinder
    {
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

            // Ephemeral ports tend to start around 49000 (see 
            // https://support.microsoft.com/en-us/help/929851/the-default-dynamic-port-range-for-tcp-ip-has-changed-in-windows-vista)
            // Try to stay below

            for (var port = 10000; port < 49000; port++) {
                if (!occupiedPorts.Contains(port)) {
                    return port;
                }
            }

            throw new SystemException("No port available");
        }
    }
}
