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
using Google.Solutions.Common.ApiExtensions;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Management.Data.Inventory;
using Google.Solutions.IapDesktop.Extensions.Management.Services.Inventory;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Views.InstanceProperties
{
    internal class InstancePropertiesInspectorModel
    {
        private const string InstanceCategory = "Instance details";
        private const string NetworkCategory = "Instance network";
        private const string SchedulingCategory = "Scheduling";
        private const string OsCategory = "Operating system";
        private const string GuestAgentConfigurationCategory = "Guest agent configuration";
        private const string InstanceConfigurationCategory = "Instance configuration";
        private const string SshConfigurationCategory = "SSH configuration";

        private readonly Project projectDetails;
        private readonly Instance instanceDetails;
        private readonly GuestOsInfo guestOsInfo;

        private FeatureFlag GetMetadataFeatureFlag(string key, bool trueMeansEnabled)
        {
            var effectiveValue = this.instanceDetails.GetFlag(this.projectDetails, key);
            return effectiveValue == trueMeansEnabled
                ? FeatureFlag.Enabled
                : FeatureFlag.Disabled;
        }

        internal InstancePropertiesInspectorModel(
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
        [Description("Name of the VM instance")]
        public string InstanceName => this.instanceDetails.Name;

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("ID")]
        [Description("Unique ID of the VM instance")]
        public ulong InstanceId => this.instanceDetails.Id.Value;

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("Status")]
        [Description("Status of VM, see " +
                     "https://cloud.google.com/compute/docs/instances/instance-life-cycle")]
        public string Status => this.instanceDetails.Status;

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("Hostname")]
        [Description("Custom hostname, see " +
                     "https://cloud.google.com/compute/docs/instances/custom-hostname-vm")]
        public string Hostname => this.instanceDetails.Hostname;

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("Machine type")]
        [Description("Type and size of VM, see " +
                     "https://cloud.google.com/compute/docs/machine-types")]
        public string MachineType
            => MachineTypeLocator
                .FromString(this.instanceDetails.MachineType)
                .Name;

        [Browsable(true)]
        [Category(InstanceCategory)]
        [DisplayName("Licenses")]
        [Description("Operating system, see " +
                     "https://cloud.google.com/sdk/gcloud/reference/compute/images/import#--os")]
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
        [Description("Network tags, see " +
                     "https://cloud.google.com/vpc/docs/add-remove-network-tags")]
        public string Tags
             => this.instanceDetails.Tags != null && this.instanceDetails.Tags.Items != null
                ? string.Join(", ", this.instanceDetails.Tags.Items)
                : null;

        [Browsable(true)]
        [Category(NetworkCategory)]
        [DisplayName("IP address (internal)")]
        [Description("Primary internal IP addresses, see " +
                     "https://cloud.google.com/compute/docs/ip-addresses#networkaddresses")]
        public string InternalIp => this.instanceDetails.InternalAddress()?.ToString();

        [Browsable(true)]
        [Category(NetworkCategory)]
        [DisplayName("IP address (external)")]
        [Description("External IP addresses, see " +
                     "https://cloud.google.com/compute/docs/ip-addresses#externaladdresses")]
        public string ExternalIp => this.instanceDetails.PublicAddress()?.ToString();

        [Browsable(true)]
        [Category(SchedulingCategory)]
        [DisplayName("Sole tenant VM")]
        [Description("Indicates if this VM is scheduled to run on a sole-tenant node, see " +
                     "https://cloud.google.com/compute/docs/nodes/sole-tenant-nodes")]
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
        [Description("CPU architecture")]
        public string Architecture => this.guestOsInfo?.Architecture;

        [Browsable(true)]
        [Category(OsCategory)]
        [DisplayName("Kernel")]
        [Description("Kernel version of guest operating system")]
        public string KernelVersion => this.guestOsInfo?.KernelVersion;

        [Browsable(true)]
        [Category(OsCategory)]
        [DisplayName("Name")]
        [Description("Name of guest operating system")]
        public string OperatingSystemFullName => this.guestOsInfo?.OperatingSystemFullName;

        [Browsable(true)]
        [Category(OsCategory)]
        [DisplayName("Version")]
        [Description("Version of guest operating system")]
        public string OperatingSystemVersion => this.guestOsInfo?.OperatingSystemVersion.ToString();

        //---------------------------------------------------------------------
        // Guest agent configuration.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(GuestAgentConfigurationCategory)]
        [DisplayName("OS Inventory")]
        [Description("Enable OS inventory management, " +
                     "see https://cloud.google.com/compute/docs/instances/" +
                     "view-os-details#enable-guest-attributes")]
        public FeatureFlag OsInventory => GetMetadataFeatureFlag("enable-os-inventory", true);

        [Browsable(true)]
        [Category(GuestAgentConfigurationCategory)]
        [DisplayName("Diagnostics")]
        [Description("Enable collection of diagnostic information, " +
                     "see https://cloud.google.com/compute/docs/instances/" +
                     "collecting-diagnostic-information#collecting_diagnostic_information_from_a_vm")]
        public FeatureFlag Diagnostics => GetMetadataFeatureFlag("enable-diagnostics", true);

        //---------------------------------------------------------------------
        // SSH configuration.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(SshConfigurationCategory)]
        [DisplayName("OS Login")]
        [Description("Use OS Login for SSH key management, " +
                     "see https://cloud.google.com/compute/docs/instances/managing-instance-access.")]
        public FeatureFlag OsLogin => GetMetadataFeatureFlag("enable-oslogin", true);

        [Browsable(true)]
        [Category(SshConfigurationCategory)]
        [Description("Require multi-factor authentication for SSH login, " +
                     "see https://cloud.google.com/compute/docs/oslogin/setup-two-factor-authentication.")]
        [DisplayName("OS Login 2FA")]
        public FeatureFlag OsLogin2FA => GetMetadataFeatureFlag("enable-oslogin-2fa", true);

        [Browsable(true)]
        [Category(SshConfigurationCategory)]
        [Description("Require security key for SSH authentication, " +
                     "see https://cloud.google.com/compute/docs/oslogin/security-keys.")]
        [DisplayName("OS Login Security Key")]
        public FeatureFlag OsLoginWithSecurityKey => GetMetadataFeatureFlag("enable-oslogin-sk", true);

        [Browsable(true)]
        [Category(SshConfigurationCategory)]
        [DisplayName("Block project-wide SSH keys")]
        [Description("Disallow project-side SSH keys, " +
                     "see https://cloud.google.com/compute/docs/instances/adding-removing-ssh-keys#block-project-keys.")]
        public FeatureFlag BlockProjectSshKeys => GetMetadataFeatureFlag("block-project-ssh-keys", true);

        //---------------------------------------------------------------------
        // Instance configuration.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(InstanceConfigurationCategory)]
        [DisplayName("Serial port access")]
        [Description("Enable access to special administrative console, " +
                     "see https://cloud.google.com/compute/docs/instances/" +
                     "interacting-with-serial-console#enable_project_access")]
        public FeatureFlag SerialPortAccess => GetMetadataFeatureFlag("serial-port-enable", true);

        [Browsable(true)]
        [Category(InstanceConfigurationCategory)]
        [DisplayName("Guest attributes")]
        [Description("Enable guest attributes, " +
                     "see https://cloud.google.com/compute/docs/storing-retrieving-metadata#enable_attributes")]
        public FeatureFlag GuestAttributes => GetMetadataFeatureFlag("enable-guest-attributes", true);

        //---------------------------------------------------------------------
        // Loading.
        //---------------------------------------------------------------------

        public override string ToString() => this.InstanceName;

        public static async Task<InstancePropertiesInspectorModel> LoadAsync(
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

            //
            // Reading OS inventory data can fail because of a 
            // `compute.disableGuestAttributesAccess` constraint.
            //
            GuestOsInfo osInfo;
            try
            {
                osInfo = await inventoryService.GetInstanceInventoryAsync(
                    instanceLocator,
                    token)
                .ConfigureAwait(false);
            }
            catch (Exception e) when (e.Unwrap() is GoogleApiException apiEx &&
                apiEx.IsConstraintViolation())
            {
                ApplicationTraceSources.Default.TraceWarning(
                    "Failed to load OS inventory data: {0}", e);

                // Proceed with empty data.
                osInfo = null;
            }

            return new InstancePropertiesInspectorModel(
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
