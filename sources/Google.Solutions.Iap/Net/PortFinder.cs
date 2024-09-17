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

using Google.Solutions.Common.Format;
using Google.Solutions.Common.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;

namespace Google.Solutions.Iap.Net
{
    /// <summary>
    /// Helper class to find unsed local TCP ports.
    /// </summary>
    public class PortFinder
    {
        private const int MaxAttempts = 1000;

        //
        // Try to stay below the ephemeral port range, which
        // starts around 49000.
        //
        // Cf. https://support.microsoft.com/en-us/help/929851.
        //
        private const ushort PortRangeStart = 10000;
        private const ushort PortRangeEnd = 49000;

        /// <summary>
        /// Size of the available port range, in bits.
        /// </summary>
        private const ushort PortRangeSize = 15;

        //
        // Seed for determining a preferred port.
        //
        private readonly BsdChecksum seed = new BsdChecksum(PortRangeSize);

        static PortFinder()
        {
            Debug.Assert(PortRangeStart + (1 << (PortRangeSize - 1)) < PortRangeEnd);
        }

        private static HashSet<ushort> QueryOccupiedPorts()
        {
            var occupiedServerPorts = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Select(l => (ushort)l.Port)
                .ToHashSet();

            var occupiedClientPorts = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .Select(c => (ushort)c.LocalEndPoint.Port)
                .ToHashSet();

            var allOccupiedPorts = new HashSet<ushort>(occupiedClientPorts);
            allOccupiedPorts.UnionWith(occupiedServerPorts);
            return allOccupiedPorts;
        }

        /// <summary>
        /// Add seed to use for assigning a deterministic port.
        /// </summary>
        public void AddSeed(byte[] data)
        {
            this.seed.Add(data);
        }

        /// <summary>
        /// Determine an unused port. If possible, return a deterministic
        /// port number based on the seed.
        /// </summary>
        /// <returns></returns>
        public ushort FindPort(out bool isPreferred)
        {
            var occupiedPorts = QueryOccupiedPorts();
            if (this.seed.Value != 0)
            {
                var preferredPort = (ushort)(PortRangeStart + this.seed.Value);
                Debug.Assert(preferredPort >= PortRangeStart);
                Debug.Assert(preferredPort <= PortRangeEnd);

                if (!occupiedPorts.Contains(preferredPort))
                {
                    //
                    // Preferred port is available.
                    //
                    isPreferred = true;

                    return preferredPort;
                }
            }

            //
            // Find a random port.
            //
            // Make a reasonable number of attempts without risking getting
            // stuck in an infinite loop.
            //
            for (var attempts = 0; attempts < MaxAttempts; attempts++)
            {
                var port = (ushort)StaticRandom.Next(PortRangeStart, PortRangeEnd);
                if (!occupiedPorts.Contains(port))
                {
                    isPreferred = false;
                    return port;
                }
            }

            throw new IOException(
                "Attempting to dynamically allocating a TCP port failed");
        }

        private static class StaticRandom
        {
            private static readonly Random random = new Random(Environment.TickCount);

            public static int Next(int minValue, int maxValue)
            {
                //
                // Use the same instance of Random every time.
                // 
                // NB. Random is not thread-safe, so we need a lock.
                //
                lock (random)
                {
                    return random.Next(minValue, maxValue);
                }
            }
        }
    }
}
