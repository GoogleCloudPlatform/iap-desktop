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
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Os.Inventory;
using Google.Solutions.IapDesktop.Extensions.Os.Services.Inventory;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Os.Views.InstanceDetails
{
    internal class InstanceDetailsModel
    {
        private const string InstanceCategory = "Instance details";
        private const string NetworkCategory = "Instance network";
        private const string SchedulingCategory = "Scheduling";
        private const string OsCategory = "Operating system";
        private const string GuestAgentConfigurationCategory = "Guest agent configuration";
        private const string InstanceConfigurationCategory = "Instance configuration";

        private readonly Project projectDetails;
        private readonly Instance instanceDetails;
        private readonly GuestOsInfo guestOsInfo;

        private string GetMetadata(string key)
        {
            //
            // Check instance-specific metadata.
            //
            var value = this.instanceDetails.Metadata?.Items
                .EnsureNotNull()
                .FirstOrDefault(item => item.Key == key)?.Value;
            if (value != null)
            {
                return value;
            }

            //
            // Check common instance metadata.
            //
            return this.projectDetails.CommonInstanceMetadata?.Items
                .EnsureNotNull()
                .FirstOrDefault(item => item.Key == key)?.Value;
        }

        private FeatureFlag GetMetadataFeatureFlag(string key, bool trueMeansEnabled)
        {
            var isTrue = "true".Equals(GetMetadata(key), StringComparison.OrdinalIgnoreCase);
            var effectiveValue = trueMeansEnabled ? isTrue : !isTrue;
            return effectiveValue
                ? FeatureFlag.Enabled
                : FeatureFlag.Disabled;
        }

        internal InstanceDetailsModel(
            Project projectDetails,
            Instance instanceDetails,
            GuestOsInfo guestOsInfo)
        {
            Debug.Assert(projectDetails != null);
            Debug.Assert(instanceDetails != null);

            this.projectDetails = projectDetails;
            this.instanceDetails = instanceDetails;
            this.guestOsInfo = guestOsInfo;
        }

        //---------------------------------------------------------------------
        // Browsable properties.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("Name")]
        public string InstanceName => this.instanceDetails.Name;

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("ID")]
        public ulong InstanceId => this.instanceDetails.Id.Value;

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("Status")]
        public string Status => this.instanceDetails.Status;

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("Hostname")]
        public string Hostname => this.instanceDetails.Hostname;

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("Machine type")]
        public string MachineType
            => MachineTypeLocator
                .FromString(this.instanceDetails.MachineType)
                .Name;

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("Licenses")]
        public string Licenses
             => this.instanceDetails.Disks != null
                ? string.Join(", ", this.instanceDetails.Disks
                    .EnsureNotNull()
                    .Where(d => d.Licenses != null && d.Licenses.Any())
                    .SelectMany(d => d.Licenses)
                    .Select(l => LicenseLocator.FromString(l).Name))
                : null;
        //---------------------------------------------------------------------
        // Network.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(NetworkCategory)]
        [DisplayName("Network tags")]
        public string Tags
             => this.instanceDetails.Tags != null && this.instanceDetails.Tags.Items != null
                ? string.Join(", ", this.instanceDetails.Tags.Items)
                : null;

        [Browsable(true)]
        [Category(NetworkCategory)]
        [DisplayName("IP address (internal)")]
        public string InternalIp
            => this.instanceDetails
                .NetworkInterfaces
                .EnsureNotNull()
                .Select(nic => nic.NetworkIP)
                .FirstOrDefault();

        [Browsable(true)]
        [Category(NetworkCategory)]
        [DisplayName("IP address (external)")]
        public string ExternalIp
            => this.instanceDetails
                .NetworkInterfaces
                .EnsureNotNull()
                .Where(nic => nic.AccessConfigs != null)
                .SelectMany(nic => nic.AccessConfigs)
                .EnsureNotNull()
                .Where(accessConfig => accessConfig.Type == "ONE_TO_ONE_NAT")
                .Select(accessConfig => accessConfig.NatIP)
                .FirstOrDefault();

        [Browsable(true)]
        [Category(SchedulingCategory)]
        [DisplayName("Sole tenant VM")]
        public bool IsSoleTenant
            => this.instanceDetails.Scheduling?.NodeAffinities != null &&
               this.instanceDetails.Scheduling.NodeAffinities.Any();

        //---------------------------------------------------------------------
        // OS Inventory data.
        //---------------------------------------------------------------------

        [Browsable(false)]
        public bool IsOsInventoryInformationPopulated => this.guestOsInfo != null;

        [Browsable(true)]
        [Category(OsCategory)]
        [DisplayName("Architecture")]
        public string Architecture => this.guestOsInfo?.Architecture;

        [Browsable(true)]
        [Category(OsCategory)]
        [DisplayName("Kernel")]
        public string KernelVersion => this.guestOsInfo?.KernelVersion;

        [Browsable(true)]
        [Category(OsCategory)]
        [DisplayName("Name")]
        public string OperatingSystemFullName => this.guestOsInfo?.OperatingSystemFullName;

        [Browsable(true)]
        [Category(OsCategory)]
        [DisplayName("Version")]
        public string OperatingSystemVersion => this.guestOsInfo?.OperatingSystemVersion.ToString();

        //---------------------------------------------------------------------
        // Guest agent configuration.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(GuestAgentConfigurationCategory)]
        [DisplayName("OS Inventory")]
        public FeatureFlag OsInventory => GetMetadataFeatureFlag("enable-os-inventory", true);

        [Browsable(true)]
        [Category(GuestAgentConfigurationCategory)]
        [DisplayName("Diagnostics")]
        public FeatureFlag Diagnostics => GetMetadataFeatureFlag("enable-diagnostics", true);

        //
        // NB. OS login not relevant yet for Windows.
        //

        //[Browsable(true)]
        //[Category(GuestAgentConfigurationCategory)]
        //[DisplayName("OS Login")]
        //public FeatureFlag OsLogin => GetMetadataFeatureFlag("enable-oslogin", true);

        //[Browsable(true)]
        //[Category(GuestAgentConfigurationCategory)]
        //[DisplayName("OS Login 2FA")]
        //public FeatureFlag OsLogin2FA => GetMetadataFeatureFlag("enable-oslogin-2fa", true);

        //---------------------------------------------------------------------
        // Instance configuration.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(InstanceConfigurationCategory)]
        [DisplayName("Serial port access")]
        public FeatureFlag SerialPortAccess => GetMetadataFeatureFlag("serial-port-enable", true);

        [Browsable(true)]
        [Category(InstanceConfigurationCategory)]
        [DisplayName("Guest attributes")]
        public FeatureFlag GuestAttributes => GetMetadataFeatureFlag("enable-guest-attributes", true);

        [Browsable(true)]
        [Category(InstanceConfigurationCategory)]
        [DisplayName("Internal DNS mode")]
        public string InternalDnsMode => GetMetadata("VmDnsSetting");

        //---------------------------------------------------------------------
        // Loading.
        //---------------------------------------------------------------------

        public async static Task<InstanceDetailsModel> LoadAsync(
            InstanceLocator instanceLocator,
            IComputeEngineAdapter computeEngineAdapter,
            IInventoryService inventoryService,
            CancellationToken token)
        {
            var instance = await computeEngineAdapter
                .GetInstanceAsync(
                    instanceLocator,
                    token)
                .ConfigureAwait(false);

            var project = await computeEngineAdapter
                .GetProjectAsync(
                    instanceLocator.ProjectId, 
                    token)
                .ConfigureAwait(false);

            var osInfo = await inventoryService.GetInstanceInventoryAsync(
                    instanceLocator,
                    token)
                .ConfigureAwait(false);

            return new InstanceDetailsModel(
                project,
                instance,
                osInfo);
        }
    }

    public enum FeatureFlag
    {
        Enabled,
        Disabled
    }
}
