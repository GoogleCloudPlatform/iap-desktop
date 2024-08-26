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
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory
{
    public class GuestOsInfo
    {
        public const string GuestAttributePath = "guestInventory/";

        public InstanceLocator Instance { get; }

        public string? Architecture { get; }
        public string? KernelRelease { get; }
        public string? KernelVersion { get; }
        public DateTime? LastUpdated { get; }
        public string? OperatingSystem { get; }
        public string? OperatingSystemFullName { get; }
        public Version? OperatingSystemVersion { get; }
        public string? AgentVersion { get; }

        public GuestPackages InstalledPackages { get; }
        public GuestPackages AvailablePackages { get; }

        internal GuestOsInfo(
            InstanceLocator instance,
            string? architecture,
            string? kernelRelease,
            string? kernelVersion,
            string? operatingSystem,
            string? operatingSystemFullName,
            Version? operatingSystemVersion,
            string? agentVersion,
            DateTime? lastUpdated,
            GuestPackages installedPackages,
            GuestPackages availablePackages)
        {
            this.Instance = instance;
            this.Architecture = architecture;
            this.KernelRelease = kernelRelease;
            this.KernelVersion = kernelVersion;
            this.OperatingSystem = operatingSystem;
            this.OperatingSystemFullName = operatingSystemFullName;
            this.OperatingSystemVersion = operatingSystemVersion;
            this.AgentVersion = agentVersion;
            this.LastUpdated = lastUpdated;
            this.InstalledPackages = installedPackages;
            this.AvailablePackages = availablePackages;
        }

        private static T DecodeAndParseBase64Gzip<T>(string base64gzipped)
        {
            using (var reader = new StreamReader(
                    new GZipStream(
                        new MemoryStream(Convert.FromBase64String(base64gzipped)),
                        CompressionMode.Decompress)))
            {
                var json = reader.ReadToEnd();
            }

            using (var reader = new JsonTextReader(
                new StreamReader(
                    new GZipStream(
                        new MemoryStream(Convert.FromBase64String(base64gzipped)),
                        CompressionMode.Decompress))))
            {
                return new JsonSerializer().Deserialize<T>(reader);
            }
        }

        public static GuestOsInfo FromGuestAttributes(
            InstanceLocator instanceLocator,
            IList<GuestAttributesEntry> guestAttributes)
        {
            Precondition.ExpectNotNull(guestAttributes, nameof(guestAttributes));

            var lastUpdated = guestAttributes.FirstOrDefault(a => a.Key == "LastUpdated")?.Value;
            var installedPackages = guestAttributes.FirstOrDefault(a => a.Key == "InstalledPackages")?.Value;
            var availablePackages = guestAttributes.FirstOrDefault(a => a.Key == "PackageUpdates")?.Value;
            var version = guestAttributes.FirstOrDefault(a => a.Key == "Version")?.Value;

            if (version == null || string.IsNullOrWhiteSpace(version))
            {
                version = null;
            }
            else if (version.IndexOf('.') == -1)
            {
                // Version.Parse expects at least one dot-version.
                version += ".0";
            }

            return new GuestOsInfo(
                instanceLocator,
                guestAttributes.FirstOrDefault(a => a.Key == "Architecture")?.Value,
                guestAttributes.FirstOrDefault(a => a.Key == "KernelRelease")?.Value,
                guestAttributes.FirstOrDefault(a => a.Key == "KernelVersion")?.Value,
                guestAttributes.FirstOrDefault(a => a.Key == "ShortName")?.Value,
                guestAttributes.FirstOrDefault(a => a.Key == "LongName")?.Value,
                version != null
                    ? Version.Parse(version)
                    : null,
                guestAttributes.FirstOrDefault(a => a.Key == "OSConfigAgentVersion")?.Value,
                lastUpdated != null
                    ? (DateTime?)DateTime.Parse(lastUpdated)
                    : null,
                installedPackages != null
                    ? DecodeAndParseBase64Gzip<GuestPackages>(installedPackages)
                    : null,
                availablePackages != null
                    ? DecodeAndParseBase64Gzip<GuestPackages>(availablePackages)
                    : null);
        }
    }
}
