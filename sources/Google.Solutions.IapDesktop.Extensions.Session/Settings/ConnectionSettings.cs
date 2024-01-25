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
using Google.Solutions.Common.Util;
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
using System.Linq;
using System.Security.Cryptography;

#pragma warning disable CA1027 // Mark enums with FlagsAttribute

namespace Google.Solutions.IapDesktop.Extensions.Session.Settings
{
    public class ConnectionSettings : ISettingsCollection
    {
        /// <summary>
        /// Resource (instance, zone, project) that these settings apply to.
        /// </summary>
        public ResourceLocator Resource { get; }

        /// <summary>
        /// Create new empty settings.
        /// </summary>
        /// <param name="resource"></param>
        internal ConnectionSettings(ResourceLocator resource)
            : this(resource, null)
        {
        }

        /// <summary>
        /// Initialize settings from a registry key.
        /// </summary>
        public ConnectionSettings(
            ResourceLocator resource,
            RegistryKey key)
        {
            this.Resource = resource.ExpectNotNull(nameof(resource));

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
                "Connection bar",
                "Show connection bar in full-screen mode.",
                Categories.RdpDisplay,
                RdpConnectionBarState._Default,
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
            this.RdpRedirectWebAuthn = RegistryEnumSetting<RdpRedirectWebAuthn>.FromKey(
                "RdpRedirectWebAuthn",
                "Redirect WebAuthn authenticators",
                "Use local security key or Windows Hello device for WebAuthn authentication.",
                Categories.RdpResources,
                Protocol.Rdp.RdpRedirectWebAuthn._Default,
                key);
            this.RdpHookWindowsKeys = RegistryEnumSetting<RdpHookWindowsKeys>.FromKey(
                "RdpHookWindowsKeys",
                "Windows shortcuts",
                "Enable Windows shortcuts (like Win+R)",
                Categories.RdpResources,
                Protocol.Rdp.RdpHookWindowsKeys._Default,
                key);
            this.RdpRestrictedAdminMode = RegistryEnumSetting<RdpRestrictedAdminMode>.FromKey(
                "RdpRestrictedAdminMode",
                "Restricted Admin mode",
                "Disable the transmission of reusable credentials to the VM. This mode requires " +
                    "a user account with local administrator privileges on the VM, and the " +
                    "VM must be configured to permit Restricted Admin mode.",
                Categories.RdpSecurity,
                Protocol.Rdp.RdpRestrictedAdminMode._Default,
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
            this.SshPublicKeyAuthentication = RegistryEnumSetting<SshPublicKeyAuthentication>.FromKey(
                "SshPublicKeyAuthentication",
                "Public key authentication",
                "Automatically create an SSH key pair and publish it using OS Login or metadata keys.",
                Categories.SshCredentials,
                Protocol.Ssh.SshPublicKeyAuthentication._Default,
                key);
            this.SshUsername = RegistryStringSetting.FromKey(
                "SshUsername",
                "Username",
                "Linux username, optional",
                Categories.SshCredentials,
                null,
                key,
                username => string.IsNullOrEmpty(username) ||
                            LinuxUser.IsValidUsername(username));
            this.SshPassword = RegistrySecureStringSetting.FromKey(
                "SshPassword",
                "Password",
                "Password, only applicable if public key authentication is disabled",
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

        /// <summary>
        /// Create settings from a URL.
        /// </summary>
        public ConnectionSettings(IapRdpUrl url)
            : this(url.Instance)
        {
            //
            // Apply values from URL.
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

        internal ConnectionSettings OverlayBy(ConnectionSettings overlay)
        {
            overlay.ExpectNotNull(nameof(overlay));

            var merged = new ConnectionSettings(overlay.Resource);

            //
            // Apply this.
            //
            merged.RdpUsername = (RegistryStringSetting)
                this.RdpUsername.OverlayBy(overlay.RdpUsername);
            merged.RdpPassword = (RegistrySecureStringSetting)
                this.RdpPassword.OverlayBy(overlay.RdpPassword);
            merged.RdpDomain = (RegistryStringSetting)
                this.RdpDomain.OverlayBy(overlay.RdpDomain);
            merged.RdpConnectionBar = (RegistryEnumSetting<RdpConnectionBarState>)
                this.RdpConnectionBar.OverlayBy(overlay.RdpConnectionBar);
            merged.RdpAuthenticationLevel = (RegistryEnumSetting<RdpAuthenticationLevel>)
                this.RdpAuthenticationLevel.OverlayBy(overlay.RdpAuthenticationLevel);
            merged.RdpColorDepth = (RegistryEnumSetting<RdpColorDepth>)
                this.RdpColorDepth.OverlayBy(overlay.RdpColorDepth);
            merged.RdpAudioMode = (RegistryEnumSetting<RdpAudioMode>)
                this.RdpAudioMode.OverlayBy(overlay.RdpAudioMode);
            merged.RdpUserAuthenticationBehavior = (RegistryEnumSetting<RdpUserAuthenticationBehavior>)
                this.RdpUserAuthenticationBehavior.OverlayBy(overlay.RdpUserAuthenticationBehavior);
            merged.RdpNetworkLevelAuthentication = (RegistryEnumSetting<RdpNetworkLevelAuthentication>)
                this.RdpNetworkLevelAuthentication.OverlayBy(overlay.RdpNetworkLevelAuthentication);
            merged.RdpConnectionTimeout = (RegistryDwordSetting)
                this.RdpConnectionTimeout.OverlayBy(overlay.RdpConnectionTimeout);
            merged.RdpPort = (RegistryDwordSetting)
                this.RdpPort.OverlayBy(overlay.RdpPort);
            merged.RdpTransport = (RegistryEnumSetting<SessionTransportType>)
                this.RdpTransport.OverlayBy(overlay.RdpTransport);
            merged.RdpRedirectClipboard = (RegistryEnumSetting<RdpRedirectClipboard>)
                this.RdpRedirectClipboard.OverlayBy(overlay.RdpRedirectClipboard);
            merged.RdpRedirectPrinter = (RegistryEnumSetting<RdpRedirectPrinter>)
                this.RdpRedirectPrinter.OverlayBy(overlay.RdpRedirectPrinter);
            merged.RdpRedirectSmartCard = (RegistryEnumSetting<RdpRedirectSmartCard>)
                this.RdpRedirectSmartCard.OverlayBy(overlay.RdpRedirectSmartCard);
            merged.RdpRedirectPort = (RegistryEnumSetting<RdpRedirectPort>)
                this.RdpRedirectPort.OverlayBy(overlay.RdpRedirectPort);
            merged.RdpRedirectDrive = (RegistryEnumSetting<RdpRedirectDrive>)
                this.RdpRedirectDrive.OverlayBy(overlay.RdpRedirectDrive);
            merged.RdpRedirectDevice = (RegistryEnumSetting<RdpRedirectDevice>)
                this.RdpRedirectDevice.OverlayBy(overlay.RdpRedirectDevice);
            merged.RdpRedirectWebAuthn = (RegistryEnumSetting<RdpRedirectWebAuthn>)
                this.RdpRedirectWebAuthn.OverlayBy(overlay.RdpRedirectWebAuthn);
            merged.RdpHookWindowsKeys = (RegistryEnumSetting<RdpHookWindowsKeys>)
                this.RdpHookWindowsKeys.OverlayBy(overlay.RdpHookWindowsKeys);
            merged.RdpRestrictedAdminMode = (RegistryEnumSetting<RdpRestrictedAdminMode>)
                this.RdpRestrictedAdminMode.OverlayBy(overlay.RdpRestrictedAdminMode);

            merged.SshPort = (RegistryDwordSetting)
                this.SshPort.OverlayBy(overlay.SshPort);
            merged.SshTransport = (RegistryEnumSetting<SessionTransportType>)
                this.SshTransport.OverlayBy(overlay.SshTransport);
            merged.SshPublicKeyAuthentication = (RegistryEnumSetting<SshPublicKeyAuthentication>)
                this.SshPublicKeyAuthentication.OverlayBy(overlay.SshPublicKeyAuthentication);
            merged.SshUsername = (RegistryStringSetting)
                this.SshUsername.OverlayBy(overlay.SshUsername);
            merged.SshPassword = (RegistrySecureStringSetting)
                this.SshPassword.OverlayBy(overlay.SshPassword);
            merged.SshConnectionTimeout = (RegistryDwordSetting)
                this.SshConnectionTimeout.OverlayBy(overlay.SshConnectionTimeout);

            merged.AppUsername = (RegistryStringSetting)
                this.AppUsername.OverlayBy(overlay.AppUsername);
            merged.AppNetworkLevelAuthentication = (RegistryEnumSetting<AppNetworkLevelAuthenticationState>)
                this.AppNetworkLevelAuthentication.OverlayBy(overlay.AppNetworkLevelAuthentication);

            Debug.Assert(merged.Settings.All(s => s != null));

            return merged;
        }

        //---------------------------------------------------------------------
        // RDP settings.
        //---------------------------------------------------------------------

        public RegistryStringSetting RdpUsername { get; private set; }
        public RegistrySecureStringSetting RdpPassword { get; private set; }
        public RegistryStringSetting RdpDomain { get; private set; }
        public RegistryEnumSetting<RdpConnectionBarState> RdpConnectionBar { get; private set; }
        public RegistryEnumSetting<RdpAuthenticationLevel> RdpAuthenticationLevel { get; private set; }
        public RegistryEnumSetting<RdpColorDepth> RdpColorDepth { get; private set; }
        public RegistryEnumSetting<RdpAudioMode> RdpAudioMode { get; private set; }
        public RegistryEnumSetting<RdpUserAuthenticationBehavior> RdpUserAuthenticationBehavior { get; private set; }
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
        public RegistryEnumSetting<RdpRedirectWebAuthn> RdpRedirectWebAuthn { get; private set; }
        public RegistryEnumSetting<RdpHookWindowsKeys> RdpHookWindowsKeys { get; private set; }
        public RegistryEnumSetting<RdpRestrictedAdminMode> RdpRestrictedAdminMode { get; private set; }

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

            this.RdpColorDepth,
            this.RdpConnectionBar,

            this.RdpAudioMode,
            this.RdpHookWindowsKeys,
            this.RdpRedirectClipboard,
            this.RdpRedirectPrinter,
            this.RdpRedirectSmartCard,
            this.RdpRedirectPort,
            this.RdpRedirectDrive,
            this.RdpRedirectDevice,
            this.RdpRedirectWebAuthn,

            this.RdpUserAuthenticationBehavior,
            this.RdpNetworkLevelAuthentication,
            this.RdpAuthenticationLevel,
            this.RdpRestrictedAdminMode,
        };

        //---------------------------------------------------------------------
        // SSH settings.
        //---------------------------------------------------------------------

        public RegistryDwordSetting SshPort { get; private set; }
        public RegistryEnumSetting<SessionTransportType> SshTransport { get; private set; }
        public RegistryStringSetting SshUsername { get; private set; }
        public RegistrySecureStringSetting SshPassword { get; private set; }
        public RegistryDwordSetting SshConnectionTimeout { get; private set; }
        public RegistryEnumSetting<SshPublicKeyAuthentication> SshPublicKeyAuthentication { get; private set; }

        internal IEnumerable<ISetting> SshSettings => new ISetting[]
        {
            //
            // NB. The order determines the default order in the PropertyGrid
            // (assuming the PropertyGrid doesn't force alphabetical order).
            //
            this.SshTransport,
            this.SshConnectionTimeout,
            this.SshPort,
            this.SshPublicKeyAuthentication,
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
        // ISettingsCollection.
        //---------------------------------------------------------------------

        public IEnumerable<ISetting> Settings
        {
            get => this.RdpSettings
                .Concat(this.SshSettings)
                .Concat(this.AppSettings);
        }

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
    }
}
