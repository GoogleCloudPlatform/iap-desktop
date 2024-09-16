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
using Google.Solutions.Common.Linq;
using System.Linq;
using System.Net;

namespace Google.Solutions.Apis.Compute
{
    public static class InstanceExtensions
    {
        //---------------------------------------------------------------------
        // Private
        //---------------------------------------------------------------------

        private static bool IsWindowsInstanceByGuestOsFeature(Instance instance)
        {
            //
            // For an instance to be a valid Windows instance, at least one of the disks
            // (the boot disk) has to be marked as "WINDOWS". 
            // Note that older disks might lack this feature.
            //
            return instance.Disks
                .EnsureNotNull()
                .Where(d => d.GuestOsFeatures != null)
                .SelectMany(d => d.GuestOsFeatures)
                .EnsureNotNull()
                .Any(f => f.Type == "WINDOWS");
        }

        private static bool IsWindowsInstanceByLicense(Instance instance)
        {
            //
            // For an instance to be a valid Windows instance, at least one of the disks
            // has to have an associated Windows license. This is also true for
            // BYOL'ed instances.
            //
            return instance.Disks
                .EnsureNotNull()
                .Where(d => d.Licenses != null)
                .SelectMany(d => d.Licenses)
                .EnsureNotNull()
                .Any(l => LicenseLocator.Parse(l).IsWindowsLicense());
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static bool IsWindowsInstance(this Instance instance)
        {
            return IsWindowsInstanceByGuestOsFeature(instance) ||
                   IsWindowsInstanceByLicense(instance);
        }

        public static ZoneLocator GetZoneLocator(this Instance instance)
        {
            return ZoneLocator.Parse(instance.Zone);
        }

        public static InstanceLocator GetInstanceLocator(this Instance instance)
        {
            var zone = instance.GetZoneLocator();
            return new InstanceLocator(
                zone.ProjectId,
                zone.Name,
                instance.Name);
        }

        public static IPAddress PublicAddress(this Instance instance)
        {
            return instance
                .NetworkInterfaces
                .EnsureNotNull()
                .Where(nic => nic.AccessConfigs != null)
                .SelectMany(nic => nic.AccessConfigs)
                .EnsureNotNull()
                .Where(accessConfig => accessConfig.Type == "ONE_TO_ONE_NAT")
                .Select(accessConfig => accessConfig.NatIP)
                .Where(ip => ip != null)
                .Select(ip => IPAddress.Parse(ip))
                .FirstOrDefault();
        }

        /// <summary>
        /// Return the IPv4 address of nic0, or null if that doesn't exist.
        /// </summary>
        public static IPAddress PrimaryInternalAddress(this Instance instance)
        {
            return instance
                .NetworkInterfaces
                .EnsureNotNull()
                .Where(nic => nic.Name == "nic0" && nic.NetworkIP != null)
                .Select(ip => IPAddress.Parse(ip.NetworkIP))
                .FirstOrDefault();
        }
    }
}
