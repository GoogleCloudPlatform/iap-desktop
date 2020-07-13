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
using Google.Solutions.IapDesktop.Application.Util;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;

#pragma warning disable IDE1006 // Underscores in names.
#pragma warning disable IDE1027// Mark as flags.

namespace Google.Solutions.IapDesktop.Application.Services.Persistence
{
    /// <summary>
    /// Registry-backed repository for connection settings.
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
    [ComVisible(false)]
    public class ConnectionSettingsRepository : SettingsRepositoryBase<ConnectionSettings>
    {
        private const string ZonePrefix = "zone-";
        private const string VmPrefix = "vm-";

        public ConnectionSettingsRepository(RegistryKey baseKey) : base(baseKey)
        {
            Utilities.ThrowIfNull(baseKey, nameof(baseKey));
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        public IEnumerable<ProjectConnectionSettings> ListProjectSettings()
        {
            foreach (var projectId in this.baseKey.GetSubKeyNames())
            {
                yield return GetProjectSettings(projectId);
            }
        }

        public ProjectConnectionSettings GetProjectSettings(string projectId)
        {
            var settings = Get<ProjectConnectionSettings>(new[] { projectId });
            settings.ProjectId = projectId;
            return settings;
        }

        public void SetProjectSettings(ProjectConnectionSettings settings)
        {
            Set<ProjectConnectionSettings>(new[] { settings.ProjectId }, settings);
        }

        public void DeleteProjectSettings(string projectId)
        {
            this.baseKey.DeleteSubKeyTree(projectId, false);
        }


        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        public ZoneConnectionSettings GetZoneSettings(string projectId, string zoneId)
        {
            var settings = Get<ZoneConnectionSettings>(new[] { projectId, ZonePrefix + zoneId });
            settings.ZoneId = zoneId;
            return settings;
        }

        public void SetZoneSettings(string projectId, ZoneConnectionSettings settings)
        {
            Set<ZoneConnectionSettings>(new[] { projectId, ZonePrefix + settings.ZoneId }, settings);
        }


        //---------------------------------------------------------------------
        // Virtual Machines.
        //---------------------------------------------------------------------

        public VmInstanceConnectionSettings GetVmInstanceSettings(string projectId, string instanceName)
        {
            var settings = Get<VmInstanceConnectionSettings>(new[] { projectId, VmPrefix + instanceName });
            settings.InstanceName = instanceName;
            return settings;
        }

        public void SetVmInstanceSettings(string projectId, VmInstanceConnectionSettings settings)
        {
            Set<VmInstanceConnectionSettings>(new[] { projectId, VmPrefix + settings.InstanceName }, settings);
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
        AutoAdjust = 2,

        [Browsable(false)]
        _Default = AutoAdjust
    }

    public enum RdpAuthenticationLevel
    {
        // Likely to fail when using IAP unless the cert has been issued
        // for "localhost".
        AttemptServerAuthentication = 0,

        // Almsot guaranteed to fail, so do not even display it.
        [Browsable(false)]
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

    public enum RdpBitmapPersistence
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpCredentialGenerationBehavior
    {
        Always,
        Prompt,
        Disable,

        [Browsable(false)]
        _Default = Prompt
    }

    public abstract class ConnectionSettingsBase
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

        public RdpBitmapPersistence BitmapPersistence { get; set; }
            = RdpBitmapPersistence._Default;

        [DwordRegistryValueAttribute("BitmapPersistence")]
        protected int? _BitmapPersistence
        {
            get => (int)this.BitmapPersistence;
            set => this.BitmapPersistence = value != null
                ? (RdpBitmapPersistence)value
                : RdpBitmapPersistence._Default;
        }

        [DwordRegistryValueAttribute("ConnectionTimeout")]
        public int ConnectionTimeout { get; set; } = 30;

        public RdpCredentialGenerationBehavior CredentialGenerationBehavior { get; set; }
            = RdpCredentialGenerationBehavior._Default;

        [DwordRegistryValueAttribute("CredentialGenerationBehavior")]
        protected int? _CredentialGenerationBehavior
        {
            get => (int)this.CredentialGenerationBehavior;
            set => this.CredentialGenerationBehavior = value != null
                ? (RdpCredentialGenerationBehavior)value
                : RdpCredentialGenerationBehavior._Default;
        }
    }

    public class VmInstanceConnectionSettings : ConnectionSettingsBase
    {
        public string InstanceName { get; set; }
    }

    public class ZoneConnectionSettings : ConnectionSettingsBase
    {
        public string ZoneId { get; set; }
    }

    public class ProjectConnectionSettings : ConnectionSettingsBase
    {
        public string ProjectId { get; set; }
    }

    public class ConnectionSettings : ConnectionSettingsBase
    {
    }
}
