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

using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Settings
{
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
        Allow = 0,
        AllowIfNoCredentialsFound = 1,
        Disallow = 2,
        Force = 3,

        [Browsable(false)]
        _Default = AllowIfNoCredentialsFound
    }

    public abstract class ConnectionSettingsBase : IRegistrySettingsCollection
    {
        public RegistryStringSetting Username { get; private set; }
        public RegistrySecureStringSetting Password { get; private set; }
        public RegistryStringSetting Domain { get; private set; }
        public RegistryEnumSetting<RdpConnectionBarState> ConnectionBar { get; private set; }
        public RegistryEnumSetting<RdpDesktopSize> DesktopSize { get; private set; }
        public RegistryEnumSetting<RdpAuthenticationLevel> AuthenticationLevel { get; private set; }
        public RegistryEnumSetting<RdpColorDepth> ColorDepth { get; private set; }
        public RegistryEnumSetting<RdpAudioMode> AudioMode { get; private set; }
        public RegistryEnumSetting<RdpRedirectClipboard> RedirectClipboard { get; private set; }
        public RegistryEnumSetting<RdpUserAuthenticationBehavior> UserAuthenticationBehavior { get; private set; }
        public RegistryEnumSetting<RdpBitmapPersistence> BitmapPersistence { get; private set; }
        public RegistryDwordSetting ConnectionTimeout { get; private set; }
        public RegistryEnumSetting<RdpCredentialGenerationBehavior> CredentialGenerationBehavior { get; private set; }

        public IEnumerable<ISetting> Settings => new ISetting[]
        {
            this.Username,
            this.Password,
            this.Domain,
            this.ConnectionBar,
            this.DesktopSize,
            this.AuthenticationLevel,
            this.ColorDepth,
            this.AudioMode,
            this.RedirectClipboard,
            this.UserAuthenticationBehavior,
            this.BitmapPersistence,
            this.ConnectionTimeout,
            this.CredentialGenerationBehavior,
        };

        protected void InitializeFromKey(RegistryKey key)
        {
            this.Username = RegistryStringSetting.FromKey(
                "Username",
                "Username",
                "Windows logon username",
                "Credentials",
                null,
                key,
                _ => true);
            this.Password = RegistrySecureStringSetting.FromKey(
                "Password",
                "Password",
                "Windows logon password",
                "Credentials",
                key,
                DataProtectionScope.CurrentUser);
            this.Domain = RegistryStringSetting.FromKey(
                "Domain",
                "Domain",
                "Windows logon domain",
                "Credentials",
                null,
                key,
                _ => true);
            this.ConnectionBar = RegistryEnumSetting<RdpConnectionBarState>.FromKey(
                "ConnectionBar",
                "Show connection bar",
                "Show connection bar in full-screen mode",
                "Display",
                RdpConnectionBarState._Default,
                key);
            this.DesktopSize = RegistryEnumSetting<RdpDesktopSize>.FromKey(
                "DesktopSize",
                "Desktop size",
                "Size of remote desktop",
                "Display",
                RdpDesktopSize._Default,
                key);
            this.AuthenticationLevel = RegistryEnumSetting<RdpAuthenticationLevel>.FromKey(
                "AuthenticationLevel",
                "Server authentication",
                "Require server authentication when connecting",
                "Connection",
                RdpAuthenticationLevel._Default,
                key);
            this.ColorDepth = RegistryEnumSetting<RdpColorDepth>.FromKey(
                "ColorDepth",
                "Color depth",
                "Color depth of remote desktop",
                "Display",
                RdpColorDepth._Default,
                key);
            this.AudioMode = RegistryEnumSetting<RdpAudioMode>.FromKey(
                "AudioMode",
                "Audio mode",
                "Redirect audio when playing on server",
                "Local resources",
                RdpAudioMode._Default,
                key);
            this.RedirectClipboard = RegistryEnumSetting<RdpRedirectClipboard>.FromKey(
                "RedirectClipboard",
                "Redirect clipboard",
                "Allow clipboard contents to be shared with remote desktop",
                "Local resources",
                RdpRedirectClipboard._Default,
                key);
            this.UserAuthenticationBehavior = RegistryEnumSetting<RdpUserAuthenticationBehavior>.FromKey(
                "RdpUserAuthenticationBehavior",
                null, // Hidden.
                null, // Hidden.
                null, // Hidden.
                RdpUserAuthenticationBehavior._Default,
                key);
            this.BitmapPersistence = RegistryEnumSetting<RdpBitmapPersistence>.FromKey(
                "BitmapPersistence",
                "Bitmap caching",
                "Use persistent bitmap cache",
                "Performance",
                RdpBitmapPersistence._Default,
                key);
            this.ConnectionTimeout = RegistryDwordSetting.FromKey(
                "ConnectionTimeout",
                null, // Hidden.
                null, // Hidden.
                null, // Hidden.
                30,
                key,
                0, 300);
            this.CredentialGenerationBehavior = RegistryEnumSetting<RdpCredentialGenerationBehavior>.FromKey(
                "CredentialGenerationBehavior",
                null, // Hidden.
                null, // Hidden.
                null, // Hidden.
                RdpCredentialGenerationBehavior._Default,
                key);

            Debug.Assert(this.Settings.All(s => s != null));
        }

        protected static void ApplyOverlay<T>(
            T prototype,
            ConnectionSettingsBase baseSettings,
            ConnectionSettingsBase overlaySettings)
            where T : ConnectionSettingsBase
        {
            prototype.Username = (RegistryStringSetting)
                baseSettings.Username.OverlayBy(overlaySettings.Username);
            prototype.Password = (RegistrySecureStringSetting)
                baseSettings.Password.OverlayBy(overlaySettings.Password);
            prototype.Domain = (RegistryStringSetting)
                baseSettings.Domain.OverlayBy(overlaySettings.Domain);
            prototype.ConnectionBar = (RegistryEnumSetting<RdpConnectionBarState>)
                baseSettings.ConnectionBar.OverlayBy(overlaySettings.ConnectionBar);
            prototype.DesktopSize = (RegistryEnumSetting<RdpDesktopSize>)
                baseSettings.DesktopSize.OverlayBy(overlaySettings.DesktopSize);
            prototype.AuthenticationLevel = (RegistryEnumSetting<RdpAuthenticationLevel>)
                baseSettings.AuthenticationLevel.OverlayBy(overlaySettings.AuthenticationLevel);
            prototype.ColorDepth = (RegistryEnumSetting<RdpColorDepth>)
                baseSettings.ColorDepth.OverlayBy(overlaySettings.ColorDepth);
            prototype.AudioMode = (RegistryEnumSetting<RdpAudioMode>)
                baseSettings.AudioMode.OverlayBy(overlaySettings.AudioMode);
            prototype.RedirectClipboard = (RegistryEnumSetting<RdpRedirectClipboard>)
                baseSettings.RedirectClipboard.OverlayBy(overlaySettings.RedirectClipboard);
            prototype.UserAuthenticationBehavior = (RegistryEnumSetting<RdpUserAuthenticationBehavior>)
                baseSettings.UserAuthenticationBehavior.OverlayBy(overlaySettings.UserAuthenticationBehavior);
            prototype.BitmapPersistence = (RegistryEnumSetting<RdpBitmapPersistence>)
                baseSettings.BitmapPersistence.OverlayBy(overlaySettings.BitmapPersistence);
            prototype.ConnectionTimeout = (RegistryDwordSetting)
                baseSettings.ConnectionTimeout.OverlayBy(overlaySettings.ConnectionTimeout);
            prototype.CredentialGenerationBehavior = (RegistryEnumSetting<RdpCredentialGenerationBehavior>)
                baseSettings.CredentialGenerationBehavior.OverlayBy(overlaySettings.CredentialGenerationBehavior);

            Debug.Assert(baseSettings.Settings.All(s => s != null));
        }
    }

    //-------------------------------------------------------------------------
    // VM instance.
    //-------------------------------------------------------------------------

    public class VmInstanceConnectionSettings : ConnectionSettingsBase
    {
        public string ProjectId { get; }
        public string InstanceName { get; }

        private VmInstanceConnectionSettings(string projectId, string instanceName)
        {
            this.ProjectId = projectId;
            this.InstanceName = instanceName;
        }

        public static VmInstanceConnectionSettings FromKey(
            string projectId,
            string instanceName,
            RegistryKey registryKey)
        {

            var settings = new VmInstanceConnectionSettings(projectId, instanceName);
            settings.InitializeFromKey(registryKey);
            return settings;
        }

        public static VmInstanceConnectionSettings CreateNew(
            string projectId,
            string instanceName)
        {
            return FromKey(
                projectId,
                instanceName,
                null);  // Apply defaults.
        }

        public static VmInstanceConnectionSettings FromUrl(IapRdpUrl url)
        {
            var settings = CreateNew(
                url.Instance.ProjectId,
                url.Instance.Name);

            settings.ApplyValues(
                url.Parameters,
                true);

            return settings;
        }

        public VmInstanceConnectionSettings ApplyDefaults(ZoneConnectionSettings zoneSettings)
        {
            var prototype = new VmInstanceConnectionSettings(this.ProjectId, this.InstanceName);
            ApplyOverlay(prototype, zoneSettings, this);
            return prototype;
        }
    }

    //-------------------------------------------------------------------------
    // Zone.
    //-------------------------------------------------------------------------

    public class ZoneConnectionSettings : ConnectionSettingsBase
    {
        public string ProjectId { get; }
        public string ZoneId { get; }

        private ZoneConnectionSettings(string projectId, string zoneId)
        {
            this.ProjectId = projectId;
            this.ZoneId = zoneId;
        }

        public static ZoneConnectionSettings FromKey(
            string projectId,
            string zoneId,
            RegistryKey registryKey)
        {

            var settings = new ZoneConnectionSettings(projectId, zoneId);
            settings.InitializeFromKey(registryKey);
            return settings;
        }

        public static ZoneConnectionSettings CreateNew(
            string projectId,
            string zoneId)
        {
            return FromKey(
                projectId,
                zoneId,
                null);  // Apply defaults.
        }

        public VmInstanceConnectionSettings OverlayBy(VmInstanceConnectionSettings instanceSettings)
        {
            var result = VmInstanceConnectionSettings.CreateNew(
                instanceSettings.ProjectId,
                instanceSettings.InstanceName);
            ApplyOverlay(result, this, instanceSettings);
            return result;
        }
    }

    //-------------------------------------------------------------------------
    // Project.
    //-------------------------------------------------------------------------

    public class ProjectConnectionSettings : ConnectionSettingsBase
    {
        public string ProjectId { get; }

        private ProjectConnectionSettings(string projectId)
        {
            this.ProjectId = projectId;
        }

        public static ProjectConnectionSettings FromKey(
            string ProjectId,
            RegistryKey registryKey)
        {

            var settings = new ProjectConnectionSettings(ProjectId);
            settings.InitializeFromKey(registryKey);
            return settings;
        }

        public ZoneConnectionSettings OverlayBy(ZoneConnectionSettings zoneSettings)
        {
            var result = ZoneConnectionSettings.CreateNew(
                zoneSettings.ProjectId,
                zoneSettings.ZoneId);
            ApplyOverlay(result, this, zoneSettings);
            return result;
        }
    }
}
