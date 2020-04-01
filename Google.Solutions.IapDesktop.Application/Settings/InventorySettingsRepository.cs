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

using Google.Apis.Util;
using Google.Solutions.IapDesktop.Application.Registry;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    /// <summary>
    /// Registry-backed repository for GCE inventory settings.
    /// 
    /// To simplify key managent, a flat structure is used:
    /// 
    /// [base-key]
    ///    + [project-id]           => values...
    ///      + region-[region-id]   => values...
    ///      + zone-[zone-id]       => values...
    ///      + vm-[instance-name]   => values...
    ///      
    /// </summary>
    public class InventorySettingsRepository : SettingsRepositoryBase<InventorySettings>
    {
        private const string RegionPrefix = "region-";
        private const string ZonePrefix = "zone-";
        private const string VmPrefix = "vm-";

        public InventorySettingsRepository(RegistryKey baseKey) : base(baseKey)
        {
            Utilities.ThrowIfNull(baseKey, nameof(baseKey));
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        public IEnumerable<ProjectSettings> ListProjectSettings()
        {
            foreach (var projectId in this.baseKey.GetSubKeyNames())
            {
                yield return GetProjectSettings(projectId);
            }
        }

        public ProjectSettings GetProjectSettings(string projectId)
        {
            var settings = Get<ProjectSettings>(new[] { projectId });
            settings.ProjectId = projectId;
            return settings;
        }

        public void SetProjectSettings(ProjectSettings settings)
        {
            Set<ProjectSettings>(new[] { settings.ProjectId }, settings);
        }

        public void DeleteProjectSettings(string projectId)
        {
            this.baseKey.DeleteSubKeyTree(projectId, false);
        }


        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        public ZoneSettings GetZoneSettings(string projectId, string zoneId)
        {
            var settings = Get<ZoneSettings>(new[] { projectId, ZonePrefix + zoneId });
            settings.ZoneId = zoneId;
            return settings;
        }

        public void SetZoneSettings(string projectId, ZoneSettings settings)
        {
            Set<ZoneSettings>(new[] { projectId, ZonePrefix + settings.ZoneId }, settings);
        }


        //---------------------------------------------------------------------
        // Virtual Machines.
        //---------------------------------------------------------------------

        public VmInstanceSettings GetVmInstanceSettings(string projectId, string instanceName)
        {
            var settings = Get<VmInstanceSettings>(new[] { projectId, VmPrefix + instanceName });
            settings.InstanceName = instanceName;
            return settings;
        }

        public void SetVmInstanceSettings(string projectId, VmInstanceSettings settings)
        {
            Set<VmInstanceSettings>(new[] { projectId, VmPrefix + settings.InstanceName }, settings);
        }
    }


    //
    // NB. The values do not map to RDP interface values. But the numeric values
    // must be kept unchanged as they are persisted in the registry.
    //

    public enum RdpConnectionBarState
    {
        AutoHide = 0,
        Pinned = 1,
        Off = 2,

        [Browsable(false)]
        _Default = AutoHide
    }

    public enum RdpDesktopSize
    {
        ClientSize = 0,
        ScreenSize = 1,

        [Browsable(false)]
        _Default = ClientSize
    }

    public enum RdpAuthenticationLevel
    {
        AttemptServerAuthentication = 0,
        RequireServerAuthentication = 1,
        NoServerAuthentication = 3,

        [Browsable(false)]
        _Default = NoServerAuthentication
    }

    public enum RdpColorDepth
    {
        HighColor = 0,
        TrueColor = 1,
        DeepColor = 2,

        [Browsable(false)]
        _Default = TrueColor
    }

    public enum RdpAudioMode
    {
        PlayLocally = 0,
        PlayOnServer = 1,
        DoNotPlay = 2,

        [Browsable(false)]
        _Default = PlayLocally
    }

    public enum RdpRedirectClipboard
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Enabled
    }

    public enum RdpUserAuthenticationBehavior
    {
        PromptOnFailure = 0,
        AbortOnFailure = 1,

        [Browsable(false)]
        _Default = PromptOnFailure
    }

    public abstract class InventorySettingsBase
    {
        //---------------------------------------------------------------------
        // Credentials.
        //---------------------------------------------------------------------

        [StringRegistryValue("Username")]
        public string Username { get; set; }

        [SecureStringRegistryValue("Password", DataProtectionScope.CurrentUser)]
        public SecureString Password { get; set; }

        [StringRegistryValue("Domain")]
        public string Domain { get; set; }

        public RdpConnectionBarState ConnectionBar { get; set; }
            = RdpConnectionBarState.AutoHide;

        [DwordRegistryValueAttribute("ConnectionBar")]
        protected int? _ConnectionBar
        {
            get => (int)this.ConnectionBar;
            set => this.ConnectionBar = value != null
                ? (RdpConnectionBarState)value
                : RdpConnectionBarState._Default;
        }

        public RdpDesktopSize DesktopSize { get; set; }
            = RdpDesktopSize._Default;

        [DwordRegistryValueAttribute("DesktopSize")]
        protected int? _DesktopSize
        {
            get => (int)this.DesktopSize;
            set => this.DesktopSize = value != null
                ? (RdpDesktopSize)value
                : RdpDesktopSize._Default;
        }

        public RdpAuthenticationLevel AuthenticationLevel { get; set; }
            = RdpAuthenticationLevel._Default;

        [DwordRegistryValueAttribute("AuthenticationLevel")]
        protected int? _AuthenticationLevel
        {
            get => (int)this.AuthenticationLevel;
            set => this.AuthenticationLevel = value != null
                ? (RdpAuthenticationLevel)value
                : RdpAuthenticationLevel._Default;
        }

        public RdpColorDepth ColorDepth { get; set; }
            = RdpColorDepth._Default;

        [DwordRegistryValueAttribute("ColorDepth")]
        protected int? _ColorDepth
        {
            get => (int)this.ColorDepth;
            set => this.ColorDepth = value != null
                ? (RdpColorDepth)value
                : RdpColorDepth._Default;
        }

        public RdpAudioMode AudioMode { get; set; }
            = RdpAudioMode._Default;

        [DwordRegistryValueAttribute("AudioMode")]
        protected int? _AudioMode
        {
            get => (int)this.AudioMode;
            set => this.AudioMode = value != null
                ? (RdpAudioMode)value
                : RdpAudioMode._Default;
        }

        public RdpRedirectClipboard RedirectClipboard { get; set; }
            = RdpRedirectClipboard._Default;

        [DwordRegistryValueAttribute("RedirectClipboard")]
        protected int? _RedirectClipboard
        {
            get => (int)this.RedirectClipboard;
            set => this.RedirectClipboard = value != null
                ? (RdpRedirectClipboard)value
                : RdpRedirectClipboard._Default;
        }

        public RdpUserAuthenticationBehavior UserAuthenticationBehavior { get; set; }
            = RdpUserAuthenticationBehavior._Default;
    }

    public class VmInstanceSettings : InventorySettingsBase
    {
        public string InstanceName { get; set; }
    }

    public class ZoneSettings : InventorySettingsBase
    {
        public string ZoneId { get; set; }
    }

    public class ProjectSettings : InventorySettingsBase
    {
        public string ProjectId { get; set; }
    }

    public class InventorySettings : InventorySettingsBase
    {
    }
}
