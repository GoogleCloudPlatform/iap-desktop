//
// Copyright 2023 Google LLC
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

using System;
using System.ComponentModel;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp
{
    public class RdpParameters : SessionParametersBase
    {
        internal const ushort DefaultPort = 3389;
        internal static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(30);

        public ushort Port { get; set; } = DefaultPort;
        public TimeSpan ConnectionTimeout { get; set; } = DefaultConnectionTimeout;
        public RdpConnectionBarState ConnectionBar { get; set; } = RdpConnectionBarState._Default;
        public RdpAuthenticationLevel AuthenticationLevel { get; set; } = RdpAuthenticationLevel._Default;
        public RdpColorDepth ColorDepth { get; set; } = RdpColorDepth._Default;
        public RdpAudioMode AudioMode { get; set; } = RdpAudioMode._Default;
        public RdpNetworkLevelAuthentication NetworkLevelAuthentication { get; set; } = RdpNetworkLevelAuthentication._Default;
        public RdpAutomaticLogon UserAuthenticationBehavior { get; set; } = RdpAutomaticLogon._Default;
        public RdpRedirectClipboard RedirectClipboard { get; set; } = RdpRedirectClipboard._Default;
        public RdpRedirectPrinter RedirectPrinter { get; set; } = RdpRedirectPrinter._Default;
        public RdpRedirectSmartCard RedirectSmartCard { get; set; } = RdpRedirectSmartCard._Default;
        public RdpRedirectPort RedirectPort { get; set; } = RdpRedirectPort._Default;
        public RdpRedirectDrive RedirectDrive { get; set; } = RdpRedirectDrive._Default;
        public RdpRedirectDevice RedirectDevice { get; set; } = RdpRedirectDevice._Default;
        public RdpRedirectWebAuthn RedirectWebAuthn { get; set; } = RdpRedirectWebAuthn._Default;
        public RdpHookWindowsKeys HookWindowsKeys { get; set; } = RdpHookWindowsKeys._Default;
        public RdpRestrictedAdminMode RestrictedAdminMode { get; set; } = RdpRestrictedAdminMode._Default;
        public RdpSessionType SessionType { get; set; } = RdpSessionType._Default;
        public RdpDpiScaling DpiScaling { get; set; } = RdpDpiScaling._Default;
        public RdpDesktopSize DesktopSize { get; set; } = RdpDesktopSize._Default;


        /// <summary>
        /// Sources where these parameters were obtained from.
        /// </summary>
        public ParameterSources Sources { get; set; } = ParameterSources.Inventory;

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        [Flags]
        public enum ParameterSources
        {
            /// <summary>
            /// One or more parameter is sourced from a iap-rdp:/// URL.
            /// </summary>
            Url = 1,

            /// <summary>
            ///One or more parameter is sourced from the inventory.
            /// </summary>
            Inventory = 2
        }
    }

    //-------------------------------------------------------------------------
    // Enums.
    //
    // NB. The values do not map to RDP interface values. But the numeric values
    // must be kept unchanged as they are persisted as settings.
    //
    //-------------------------------------------------------------------------

    public enum RdpConnectionBarState
    {
        [Description("Auto hide")]
        AutoHide = 0,

        [Description("Pinned")]
        Pinned = 1,

        [Description("Hide")]
        Off = 2,

        [Browsable(false)]
        _Default = AutoHide
    }


    public enum RdpDesktopSize
    {
        [Description("Same as this computer")]
        ScreenSize = 1,

        [Description("Adjust automatically")]
        AutoAdjust = 2,

        [Browsable(false)]
        LegacyClientSize = 0,

        [Browsable(false)]
        _Default = AutoAdjust
    }

    public enum RdpAuthenticationLevel
    {
        // Likely to fail when using IAP unless the cert has been issued
        // for "localhost".
        AttemptServerAuthentication = 0,

        // Almost guaranteed to fail, so do not even display it.
        [Browsable(false)]
        RequireServerAuthentication = 1,

        NoServerAuthentication = 3,

        [Browsable(false)]
        _Default = NoServerAuthentication
    }

    public enum RdpColorDepth
    {
        [Description("High color (16 bit)")]
        HighColor = 0,

        [Description("True color (24 bit)")]
        TrueColor = 1,

        [Description("Highest quality (32 bit)")]
        DeepColor = 2,

        [Browsable(false)]
        _Default = TrueColor
    }

    public enum RdpAudioMode
    {
        [Description("Play on this computer")]
        PlayLocally = 0,

        [Description("Play on remote VM")]
        PlayOnServer = 1,

        [Description("Don't play")]
        DoNotPlay = 2,

        [Browsable(false)]
        _Default = PlayLocally
    }

    public enum RdpRedirectClipboard
    {
        [Description("Don't share")]
        Disabled = 0,

        [Description("Share")]
        Enabled = 1,

        [Browsable(false)]
        _Default = Enabled
    }

    /// <summary>
    /// Controls whether IAP Desktop engages in trying to
    /// automatically log on the user, or whether to hand off
    /// authentication to the RDP control entirely.
    /// </summary>
    public enum RdpAutomaticLogon
    {
        /// <summary>
        /// Allow users to enter new credentials when saved
        /// credentials are missing or invalid.
        /// </summary>
        Enabled = 0,

        /// <summary>
        /// Abort when saved credentials are missing or invalid.
        /// </summary>
        /// <remarks>
        /// This is a legacy setting that shouldn't be used anymore.
        /// </remarks>
        [Browsable(false)]
        LegacyAbortOnFailure = 1,

        /// <summary>
        /// Ignore saved credentials and always prompt, matches the
        /// "Always prompt for password upon connection" server-side
        /// group policy.
        /// </summary>
        Disabled = 2,

        [Browsable(false)]
        _Default = Enabled
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

    public enum RdpRedirectPrinter
    {
        [Description("Don't share")]
        Disabled = 0,

        [Description("Share")]
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpRedirectSmartCard
    {
        [Description("Don't share")]
        Disabled = 0,

        [Description("Share")]
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpRedirectPort
    {
        [Description("Don't share")]
        Disabled = 0,

        [Description("Share")]
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpRedirectDrive
    {
        [Description("Don't share")]
        Disabled = 0,

        [Description("Share")]
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpRedirectDevice
    {
        [Description("Don't share")]
        Disabled = 0,

        [Description("Share")]
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpRedirectWebAuthn
    {
        [Description("Don't share")]
        Disabled = 0,

        [Description("Share")]
        Enabled = 1,

        [Browsable(false)]
        _Default = Enabled
    }

    public enum RdpNetworkLevelAuthentication
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Enabled
    }

    public enum RdpHookWindowsKeys
    {
        //
        // NB. Values correspond to IMsRdpClientSecuredSettings::KeyboardHookMode.
        //
        [Description("Don't redirect")]
        Disabled = 0,

        [Description("Redirect to remote VM")]
        Enabled = 1,

        [Description("Redirect in full-screen")]
        FullScreenOnly = 2,

        [Browsable(false)]
        _Default = FullScreenOnly
    }

    public enum RdpRestrictedAdminMode
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpSessionType
    {
        /// <summary>
        /// Normal user session, might consume a CAL.
        /// </summary>
        [Description("Normal user-session")]
        User = 0,

        /// <summary>
        /// Admin session, equivalent to "mstsc /admin".
        /// </summary>
        [Description("RDS admin-session")]
        Admin = 1,

        [Browsable(false)]
        _Default = User
    }

    public enum RdpDpiScaling
    {
        [Description("Disabled (100%)")]
        Disabled = 0,

        [Description("Same as this computer")]
        Enabled = 1,

        [Browsable(false)]
        _Default = Enabled
    }
}
