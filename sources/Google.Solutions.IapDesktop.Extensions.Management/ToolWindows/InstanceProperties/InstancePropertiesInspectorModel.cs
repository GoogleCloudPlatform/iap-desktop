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
using Google.Solutions.Apis;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory;
using Google.Solutions.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.InstanceProperties
{
    [Service]
    public class InstancePropertiesInspectorModel
    {
        private static class Categories
        {
            public const string Instance = "Basic information";
            public const string Security = "Security";
            public const string Network = "Networking";
            public const string Scheduling = "Scheduling";
            public const string Os = "Operating system";
            public const string GuestAgentConfiguration = "Guest agent configuration";
            public const string InstanceConfiguration = "Configuration";
            public const string SshConfiguration = "SSH configuration";
        }

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
            InstanceLocator instance,
            Project projectDetails,
            Instance instanceDetails,
            GuestOsInfo guestOsInfo)
        {
            Debug.Assert(projectDetails != null);
            Debug.Assert(instanceDetails != null);

            this.projectDetails = projectDetails;
            this.instanceDetails = instanceDetails;
            this.guestOsInfo = guestOsInfo;

            //
            // Basic information
            //
            this.InstanceName = this.instanceDetails.Name;
            this.InstanceId = this.instanceDetails.Id ?? 0;
            this.Status = this.instanceDetails.Status;
            this.Hostname = this.instanceDetails.Hostname;
            this.MachineType = this.instanceDetails.MachineType != null
                ? MachineTypeLocator.Parse(this.instanceDetails.MachineType).Name
                : null;
            this.Licenses = this.instanceDetails.Disks
                .EnsureNotNull()
                .Where(d => d.Licenses != null && d.Licenses.Any())
                .SelectMany(d => d.Licenses)
                .Select(l => LicenseLocator.Parse(l).Name)
                .ToList();
            this.CpuPlatform = this.instanceDetails.CpuPlatform;
            this.Labels = this.instanceDetails.Labels;

            //
            // Security.
            //
            var serviceAccount = this.instanceDetails.ServiceAccounts?.FirstOrDefault();
            this.ServiceAccount = serviceAccount?.Email;
            this.ServiceAccountScopes = serviceAccount?.Scopes;
            this.VtpmEnabled = this.instanceDetails.ShieldedInstanceConfig?.EnableVtpm == true
                ? FeatureFlag.Enabled
                : FeatureFlag.Disabled;
            this.SecureBootEnabled = this.instanceDetails.ShieldedInstanceConfig?.EnableSecureBoot == true
                ? FeatureFlag.Enabled
                : FeatureFlag.Disabled;
            this.IntegrityMonitoringEnabled = this.instanceDetails.ShieldedInstanceConfig?.EnableIntegrityMonitoring == true
                ? FeatureFlag.Enabled
                : FeatureFlag.Disabled;

            //
            // Network.
            //
            this.Tags = this.instanceDetails.Tags?.Items;
            this.InternalIp = this.instanceDetails.PrimaryInternalAddress()?.ToString();
            this.ExternalIp = this.instanceDetails.PublicAddress()?.ToString();
            this.InternalZonalDnsName = new InternalDnsName.ZonalName(instance).Name;

            //
            // Scheduling.
            //
            this.IsSoleTenant = this.instanceDetails.Scheduling?.NodeAffinities != null &&
               this.instanceDetails.Scheduling.NodeAffinities.Any();
            this.IsPreemptible = this.instanceDetails.Scheduling.Preemptible == true;

            //
            // OS Inventory data.
            //
            this.IsOsInventoryInformationPopulated = this.guestOsInfo != null;
            this.Architecture = this.guestOsInfo?.Architecture;
            this.KernelVersion = this.guestOsInfo?.KernelVersion;
            this.OperatingSystemFullName = this.guestOsInfo?.OperatingSystemFullName;
            this.OperatingSystemVersion = this.guestOsInfo?.OperatingSystemVersion?.ToString();

            //
            // Guest agent configuration.
            //
            this.OsInventory = GetMetadataFeatureFlag("enable-os-inventory", true);
            this.Diagnostics = GetMetadataFeatureFlag("enable-diagnostics", true);
            this.OsLogin = GetMetadataFeatureFlag("enable-oslogin", true);
            this.OsLogin2FA = GetMetadataFeatureFlag("enable-oslogin-2fa", true);
            this.OsLoginWithSecurityKey = GetMetadataFeatureFlag("enable-oslogin-sk", true);
            this.BlockProjectSshKeys = GetMetadataFeatureFlag("block-project-ssh-keys", true);

            //
            // Instance configuration.
            //
            this.SerialPortAccess = GetMetadataFeatureFlag("serial-port-enable", true);
            this.GuestAttributes = GetMetadataFeatureFlag("enable-guest-attributes", true);

            //
            // Metadata, where instance metadata overrides project metadata.
            //
            var metadata = this.projectDetails
                .CommonInstanceMetadata?
                .Items?
                .EnsureNotNull()
                .ToDictionary(i => i.Key, i => i.Value);
            this.instanceDetails
                .Metadata?
                .Items?
                .EnsureNotNull()
                .ToList()
                .ForEach(i => metadata[i.Key] = i.Value);

            this.Metadata = metadata;
        }

        //---------------------------------------------------------------------
        // Basic information.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(Categories.Instance)]
        [DisplayName("Name")]
        [Description("Name of the VM instance")]
        public string InstanceName { get; }

        [Browsable(true)]
        [Category(Categories.Instance)]
        [DisplayName("ID")]
        [Description("The unique ID of the VM instance")]
        public ulong InstanceId { get; }

        [Browsable(true)]
        [Category(Categories.Instance)]
        [DisplayName("Status")]
        [Description("The current status of the VM, see " +
                     "https://cloud.google.com/compute/docs/instances/instance-life-cycle")]
        public string Status { get; }

        [Browsable(true)]
        [Category(Categories.Instance)]
        [DisplayName("Hostname")]
        [Description("The custom hostname, see " +
                     "https://cloud.google.com/compute/docs/instances/custom-hostname-vm")]
        public string Hostname { get; }

        [Browsable(true)]
        [Category(Categories.Instance)]
        [DisplayName("Machine type")]
        [Description("The type and size of VM, see " +
                     "https://cloud.google.com/compute/docs/machine-types")]
        public string MachineType { get; }

        [Browsable(true)]
        [Category(Categories.Instance)]
        [DisplayName("CPU platform")]
        [Description("CPU platform, see " +
                     "https://cloud.google.com/compute/docs/cpu-platforms")]
        public string CpuPlatform { get; }

        [Browsable(true)]
        [Category(Categories.Instance)]
        [DisplayName("Licenses")]
        [Description("The licenses applied to the VM, see " +
                     "https://cloud.google.com/sdk/gcloud/reference/compute/images/import#--os")]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public ICollection<string> Licenses { get; }

        [Browsable(true)]
        [Category(Categories.Instance)]
        [DisplayName("Labels")]
        [Description("Labels, see " +
                     "https://cloud.google.com/compute/docs/labeling-resources")]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public IDictionary<string, string> Labels { get; }

        //---------------------------------------------------------------------
        // Security.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(Categories.Security)]
        [DisplayName("Service account")]
        [Description("The service account that is attached to this instance")]
        public string ServiceAccount { get; }

        [Browsable(true)]
        [Category(Categories.Security)]
        [DisplayName("Service account scopes")]
        [Description("OAuth scopes for which this VM can obtain credentials")]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public ICollection<string> ServiceAccountScopes { get; }

        [Browsable(true)]
        [Category(Categories.Security)]
        [DisplayName("vTPM")]
        [Description("Indicates whether this VM has a virtual TPM device")]
        public FeatureFlag VtpmEnabled { get; }

        [Browsable(true)]
        [Category(Categories.Security)]
        [DisplayName("Secure boot")]
        [Description("Indicates whether this VM uses secure boot")]
        public FeatureFlag SecureBootEnabled { get; }

        [Browsable(true)]
        [Category(Categories.Security)]
        [DisplayName("Integrity monitoring")]
        [Description("Indicates whether this uses integrity monitoring")]
        public FeatureFlag IntegrityMonitoringEnabled { get; }

        //---------------------------------------------------------------------
        // Network.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(Categories.Network)]
        [DisplayName("Network tags")]
        [Description("Network tags, see " +
                     "https://cloud.google.com/vpc/docs/add-remove-network-tags")]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public ICollection<string> Tags { get; }

        [Browsable(true)]
        [Category(Categories.Network)]
        [DisplayName("IP address (internal)")]
        [Description("The VM's primary internal IP address, see " +
                     "https://cloud.google.com/compute/docs/ip-addresses#networkaddresses")]
        public string InternalIp { get; }

        [Browsable(true)]
        [Category(Categories.Network)]
        [DisplayName("IP address (external)")]
        [Description("The VM's external IP address, see " +
                     "https://cloud.google.com/compute/docs/ip-addresses#externaladdresses")]
        public string ExternalIp { get; }

        [Browsable(true)]
        [Category(Categories.Network)]
        [DisplayName("Internal DNS name")]
        [Description("Internal zonal DNS name, see " +
                     "https://cloud.google.com/compute/docs/internal-dns#about_internal_dns")]
        public string InternalZonalDnsName { get; }

        //---------------------------------------------------------------------
        // Scheduling.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(Categories.Scheduling)]
        [DisplayName("Sole tenant VM")]
        [Description("Indicates whether this VM is scheduled to run on a sole-tenant node, see " +
                     "https://cloud.google.com/compute/docs/nodes/sole-tenant-nodes")]
        public bool IsSoleTenant { get; }

        [Browsable(true)]
        [Category(Categories.Scheduling)]
        [DisplayName("Preemptible VM")]
        [Description("Indicates whether this VM is preemptible, see " +
                     "https://cloud.google.com/compute/docs/instances/preemptible")]
        public bool IsPreemptible { get; }

        //---------------------------------------------------------------------
        // OS Inventory data.
        //---------------------------------------------------------------------

        [Browsable(false)]
        public bool IsOsInventoryInformationPopulated { get; }

        [Browsable(true)]
        [Category(Categories.Os)]
        [DisplayName("Architecture")]
        [Description("The VM's CPU architecture")]
        public string Architecture { get; }

        [Browsable(true)]
        [Category(Categories.Os)]
        [DisplayName("Kernel")]
        [Description("The guest operating system's kernel version")]
        public string KernelVersion { get; }

        [Browsable(true)]
        [Category(Categories.Os)]
        [DisplayName("Name")]
        [Description("The name of the guest operating system")]
        public string OperatingSystemFullName { get; }

        [Browsable(true)]
        [Category(Categories.Os)]
        [DisplayName("Version")]
        [Description("The version of the guest operating system")]
        public string OperatingSystemVersion { get; }

        //---------------------------------------------------------------------
        // Guest agent configuration.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(Categories.GuestAgentConfiguration)]
        [DisplayName("OS Inventory")]
        [Description("Indicates whether OS inventory management is enabled, " +
                     "see https://cloud.google.com/compute/docs/instances/" +
                     "view-os-details#enable-guest-attributes")]
        public FeatureFlag OsInventory { get; }

        [Browsable(true)]
        [Category(Categories.GuestAgentConfiguration)]
        [DisplayName("Diagnostics")]
        [Description("Indicates whether the collection of diagnostic information is enabled, " +
                     "see https://cloud.google.com/compute/docs/instances/" +
                     "collecting-diagnostic-information#collecting_diagnostic_information_from_a_vm")]
        public FeatureFlag Diagnostics { get; }

        //---------------------------------------------------------------------
        // SSH configuration.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(Categories.SshConfiguration)]
        [DisplayName("OS Login")]
        [Description("Indicates whether OS Login is enabled, " +
                     "see https://cloud.google.com/compute/docs/instances/managing-instance-access.")]
        public FeatureFlag OsLogin { get; }

        [Browsable(true)]
        [Category(Categories.SshConfiguration)]
        [Description("Indicates whether the instance requires multi-factor authentication for SSH " +
                     "see https://cloud.google.com/compute/docs/oslogin/setup-two-factor-authentication.")]
        [DisplayName("OS Login 2FA")]
        public FeatureFlag OsLogin2FA { get; }

        [Browsable(true)]
        [Category(Categories.SshConfiguration)]
        [Description("Indicates whether the instance requires a security key for SSH, " +
                     "see https://cloud.google.com/compute/docs/oslogin/security-keys.")]
        [DisplayName("OS Login Security Key")]
        public FeatureFlag OsLoginWithSecurityKey { get; }

        [Browsable(true)]
        [Category(Categories.SshConfiguration)]
        [DisplayName("Block project-wide SSH keys")]
        [Description("Indicates whether project-side SSH keys are disabled for this VM, " +
                     "see https://cloud.google.com/compute/docs/instances/adding-removing-ssh-keys#block-project-keys.")]
        public FeatureFlag BlockProjectSshKeys { get; }

        //---------------------------------------------------------------------
        // Instance configuration.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category(Categories.InstanceConfiguration)]
        [DisplayName("Serial port access")]
        [Description("Indicates whether the special administrative console can be used, " +
                     "see https://cloud.google.com/compute/docs/instances/" +
                     "interacting-with-serial-console#enable_project_access")]
        public FeatureFlag SerialPortAccess { get; }

        [Browsable(true)]
        [Category(Categories.InstanceConfiguration)]
        [DisplayName("Guest attributes")]
        [Description("Indicates whether guest attributes are enabled, " +
                     "see https://cloud.google.com/compute/docs/storing-retrieving-metadata#enable_attributes")]
        public FeatureFlag GuestAttributes { get; }

        [Browsable(true)]
        [Category(Categories.InstanceConfiguration)]
        [DisplayName("Metadata")]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        [Description("Merge result of project and instance metadata, " +
                     "see https://cloud.google.com/compute/docs/metadata/overview")]
        public IDictionary<string, string> Metadata { get; }

        //---------------------------------------------------------------------
        // Loading.
        //---------------------------------------------------------------------

        public override string ToString() => this.InstanceName;

        public static async Task<InstancePropertiesInspectorModel> LoadAsync(
            InstanceLocator instanceLocator,
            IComputeEngineClient computeClient,
            IGuestOsInventory packageInventory,
            CancellationToken token)
        {
            var instance = await computeClient
                .GetInstanceAsync(
                    instanceLocator,
                    token)
                .ConfigureAwait(false);

            var project = await computeClient
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
                osInfo = await packageInventory.GetInstanceInventoryAsync(
                    instanceLocator,
                    token)
                .ConfigureAwait(false);
            }
            catch (Exception e) when (e.Unwrap() is GoogleApiException apiEx &&
                apiEx.IsConstraintViolation())
            {
                ApplicationTraceSource.Log.TraceWarning(
                    "Failed to load OS inventory data: {0}", e);

                // Proceed with empty data.
                osInfo = null;
            }

            return new InstancePropertiesInspectorModel(
                instanceLocator,
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
