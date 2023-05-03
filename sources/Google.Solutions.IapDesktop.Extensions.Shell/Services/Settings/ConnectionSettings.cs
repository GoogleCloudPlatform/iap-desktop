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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

#pragma warning disable CA1027 // Mark enums with FlagsAttribute

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings
{


    public abstract class ConnectionSettingsBase : IRegistrySettingsCollection
    {
        //---------------------------------------------------------------------
        // RDP settings.
        //---------------------------------------------------------------------

        public RegistryStringSetting RdpUsername { get; private set; }
        public RegistrySecureStringSetting RdpPassword { get; private set; }
        public RegistryStringSetting RdpDomain { get; private set; }
        public RegistryEnumSetting<RdpConnectionBarState> RdpConnectionBar { get; private set; }
        public RegistryEnumSetting<RdpDesktopSize> RdpDesktopSize { get; private set; }
        public RegistryEnumSetting<RdpAuthenticationLevel> RdpAuthenticationLevel { get; private set; }
        public RegistryEnumSetting<RdpColorDepth> RdpColorDepth { get; private set; }
        public RegistryEnumSetting<RdpAudioMode> RdpAudioMode { get; private set; }
        public RegistryEnumSetting<RdpUserAuthenticationBehavior> RdpUserAuthenticationBehavior { get; private set; }
        public RegistryEnumSetting<RdpBitmapPersistence> RdpBitmapPersistence { get; private set; }
        public RegistryEnumSetting<RdpNetworkLevelAuthentication> RdpNetworkLevelAuthentication { get; private set; }
        public RegistryDwordSetting RdpConnectionTimeout { get; private set; }
        public RegistryDwordSetting RdpPort { get; private set; }
        public RegistryEnumSetting<Transport.TransportType> RdpTransport { get; private set; }
        public RegistryEnumSetting<RdpRedirectClipboard> RdpRedirectClipboard { get; private set; }
        public RegistryEnumSetting<RdpRedirectPrinter> RdpRedirectPrinter { get; private set; }
        public RegistryEnumSetting<RdpRedirectSmartCard> RdpRedirectSmartCard { get; private set; }
        public RegistryEnumSetting<RdpRedirectPort> RdpRedirectPort { get; private set; }
        public RegistryEnumSetting<RdpRedirectDrive> RdpRedirectDrive { get; private set; }
        public RegistryEnumSetting<RdpRedirectDevice> RdpRedirectDevice { get; private set; }
        public RegistryEnumSetting<RdpHookWindowsKeys> RdpHookWindowsKeys { get; private set; }

        internal IEnumerable<ISetting> RdpSettings => new ISetting[]
        {
            this.RdpUsername,
            this.RdpPassword,
            this.RdpDomain,
            this.RdpConnectionBar,
            this.RdpDesktopSize,
            this.RdpAuthenticationLevel,
            this.RdpColorDepth,
            this.RdpAudioMode,
            this.RdpUserAuthenticationBehavior,
            this.RdpBitmapPersistence,
            this.RdpNetworkLevelAuthentication,
            this.RdpConnectionTimeout,
            this.RdpPort,
            this.RdpTransport,
            this.RdpRedirectClipboard,
            this.RdpRedirectPrinter,
            this.RdpRedirectSmartCard,
            this.RdpRedirectPort,
            this.RdpRedirectDrive,
            this.RdpRedirectDevice,
            this.RdpHookWindowsKeys
        };

        internal bool IsRdpSetting(ISetting setting) => this.RdpSettings.Contains(setting);

        //---------------------------------------------------------------------
        // SSH settings.
        //---------------------------------------------------------------------

        public RegistryDwordSetting SshPort { get; private set; }
        public RegistryEnumSetting<Transport.TransportType> SshTransport { get; private set; }
        public RegistryStringSetting SshUsername { get; private set; }
        public RegistryDwordSetting SshConnectionTimeout { get; private set; }

        internal IEnumerable<ISetting> SshSettings => new ISetting[]
        {
            this.SshPort,
            this.SshTransport,
            this.SshUsername,
            this.SshConnectionTimeout
        };

        internal bool IsSshSetting(ISetting setting) => this.SshSettings.Contains(setting);

        //---------------------------------------------------------------------
        // IRegistrySettingsCollection.
        //---------------------------------------------------------------------

        private static class Categories
        {
            public const string RdpCredentials = "Remote Desktop Credentials";
            public const string RdpConnection = "Remote Desktop Connection";
            public const string RdpDisplay = "Remote Desktop Display";
            public const string RdpResources = "Remote Desktop Resources";
            public const string RdpAdvanced = "Remote Desktop Specialist Settings";

            public const string SshConnection = "SSH Connection";
            public const string SshCredentials = "SSH Credentials";
        }

        public IEnumerable<ISetting> Settings => this.RdpSettings.Concat(this.SshSettings);

        protected void InitializeFromKey(RegistryKey key)
        {
            //
            // RDP Settings.
            //
            this.RdpUsername = RegistryStringSetting.FromKey(
                "Username",
                "Username",
                "Username of a local user, SAM account name of a domain user, or UPN (user@domain).",
                Categories.RdpCredentials,
                null,
                key,
                _ => true);
            this.RdpPassword = RegistrySecureStringSetting.FromKey(
                "Password",
                "Password",
                "Windows logon password.",
                Categories.RdpCredentials,
                key,
                DataProtectionScope.CurrentUser);
            this.RdpDomain = RegistryStringSetting.FromKey(
                "Domain",
                "Domain",
                "NetBIOS domain name or computer name. Leave blank when using UPN as username.",
                Categories.RdpCredentials,
                null,
                key,
                _ => true);
            this.RdpConnectionBar = RegistryEnumSetting<RdpConnectionBarState>.FromKey(
                "ConnectionBar",
                "Show connection bar",
                "Show connection bar in full-screen mode.",
                Categories.RdpDisplay,
                RdpConnectionBarState._Default,
                key);
            this.RdpDesktopSize = RegistryEnumSetting<RdpDesktopSize>.FromKey(
                "DesktopSize",
                "Desktop size",
                "Size of remote desktop.",
                Categories.RdpDisplay,
                Session.RdpDesktopSize._Default,
                key);
            this.RdpAuthenticationLevel = RegistryEnumSetting<RdpAuthenticationLevel>.FromKey(
                "AuthenticationLevel",
                "Server authentication",
                "Require server authentication when connecting.",
                Categories.RdpAdvanced,
                Session.RdpAuthenticationLevel._Default,
                key);
            this.RdpColorDepth = RegistryEnumSetting<RdpColorDepth>.FromKey(
                "ColorDepth",
                "Color depth",
                "Color depth of remote desktop.",
                Categories.RdpDisplay,
                Session.RdpColorDepth._Default,
                key);
            this.RdpAudioMode = RegistryEnumSetting<RdpAudioMode>.FromKey(
                "AudioMode",
                "Audio mode",
                "Redirect audio when playing on server.",
                Categories.RdpResources,
                Session.RdpAudioMode._Default,
                key);
            this.RdpUserAuthenticationBehavior = RegistryEnumSetting<RdpUserAuthenticationBehavior>.FromKey(
                "RdpUserAuthenticationBehavior",
                null, // Hidden.
                null, // Hidden.
                null, // Hidden.
                Session.RdpUserAuthenticationBehavior._Default,
                key);
            this.RdpBitmapPersistence = RegistryEnumSetting<RdpBitmapPersistence>.FromKey(
                "BitmapPersistence",
                "Bitmap caching",
                "Use persistent bitmap cache. Enabling caching substantially increases memory usage.",
                Categories.RdpAdvanced,
                Session.RdpBitmapPersistence._Default,
                key);
            this.RdpNetworkLevelAuthentication = RegistryEnumSetting<RdpNetworkLevelAuthentication>.FromKey(
                "NetworkLevelAuthentication",
                "Network level authentication",
                "Secure connection using network level authentication (NLA). " +
                    "Disable NLA only if the server uses a custom credential service provider." +
                    "Disabling NLA automatically enables server authentication.",
                Categories.RdpAdvanced,
                Session.RdpNetworkLevelAuthentication._Default,
                key);
            this.RdpConnectionTimeout = RegistryDwordSetting.FromKey(
                "ConnectionTimeout",
                "Connection timeout",
                "Timeout for establishing a Remote Desktop connection, in seconds. " +
                    "Use a timeout that allows sufficient time for credential prompts.",
                Categories.RdpAdvanced,
                (int)RdpSessionParameters.DefaultConnectionTimeout.TotalSeconds,
                key,
                0, 300);
            this.RdpPort = RegistryDwordSetting.FromKey(
                "RdpPort",
                "Server port",
                "Server port.",
                Categories.RdpConnection,
                RdpSessionParameters.DefaultPort,
                key,
                1,
                ushort.MaxValue);
            this.RdpTransport = RegistryEnumSetting<Transport.TransportType>.FromKey(
                "TransportType",
                "Connect via",
                $"Type of transport. Use {Transport.TransportType.IapTunnel} unless " +
                    "you need to connect to a VM's internal IP address via " +
                    "Cloud VPN or Interconnect.",
                Categories.RdpConnection,
                Transport.TransportType._Default,
                key);
            this.RdpRedirectClipboard = RegistryEnumSetting<RdpRedirectClipboard>.FromKey(
                "RedirectClipboard",
                "Redirect clipboard",
                "Allow clipboard contents to be shared with remote desktop.",
                Categories.RdpResources,
                Session.RdpRedirectClipboard._Default,
                key);
            this.RdpRedirectPrinter = RegistryEnumSetting<RdpRedirectPrinter>.FromKey(
                "RdpRedirectPrinter",
                "Redirect printers",
                "Share local printers with remote desktop.",
                Categories.RdpResources,
                Session.RdpRedirectPrinter._Default,
                key);
            this.RdpRedirectSmartCard = RegistryEnumSetting<RdpRedirectSmartCard>.FromKey(
                "RdpRedirectSmartCard",
                "Redirect smart cards",
                "Share local smart carrds with remote desktop.",
                Categories.RdpResources,
                Session.RdpRedirectSmartCard._Default,
                key);
            this.RdpRedirectPort = RegistryEnumSetting<RdpRedirectPort>.FromKey(
                "RdpRedirectPort",
                "Redirect local ports",
                "Share local ports (COM, LPT) with remote desktop.",
                Categories.RdpResources,
                Session.RdpRedirectPort._Default,
                key);
            this.RdpRedirectDrive = RegistryEnumSetting<RdpRedirectDrive>.FromKey(
                "RdpRedirectDrive",
                "Redirect drives",
                "Share local drives with remote desktop.",
                Categories.RdpResources,
                Session.RdpRedirectDrive._Default,
                key);
            this.RdpRedirectDevice = RegistryEnumSetting<RdpRedirectDevice>.FromKey(
                "RdpRedirectDevice",
                "Redirect devices",
                "Share local devices with remote desktop.",
                Categories.RdpResources,
                Session.RdpRedirectDevice._Default,
                key);
            this.RdpHookWindowsKeys = RegistryEnumSetting<RdpHookWindowsKeys>.FromKey(
                "RdpHookWindowsKeys",
                "Enable Windows shortcuts",
                "Enable Windows shortcuts (like Win+R)",
                Categories.RdpAdvanced,
                Session.RdpHookWindowsKeys._Default,
                key);

            //
            // SSH Settings.
            //
            this.SshPort = RegistryDwordSetting.FromKey(
                "SshPort",
                "Server port",
                "Server port",
                Categories.SshConnection,
                SshSessionParameters.DefaultPort,
                key,
                1,
                ushort.MaxValue);
            this.SshTransport = RegistryEnumSetting<Transport.TransportType>.FromKey(
                "TransportType",
                "Connect via",
                $"Type of transport. Use {Transport.TransportType.IapTunnel} unless " +
                    "you need to connect to a VM's internal IP address via " +
                    "Cloud VPN or Interconnect.",
                Categories.SshConnection,
                Transport.TransportType._Default,
                key);
            this.SshUsername = RegistryStringSetting.FromKey(
                "SshUsername",
                "Username",
                "Preferred Linux username (only applicable if OS Login is disabled)",
                Categories.SshCredentials,
                null,
                key,
                username => string.IsNullOrEmpty(username) ||
                            AuthorizedKeyPair.IsValidUsername(username));
            this.SshConnectionTimeout = RegistryDwordSetting.FromKey(
                "SshConnectionTimeout",
                null, // Hidden.
                null, // Hidden.
                null, // Hidden.
                (int)RdpSessionParameters.DefaultConnectionTimeout.TotalSeconds,
                key,
                0, 300);

            Debug.Assert(this.Settings.All(s => s != null));
        }

        protected static void ApplyOverlay<T>(
            T prototype,
            ConnectionSettingsBase baseSettings,
            ConnectionSettingsBase overlaySettings)
            where T : ConnectionSettingsBase
        {
            prototype.RdpUsername = (RegistryStringSetting)
                baseSettings.RdpUsername.OverlayBy(overlaySettings.RdpUsername);
            prototype.RdpPassword = (RegistrySecureStringSetting)
                baseSettings.RdpPassword.OverlayBy(overlaySettings.RdpPassword);
            prototype.RdpDomain = (RegistryStringSetting)
                baseSettings.RdpDomain.OverlayBy(overlaySettings.RdpDomain);
            prototype.RdpConnectionBar = (RegistryEnumSetting<RdpConnectionBarState>)
                baseSettings.RdpConnectionBar.OverlayBy(overlaySettings.RdpConnectionBar);
            prototype.RdpDesktopSize = (RegistryEnumSetting<RdpDesktopSize>)
                baseSettings.RdpDesktopSize.OverlayBy(overlaySettings.RdpDesktopSize);
            prototype.RdpAuthenticationLevel = (RegistryEnumSetting<RdpAuthenticationLevel>)
                baseSettings.RdpAuthenticationLevel.OverlayBy(overlaySettings.RdpAuthenticationLevel);
            prototype.RdpColorDepth = (RegistryEnumSetting<RdpColorDepth>)
                baseSettings.RdpColorDepth.OverlayBy(overlaySettings.RdpColorDepth);
            prototype.RdpAudioMode = (RegistryEnumSetting<RdpAudioMode>)
                baseSettings.RdpAudioMode.OverlayBy(overlaySettings.RdpAudioMode);
            prototype.RdpUserAuthenticationBehavior = (RegistryEnumSetting<RdpUserAuthenticationBehavior>)
                baseSettings.RdpUserAuthenticationBehavior.OverlayBy(overlaySettings.RdpUserAuthenticationBehavior);
            prototype.RdpBitmapPersistence = (RegistryEnumSetting<RdpBitmapPersistence>)
                baseSettings.RdpBitmapPersistence.OverlayBy(overlaySettings.RdpBitmapPersistence);
            prototype.RdpNetworkLevelAuthentication = (RegistryEnumSetting<RdpNetworkLevelAuthentication>)
                baseSettings.RdpNetworkLevelAuthentication.OverlayBy(overlaySettings.RdpNetworkLevelAuthentication);
            prototype.RdpConnectionTimeout = (RegistryDwordSetting)
                baseSettings.RdpConnectionTimeout.OverlayBy(overlaySettings.RdpConnectionTimeout);
            prototype.RdpPort = (RegistryDwordSetting)
                baseSettings.RdpPort.OverlayBy(overlaySettings.RdpPort);
            prototype.RdpTransport = (RegistryEnumSetting<Transport.TransportType>)
                baseSettings.RdpTransport.OverlayBy(overlaySettings.RdpTransport);
            prototype.RdpRedirectClipboard = (RegistryEnumSetting<RdpRedirectClipboard>)
                baseSettings.RdpRedirectClipboard.OverlayBy(overlaySettings.RdpRedirectClipboard);
            prototype.RdpRedirectPrinter = (RegistryEnumSetting<RdpRedirectPrinter>)
                baseSettings.RdpRedirectPrinter.OverlayBy(overlaySettings.RdpRedirectPrinter);
            prototype.RdpRedirectSmartCard = (RegistryEnumSetting<RdpRedirectSmartCard>)
                baseSettings.RdpRedirectSmartCard.OverlayBy(overlaySettings.RdpRedirectSmartCard);
            prototype.RdpRedirectPort = (RegistryEnumSetting<RdpRedirectPort>)
                baseSettings.RdpRedirectPort.OverlayBy(overlaySettings.RdpRedirectPort);
            prototype.RdpRedirectDrive = (RegistryEnumSetting<RdpRedirectDrive>)
                baseSettings.RdpRedirectDrive.OverlayBy(overlaySettings.RdpRedirectDrive);
            prototype.RdpRedirectDevice = (RegistryEnumSetting<RdpRedirectDevice>)
                baseSettings.RdpRedirectDevice.OverlayBy(overlaySettings.RdpRedirectDevice);
            prototype.RdpHookWindowsKeys = (RegistryEnumSetting<RdpHookWindowsKeys>)
                baseSettings.RdpHookWindowsKeys.OverlayBy(overlaySettings.RdpHookWindowsKeys);

            prototype.SshPort = (RegistryDwordSetting)
                baseSettings.SshPort.OverlayBy(overlaySettings.SshPort);
            prototype.SshTransport = (RegistryEnumSetting<Transport.TransportType>)
                baseSettings.SshTransport.OverlayBy(overlaySettings.SshTransport);
            prototype.SshUsername = (RegistryStringSetting)
                baseSettings.SshUsername.OverlayBy(overlaySettings.SshUsername);
            prototype.SshConnectionTimeout = (RegistryDwordSetting)
                baseSettings.SshConnectionTimeout.OverlayBy(overlaySettings.SshConnectionTimeout);

            Debug.Assert(prototype.Settings.All(s => s != null));
            Debug.Assert(baseSettings.Settings.All(s => s != null));
        }
    }

    //-------------------------------------------------------------------------
    // VM instance.
    //-------------------------------------------------------------------------

    public class InstanceConnectionSettings : ConnectionSettingsBase
    {
        public string ProjectId { get; }
        public string InstanceName { get; }

        private InstanceConnectionSettings(string projectId, string instanceName)
        {
            this.ProjectId = projectId;
            this.InstanceName = instanceName;
        }

        protected InstanceConnectionSettings ApplyDefaults(ZoneConnectionSettings zoneSettings)
        {
            var prototype = new InstanceConnectionSettings(this.ProjectId, this.InstanceName);
            ApplyOverlay(prototype, zoneSettings, this);
            return prototype;
        }

        //-------------------------------------------------------------------------
        // Create.
        //-------------------------------------------------------------------------

        public static InstanceConnectionSettings FromKey(
            string projectId,
            string instanceName,
            RegistryKey registryKey)
        {

            var settings = new InstanceConnectionSettings(projectId, instanceName);
            settings.InitializeFromKey(registryKey);
            return settings;
        }

        internal static InstanceConnectionSettings CreateNew(
            string projectId,
            string instanceName)
        {
            return FromKey(
                projectId,
                instanceName,
                null);  // Apply defaults.
        }

        internal static InstanceConnectionSettings CreateNew(InstanceLocator instance)
            => CreateNew(
                instance.ProjectId,
                instance.Name);


        //-------------------------------------------------------------------------
        // To/from URL.
        //-------------------------------------------------------------------------

        internal void ApplySettingsFromUrl(IapRdpUrl url)
        {
            Debug.Assert(this.InstanceName == url.Instance.Name);
            Debug.Assert(this.ProjectId == url.Instance.ProjectId);

            //
            // NB. Ignore passwords in URLs.
            //
            foreach (var setting in this.Settings
                .Where(s => !(s is RegistrySecureStringSetting)))
            {
                var value = url.Parameters.Get(setting.Key);
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        setting.Value = value;
                    }
                    catch (Exception)
                    {
                        // Ignore, keeping the previous value.
                    }
                }
            }
        }

        public static InstanceConnectionSettings FromUrl(IapRdpUrl url)
        {
            var settings = CreateNew(
                url.Instance.ProjectId,
                url.Instance.Name);

            settings.ApplySettingsFromUrl(url);

            return settings;
        }

        public NameValueCollection ToUrlQuery()
        {
            // NB. Do not allow passwords to leak into URLs.
            var collection = new NameValueCollection();
            foreach (var setting in this.Settings
                .Where(s => !(s is RegistrySecureStringSetting))
                .Where(s => !s.IsDefault))
            {
                if (setting.Value is Enum enumValue)
                {
                    // Use numeric value instead of symbol because
                    // the numeric value is stable.
                    collection.Add(setting.Key, ((int)setting.Value).ToString());
                }
                else
                {
                    collection.Add(setting.Key, setting.Value.ToString());
                }
            }

            return collection;
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

        public InstanceConnectionSettings OverlayBy(InstanceConnectionSettings instanceSettings)
        {
            var result = InstanceConnectionSettings.CreateNew(
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
