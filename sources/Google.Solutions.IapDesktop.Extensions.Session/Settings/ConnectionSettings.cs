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
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Profile.Settings.Registry;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;

#pragma warning disable CA1027 // Mark enums with FlagsAttribute

namespace Google.Solutions.IapDesktop.Extensions.Session.Settings
{
    public abstract class ConnectionSettingsBase : ISettingsCollection
    {
        private static class Categories
        {
            private const ushort MaxIndex = 7;

            private static string Order(ushort order, string name)
            {
                //
                // The PropertyGrid control doesn't let us explicitly specify the
                // order of categories. To work around that limitation, prefix 
                // category names with zero-width spaces so that alphabetical 
                // sorting yields the desired result.
                //

                Debug.Assert(order <= MaxIndex);
                return new string('\u200B', MaxIndex - order) + name;
            }

            public static readonly string WindowsCredentials = Order(0, "Windows Credentials");

            public static readonly string RdpConnection = Order(1, "Remote Desktop Connection");
            public static readonly string RdpDisplay = Order(2, "Remote Desktop Display");
            public static readonly string RdpResources = Order(3, "Remote Desktop Resources");
            public static readonly string RdpSecurity = Order(4, "Remote Desktop Security Settings");

            public static readonly string SshConnection = Order(5, "SSH Connection");
            public static readonly string SshCredentials = Order(6, "SSH Credentials");

            public static readonly string AppCredentials = Order(7, "SQL Server");
        }

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
        public RegistryEnumSetting<SessionTransportType> RdpTransport { get; private set; }
        public RegistryEnumSetting<RdpRedirectClipboard> RdpRedirectClipboard { get; private set; }
        public RegistryEnumSetting<RdpRedirectPrinter> RdpRedirectPrinter { get; private set; }
        public RegistryEnumSetting<RdpRedirectSmartCard> RdpRedirectSmartCard { get; private set; }
        public RegistryEnumSetting<RdpRedirectPort> RdpRedirectPort { get; private set; }
        public RegistryEnumSetting<RdpRedirectDrive> RdpRedirectDrive { get; private set; }
        public RegistryEnumSetting<RdpRedirectDevice> RdpRedirectDevice { get; private set; }
        public RegistryEnumSetting<RdpHookWindowsKeys> RdpHookWindowsKeys { get; private set; }

        internal IEnumerable<ISetting> RdpSettings => new ISetting[]
        {
            //
            // NB. The order determines the default order in the PropertyGrid
            // (assuming the PropertyGrid doesn't force alphabetical order).
            //
            this.RdpTransport,
            this.RdpConnectionTimeout,
            this.RdpPort,
            
            this.RdpUsername,
            this.RdpPassword,
            this.RdpDomain,

            this.RdpBitmapPersistence,
            this.RdpColorDepth,
            this.RdpDesktopSize,
            this.RdpConnectionBar,

            this.RdpAudioMode,
            this.RdpHookWindowsKeys,
            this.RdpRedirectClipboard,
            this.RdpRedirectPrinter,
            this.RdpRedirectSmartCard,
            this.RdpRedirectPort,
            this.RdpRedirectDrive,
            this.RdpRedirectDevice,

            this.RdpUserAuthenticationBehavior,
            this.RdpNetworkLevelAuthentication,
            this.RdpAuthenticationLevel,
        };

        //---------------------------------------------------------------------
        // SSH settings.
        //---------------------------------------------------------------------

        public RegistryDwordSetting SshPort { get; private set; }
        public RegistryEnumSetting<SessionTransportType> SshTransport { get; private set; }
        public RegistryStringSetting SshUsername { get; private set; }
        public RegistrySecureStringSetting SshPassword { get; private set; }
        public RegistryDwordSetting SshConnectionTimeout { get; private set; }
        public RegistryEnumSetting<SshCredentialType> SshCredentialType { get; private set; }

