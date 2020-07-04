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

using Google.Apis.Compute.v1.Data;
using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Os.Inventory
{
    public class InventoryGuestAttributes
    {
        public string Architecture { get; }
        public string KernelRelease { get; }
        public string KernelVersion { get; }
        public DateTime? LastUpdated { get; }
        public string OperatingSystem { get; }
        public string OperatingSystemFullName { get; }
        public Version OperatingSystemVersion { get; }
        public string AgentVersion { get; }

        private InventoryGuestAttributes(
            string architecture,
            string kernelRelease,
            string kernelVersion,
            string operatingSystem,
            string operatingSystemFullName,
            Version operatingSystemVersion,
            string agentVersion,
            DateTime? lastUpdated)
        {
            this.Architecture = architecture;
            this.KernelRelease = kernelRelease;
            this.KernelVersion = kernelVersion;
            this.OperatingSystem = operatingSystem;
            this.OperatingSystemFullName = operatingSystemFullName;
            this.OperatingSystemVersion = operatingSystemVersion;
            this.AgentVersion = agentVersion;
            this.LastUpdated = lastUpdated;
        }

        public static InventoryGuestAttributes FromGuestAttributes(
            List<GuestAttributesEntry> guestAttributes)
        {
            Utilities.ThrowIfNull(guestAttributes, nameof(guestAttributes));

            var version = guestAttributes.FirstOrDefault(a => a.Key == "Version")?.Value;
            var lastUpdated = guestAttributes.FirstOrDefault(a => a.Key == "LastUpdated")?.Value;

            return new InventoryGuestAttributes(
                guestAttributes.FirstOrDefault(a => a.Key == "Architecture")?.Value,
                guestAttributes.FirstOrDefault(a => a.Key == "KernelRelease")?.Value,
                guestAttributes.FirstOrDefault(a => a.Key == "KernelVersion")?.Value,
                guestAttributes.FirstOrDefault(a => a.Key == "ShortName")?.Value,
                guestAttributes.FirstOrDefault(a => a.Key == "LongName")?.Value,
                version != null ? Version.Parse(version) : null,
                guestAttributes.FirstOrDefault(a => a.Key == "OSConfigAgentVersion")?.Value,
                lastUpdated != null ? (DateTime?)DateTime.Parse(lastUpdated) : null);
        }
    }
}
