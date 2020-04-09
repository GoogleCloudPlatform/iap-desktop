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
using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.Registry;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.SettingsEditor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.ProjectExplorer
{
    internal class CloudNode : TreeNode, IProjectExplorerCloudNode
    {
        private const int IconIndex = 0;

        public CloudNode()
            : base("Google Cloud", IconIndex, IconIndex)
        {
        }
    }

    internal abstract class InventoryNode : TreeNode, IProjectExplorerNode, ISettingsObject
    {
        private readonly InventoryNode parent;
        private readonly InventorySettingsBase settings;
        private readonly Action<InventorySettingsBase> saveSettings;

        public InventoryNode(
            string name,
            int iconIndex,
            InventorySettingsBase settings,
            Action<InventorySettingsBase> saveSettings,
            InventoryNode parent)
            : base(name, iconIndex, iconIndex)
        {
            this.settings = settings;
            this.saveSettings = saveSettings;
            this.parent = parent;
        }

        public void SaveChanges()
        {
            this.saveSettings(this.settings);
        }

        internal static string ShortIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);

        //---------------------------------------------------------------------
        // PropertyGrid-compatible settings properties.
        //
        // The ShouldSerializeXxx callbacks control whether a property is shown
        // bold (true) or regular (false). Note that these callbacks cease
        // working once a Default attribute is applied.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Credentials")]
        [DisplayName("Username")]
        [Description("Windows logon username")]
        public string Username
        {
            get => IsUsernameSet
                ? this.settings.Username
                : this.parent?.Username;
            set => this.settings.Username = string.IsNullOrEmpty(value) ? null : value;
        }

        protected bool IsUsernameSet => this.settings.Username != null;

        public bool ShouldSerializeUsername() => IsUsernameSet;

        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Credentials")]
        [DisplayName("Password")]
        [Description("Windows logon password")]
        [PasswordPropertyText(true)]
        public string CleartextPassword
        {
            get => IsPasswordSet
                ? new string('*', 8)
                : this.parent?.CleartextPassword;
            set => this.Password = string.IsNullOrEmpty(value)
                ? null
                : SecureStringExtensions.FromClearText(value);
        }

        protected SecureString Password
        {
            get => IsPasswordSet
                ? this.settings.Password
                : this.parent?.Password;
            set => this.settings.Password = value;
        }

        protected bool IsPasswordSet => this.settings.Password != null;

        public bool ShouldSerializeCleartextPassword() => IsPasswordSet;

        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Credentials")]
        [DisplayName("Domain")]
        [Description("Windows logon domain")]
        public string Domain
        {
            get => IsDomainSet
                ? this.settings.Domain 
                : this.parent?.Domain;
            set => this.settings.Domain = string.IsNullOrEmpty(value) ? null : value;
        }

        protected bool IsDomainSet => this.settings.Domain != null;

        public bool ShouldSerializeDomain() => IsDomainSet;

        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Display")]
        [DisplayName("Show connection bar")]
        [Description("Show connection bar in full-screen mode")]
        public RdpConnectionBarState ConnectionBar
        {
            get => IsConnectionBarSet
                ? this.settings.ConnectionBar
                : (this.parent != null ? this.parent.ConnectionBar : RdpConnectionBarState._Default);
            set => this.settings.ConnectionBar = value;
        }

        protected bool IsConnectionBarSet 
            => this.settings.ConnectionBar != RdpConnectionBarState._Default;

        public bool ShouldSerializeConnectionBar() => IsConnectionBarSet;

        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Display")]
        [DisplayName("Desktop size")]
        [Description("Size of remote desktop")]
        public RdpDesktopSize DesktopSize
        {
            get => IsDesktopSizeSet
                ? this.settings.DesktopSize
                : (this.parent != null ? this.parent.DesktopSize : RdpDesktopSize._Default);
            set => this.settings.DesktopSize = value;
        }

        protected bool IsDesktopSizeSet 
            => this.settings.DesktopSize != RdpDesktopSize._Default;

        public bool ShouldSerializeDesktopSize() => IsDesktopSizeSet;

        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Display")]
        [DisplayName("Color depth")]
        [Description("Color depth of remote desktop")]
        public RdpColorDepth ColorDepth
        {
            get => IsColorDepthSet
                ? this.settings.ColorDepth
                : (this.parent != null ? this.parent.ColorDepth : RdpColorDepth._Default);
            set => this.settings.ColorDepth = value;
        }

        protected bool IsColorDepthSet 
            => this.settings.ColorDepth != RdpColorDepth._Default;

        public bool ShouldSerializeColorDepth() => IsColorDepthSet;

        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Connection")]
        [DisplayName("Server authentication")]
        [Description("Require server authentication when connecting")]
        public RdpAuthenticationLevel AuthenticationLevel
        {
            get => IsAuthenticationLevelSet
                ? this.settings.AuthenticationLevel
                : (this.parent != null ? this.parent.AuthenticationLevel : RdpAuthenticationLevel._Default);
            set => this.settings.AuthenticationLevel = value;
        }

        protected bool IsAuthenticationLevelSet 
            => this.settings.AuthenticationLevel != RdpAuthenticationLevel._Default;

        public bool ShouldSerializeAuthenticationLevel() => IsAuthenticationLevelSet;

        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Local resources")]
        [DisplayName("Redirect clipboard")]
        [Description("Allow clipboard contents to be shared with remote desktop")]
        public RdpRedirectClipboard RedirectClipboard
        {
            get => IsRedirectClipboardSet
                ? this.settings.RedirectClipboard
                : (this.parent != null ? this.parent.RedirectClipboard : RdpRedirectClipboard._Default);
            set => this.settings.RedirectClipboard = value;
        }

        protected bool IsRedirectClipboardSet 
            => this.settings.RedirectClipboard != RdpRedirectClipboard._Default;

        public bool ShouldSerializeRedirectClipboard() => IsRedirectClipboardSet;

        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Local resources")]
        [DisplayName("Audio mode")]
        [Description("Redirect audio when playing on server")]
        public RdpAudioMode AudioMode
        {
            get => IsAudioModeSet
                ? this.settings.AudioMode
                : (this.parent != null ? this.parent.AudioMode : RdpAudioMode._Default);
            set => this.settings.AudioMode = value;
        }

        protected bool IsAudioModeSet
            => this.settings.AudioMode != RdpAudioMode._Default;

        public bool ShouldSerializeAudioMode() => IsAudioModeSet;

        //---------------------------------------------------------------------

        [Browsable(true)]
        [BrowsableSetting]
        [Category("Performance")]
        [DisplayName("Bitmap caching")]
        [Description("Use persistent bitmap cache")]
        public RdpBitmapPersistence BitmapPersistence
        {
            get => IsBitmapPersistenceSet
                ? this.settings.BitmapPersistence
                : (this.parent != null ? this.parent.BitmapPersistence : RdpBitmapPersistence._Default);
            set => this.settings.BitmapPersistence = value;
        }

        protected bool IsBitmapPersistenceSet 
            => this.settings.BitmapPersistence != RdpBitmapPersistence._Default;

        public bool ShouldSerializeBitmapPersistence() => IsBitmapPersistenceSet;
    }

    internal class ProjectNode : InventoryNode, IProjectExplorerProjectNode
    {
        private const int IconIndex = 1;

        private readonly InventorySettingsRepository settingsRepository;

        public string ProjectId => this.Text;

        public ProjectNode(InventorySettingsRepository settingsRepository, string projectId)
            : base(
                  projectId,
                  IconIndex,
                  settingsRepository.GetProjectSettings(projectId),
                  settings => settingsRepository.SetProjectSettings((ProjectSettings)settings),
                  null)
        {
            this.settingsRepository = settingsRepository;
        }

        public void Populate(
            IEnumerable<Instance> allInstances,
            Func<VmInstanceReference, bool> isConnected)
        {
            this.Nodes.Clear();

            // Narrow the list down to Windows instances - there is no point 
            // of adding Linux instanes to the list of servers.
            var instances = allInstances.Where(i => ComputeEngineAdapter.IsWindowsInstance(i));
            var zoneIds = instances.Select(i => InventoryNode.ShortIdFromUrl(i.Zone)).ToHashSet();

            foreach (var zoneId in zoneIds)
            {
                var zoneSettings = this.settingsRepository.GetZoneSettings(
                    this.ProjectId,
                    zoneId);
                var zoneNode = new ZoneNode(
                    zoneSettings,
                    changedSettings => this.settingsRepository.SetZoneSettings(this.ProjectId, changedSettings),
                    this);

                var instancesInZone = instances
                    .Where(i => InventoryNode.ShortIdFromUrl(i.Zone) == zoneId)
                    .OrderBy(i => i.Name);

                foreach (var instance in instancesInZone)
                {
                    var instanceSettings = this.settingsRepository.GetVmInstanceSettings(
                        this.ProjectId,
                        instance.Name);
                    var instanceNode = new VmInstanceNode(
                        instance,
                        instanceSettings,
                        changedSettings => this.settingsRepository.SetVmInstanceSettings(this.ProjectId, changedSettings),
                        zoneNode);
                    instanceNode.IsConnected = isConnected(
                        new VmInstanceReference(this.ProjectId, zoneId, instance.Name));

                    zoneNode.Nodes.Add(instanceNode);
                }

                this.Nodes.Add(zoneNode);
                zoneNode.Expand();
            }

            Expand();
        }
    }

    internal class ZoneNode : InventoryNode, IProjectExplorerZoneNode
    {
        private const int IconIndex = 3;

        public string ProjectId => ((ProjectNode)this.Parent).ProjectId;
        public string ZoneId => this.Text;

        public ZoneNode(
            ZoneSettings settings,
            Action<ZoneSettings> saveSettings,
            ProjectNode parent)
            : base(
                  settings.ZoneId,
                  IconIndex,
                  settings,
                  changedSettings => saveSettings((ZoneSettings)changedSettings),
                  parent)
        {
        }
    }

    internal class VmInstanceNode : InventoryNode, IProjectExplorerVmInstanceNode
    {
        private const int DisconnectedIconIndex = 4;
        private const int ConnectedIconIndex = 5;

        public VmInstanceReference Reference
            => new VmInstanceReference(this.ProjectId, this.ZoneId, this.InstanceName);

        public string ProjectId => ((ZoneNode)this.Parent).ProjectId;
        public string ZoneId => ((ZoneNode)this.Parent).ZoneId;

        public VmInstanceSettings EffectiveSettingsWithInheritanceApplied
            => new VmInstanceSettings()
            {
                InstanceName = this.InstanceName,
                AudioMode = this.AudioMode,
                AuthenticationLevel = this.AuthenticationLevel,
                ColorDepth = this.ColorDepth,
                ConnectionBar = this.ConnectionBar,
                DesktopSize = this.DesktopSize,
                RedirectClipboard = this.RedirectClipboard,
                UserAuthenticationBehavior = RdpUserAuthenticationBehavior._Default,
                Username = this.Username,
                Password = this.Password,
                Domain = this.Domain,
                BitmapPersistence = this.BitmapPersistence
            };

        private static string InternalIpFromInstance(Instance instance)
        {
            if (instance == null)
            {
                return null;
            }

            return instance
                .NetworkInterfaces
                .EnsureNotNull()
                .Select(nic => nic.NetworkIP)
                .FirstOrDefault();
        }
        private static string ExternalIpFromInstance(Instance instance)
        {
            if (instance == null)
            {
                return null;
            }

            return instance
                .NetworkInterfaces
                .EnsureNotNull()
                .Where(nic => nic.AccessConfigs != null)
                .SelectMany(nic => nic.AccessConfigs)
                .EnsureNotNull()
                .Where(accessConfig => accessConfig.Type == "ONE_TO_ONE_NAT")
                .Select(accessConfig => accessConfig.NatIP)
                .FirstOrDefault();
        }

        public VmInstanceNode(
            Instance instance,
            VmInstanceSettings settings,
            Action<VmInstanceSettings> saveSettings,
            ZoneNode parent)
            : base(
                  settings.InstanceName,
                  DisconnectedIconIndex,
                  settings,
                  changedSettings => saveSettings((VmInstanceSettings)changedSettings),
                  parent)
        {
            this.InstanceId = instance.Id.Value;
            this.Status = instance.Status;
            this.Hostname = instance.Hostname;
            this.MachineType = InventoryNode.ShortIdFromUrl(instance.MachineType);
            this.Tags = instance.Tags != null && instance.Tags.Items != null
                ? string.Join(", ", instance.Tags.Items) : null;
            this.InternalIp = InternalIpFromInstance(instance);
            this.ExternalIp = ExternalIpFromInstance(instance);
        }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("Name")]
        public string InstanceName => this.Text;

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("ID")]
        public ulong InstanceId { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("Status")]
        public string Status { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("Hostname")]
        public string Hostname { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("Machine type")]
        public string MachineType { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("Network tags")]
        public string Tags { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("IP address (internal)")]
        public string InternalIp { get; }

        [Browsable(true)]
        [BrowsableSetting]
        [Category("VM Instance")]
        [DisplayName("IP address (external)")]
        public string ExternalIp { get; }

        internal bool IsRunning => this.Status == "RUNNING";

        internal bool IsConnected
        {
            get => this.ImageIndex == ConnectedIconIndex;
            set => this.ImageIndex = this.SelectedImageIndex = value
                ? ConnectedIconIndex
                : DisconnectedIconIndex;
        }
    }
}