        internal IEnumerable<ISetting> SshSettings => new ISetting[]
        {
            //
            // NB. The order determines the default order in the PropertyGrid
            // (assuming the PropertyGrid doesn't force alphabetical order).
            //
            this.SshTransport,
            this.SshConnectionTimeout,
            this.SshPort,
            this.SshCredentialType,
            this.SshUsername,
            this.SshPassword,
        };

        //---------------------------------------------------------------------
        // App settings.
        //---------------------------------------------------------------------

        public RegistryStringSetting AppUsername { get; private set; }
        public RegistryEnumSetting<AppNetworkLevelAuthenticationState> AppNetworkLevelAuthentication { get; private set; }

        internal IEnumerable<ISetting> AppSettings => new ISetting[]
        {
            //
            // NB. The order determines the default order in the PropertyGrid
            // (assuming the PropertyGrid doesn't force alphabetical order).
            //
            this.AppUsername,
            this.AppNetworkLevelAuthentication
        };

        //---------------------------------------------------------------------
        // Filtering.
        //---------------------------------------------------------------------

        internal bool AppliesTo(
            ISetting setting,
            IProjectModelInstanceNode node)
        {
            if (this.SshSettings.Contains(setting))
            {
                return node.IsSshSupported();
            }
            else if (this.RdpSettings.Contains(setting))
            {
                return node.IsRdpSupported();
            }
            else
            {
                return true;
            }
        }

        //---------------------------------------------------------------------
        // IRegistrySettingsCollection.
        //---------------------------------------------------------------------

        public IEnumerable<ISetting> Settings
        {
            get => this.RdpSettings
                .Concat(this.SshSettings)
                .Concat(this.AppSettings);
        }

        protected void InitializeFromKey(RegistryKey key)
        {
            //
            // RDP Settings.
            //
            this.RdpUsername = RegistryStringSetting.FromKey(
                "Username",
                "Username",
                "Username of a local user, SAM account name of a domain user, or UPN (user@domain).",
                Categories.WindowsCredentials,
                null,
                key,
                _ => true);
            this.RdpPassword = RegistrySecureStringSetting.FromKey(
                "Password",
                "Password",
                "Windows logon password.",
                Categories.WindowsCredentials,
                key,
                DataProtectionScope.CurrentUser);
            this.RdpDomain = RegistryStringSetting.FromKey(
                "Domain",
                "Domain",
                "NetBIOS domain name or computer name. Leave blank when using UPN as username.",
                Categories.WindowsCredentials,
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
                Protocol.Rdp.RdpDesktopSize._Default,
                key);
            this.RdpAuthenticationLevel = RegistryEnumSetting<RdpAuthenticationLevel>.FromKey(
                "AuthenticationLevel",
                "Server authentication",
                "Require server authentication when connecting.",
                Categories.RdpSecurity,
                Protocol.Rdp.RdpAuthenticationLevel._Default,
                key);
            this.RdpColorDepth = RegistryEnumSetting<RdpColorDepth>.FromKey(
                "ColorDepth",
                "Color depth",
                "Color depth of remote desktop.",
                Categories.RdpDisplay,
                Protocol.Rdp.RdpColorDepth._Default,
                key);
            this.RdpAudioMode = RegistryEnumSetting<RdpAudioMode>.FromKey(
                "AudioMode",
                "Audio mode",
                "Redirect audio when playing on server.",
                Categories.RdpResources,
                Protocol.Rdp.RdpAudioMode._Default,
                key);
            this.RdpUserAuthenticationBehavior = RegistryEnumSetting<RdpUserAuthenticationBehavior>.FromKey(
                "RdpUserAuthenticationBehavior",
                null, // Hidden.
                null, // Hidden.
                null, // Hidden.
                Protocol.Rdp.RdpUserAuthenticationBehavior._Default,
                key);
            this.RdpBitmapPersistence = RegistryEnumSetting<RdpBitmapPersistence>.FromKey(
                "BitmapPersistence",
                "Bitmap caching",
                "Use persistent bitmap cache. Enabling caching substantially increases memory usage.",
                Categories.RdpDisplay,
                Protocol.Rdp.RdpBitmapPersistence._Default,
                key);
            this.RdpNetworkLevelAuthentication = RegistryEnumSetting<RdpNetworkLevelAuthentication>.FromKey(
                "NetworkLevelAuthentication",
                "Network level authentication",
                "Secure connection using network level authentication (NLA). " +
                    "Disable NLA only if the server uses a custom credential service provider." +
                    "Disabling NLA automatically enables server authentication.",
                Categories.RdpSecurity,
                Protocol.Rdp.RdpNetworkLevelAuthentication._Default,
                key);
            this.RdpConnectionTimeout = RegistryDwordSetting.FromKey(
                "ConnectionTimeout",
                "Connection timeout",
                "Timeout for establishing a Remote Desktop connection, in seconds. " +
                    "Use a timeout that allows sufficient time for credential prompts.",
                Categories.RdpConnection,
                (int)RdpParameters.DefaultConnectionTimeout.TotalSeconds,
                key,
                0, 300);
            this.RdpPort = RegistryDwordSetting.FromKey(
                "RdpPort",
                "Server port",
                "Server port.",
                Categories.RdpConnection,
                RdpParameters.DefaultPort,
                key,
                1,
                ushort.MaxValue);
            this.RdpTransport = RegistryEnumSetting<SessionTransportType>.FromKey(
                "TransportType",
                "Connect via",
                $"Type of transport. Use {SessionTransportType.IapTunnel} unless " +
                    "you need to connect to a VM's internal IP address via " +
                    "Cloud VPN or Interconnect.",
                Categories.RdpConnection,
                SessionTransportType._Default,
                key);
            this.RdpRedirectClipboard = RegistryEnumSetting<RdpRedirectClipboard>.FromKey(
                "RedirectClipboard",
                "Redirect clipboard",
                "Allow clipboard contents to be shared with remote desktop.",
                Categories.RdpResources,
                Protocol.Rdp.RdpRedirectClipboard._Default,
                key);
            this.RdpRedirectPrinter = RegistryEnumSetting<RdpRedirectPrinter>.FromKey(
                "RdpRedirectPrinter",
                "Redirect printers",
                "Share local printers with remote desktop.",
                Categories.RdpResources,
                Protocol.Rdp.RdpRedirectPrinter._Default,
                key);
            this.RdpRedirectSmartCard = RegistryEnumSetting<RdpRedirectSmartCard>.FromKey(
                "RdpRedirectSmartCard",
                "Redirect smart cards",
                "Share local smart carrds with remote desktop.",
                Categories.RdpResources,
                Protocol.Rdp.RdpRedirectSmartCard._Default,
                key);
            this.RdpRedirectPort = RegistryEnumSetting<RdpRedirectPort>.FromKey(
                "RdpRedirectPort",
                "Redirect local ports",
                "Share local ports (COM, LPT) with remote desktop.",
                Categories.RdpResources,
                Protocol.Rdp.RdpRedirectPort._Default,
                key);
            this.RdpRedirectDrive = RegistryEnumSetting<RdpRedirectDrive>.FromKey(
                "RdpRedirectDrive",
                "Redirect drives",
                "Share local drives with remote desktop.",
                Categories.RdpResources,
                Protocol.Rdp.RdpRedirectDrive._Default,
                key);
            this.RdpRedirectDevice = RegistryEnumSetting<RdpRedirectDevice>.FromKey(
                "RdpRedirectDevice",
                "Redirect devices",
                "Share local devices with remote desktop.",
                Categories.RdpResources,
                Protocol.Rdp.RdpRedirectDevice._Default,
                key);
            this.RdpHookWindowsKeys = RegistryEnumSetting<RdpHookWindowsKeys>.FromKey(
                "RdpHookWindowsKeys",
                "Enable Windows shortcuts",
                "Enable Windows shortcuts (like Win+R)",
                Categories.RdpResources,
                Protocol.Rdp.RdpHookWindowsKeys._Default,
                key);

            //
            // SSH Settings.
            //
            this.SshPort = RegistryDwordSetting.FromKey(
                "SshPort",
                "Server port",
                "Server port",
                Categories.SshConnection,
                SshParameters.DefaultPort,
                key,
                1,
                ushort.MaxValue);
            this.SshTransport = RegistryEnumSetting<SessionTransportType>.FromKey(
                "TransportType",
                "Connect via",
                $"Type of transport. Use {SessionTransportType.IapTunnel} unless " +
                    "you need to connect to a VM's internal IP address via " +
                    "Cloud VPN or Interconnect.",
                Categories.SshConnection,
                SessionTransportType._Default,
                key);
            this.SshCredentialType = RegistryEnumSetting<SshCredentialType>.FromKey(
                "SshCredentialType",
                "Type",
                "Type of credential to use. Use PublicKey to let IAP Desktop automatically " +
                    "create an SSH key pair and publish it using OS Login or metadata keys. " +
                    "Use Password to use password/keyboard-interactive authentication instead.",
                Categories.SshCredentials,
                Protocol.Ssh.SshCredentialType._Default,
                key);
            this.SshUsername = RegistryStringSetting.FromKey(
                "SshUsername",
                "Username",
                "Linux username, only applicable if OS Login is disabled",
                Categories.SshCredentials,
                null,
                key,
                username => string.IsNullOrEmpty(username) ||
                            LinuxUser.IsValidUsername(username));
            this.SshPassword = RegistrySecureStringSetting.FromKey(
                "SshPassword",
                "Password",
                "Password, only applicable if Type is set to Password",
                Categories.SshCredentials,
                key,
                DataProtectionScope.CurrentUser);
            this.SshConnectionTimeout = RegistryDwordSetting.FromKey(
                "SshConnectionTimeout",
                "Connection timeout",
                "Timeout for establishing SSH connections, in seconds.",
                Categories.SshConnection,
                (int)SshParameters.DefaultConnectionTimeout.TotalSeconds,
                key,
                0, 300);

            //
            // App Settings.
            //
            this.AppUsername = RegistryStringSetting.FromKey(
                "AppUsername",
                null, // Hidden.
                null, // Hidden.
                null, // Hidden.
                null,
                key,
                username => string.IsNullOrEmpty(username) || !username.Contains(' '));
            this.AppNetworkLevelAuthentication = RegistryEnumSetting<AppNetworkLevelAuthenticationState>.FromKey(
                "AppNetworkLevelAuthentication",
                "Windows authentication",
                "Use Windows authentication for SQL Server connections.",
                Categories.AppCredentials,
                AppNetworkLevelAuthenticationState._Default,
                key);

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
            prototype.RdpTransport = (RegistryEnumSetting<SessionTransportType>)
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
            prototype.SshTransport = (RegistryEnumSetting<SessionTransportType>)
                baseSettings.SshTransport.OverlayBy(overlaySettings.SshTransport);
            prototype.SshCredentialType = (RegistryEnumSetting<SshCredentialType>)
                baseSettings.SshCredentialType.OverlayBy(overlaySettings.SshCredentialType);
            prototype.SshUsername = (RegistryStringSetting)
                baseSettings.SshUsername.OverlayBy(overlaySettings.SshUsername);
            prototype.SshPassword = (RegistrySecureStringSetting)
                baseSettings.SshPassword.OverlayBy(overlaySettings.SshPassword);
            prototype.SshConnectionTimeout = (RegistryDwordSetting)
                baseSettings.SshConnectionTimeout.OverlayBy(overlaySettings.SshConnectionTimeout);

            prototype.AppUsername = (RegistryStringSetting)
                baseSettings.AppUsername.OverlayBy(overlaySettings.AppUsername);
            prototype.AppNetworkLevelAuthentication = (RegistryEnumSetting<AppNetworkLevelAuthenticationState>)
                baseSettings.AppNetworkLevelAuthentication.OverlayBy(overlaySettings.AppNetworkLevelAuthentication);

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
