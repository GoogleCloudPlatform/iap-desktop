﻿//
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

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Session
{
    public class RdpSessionParameters
    {
        internal const ushort DefaultPort = 3389;
        internal static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(30);

        public ushort Port { get; set; } = DefaultPort;
        public TimeSpan ConnectionTimeout { get; set; } = DefaultConnectionTimeout;
        public RdpConnectionBarState ConnectionBar { get; set; } = RdpConnectionBarState._Default;
        public RdpDesktopSize DesktopSize { get; set; } = RdpDesktopSize._Default;
        public RdpAuthenticationLevel AuthenticationLevel { get; set; } = RdpAuthenticationLevel._Default;
        public RdpColorDepth ColorDepth { get; set; } = RdpColorDepth._Default;
        public RdpAudioMode AudioMode { get; set; } = RdpAudioMode._Default;
        public RdpBitmapPersistence BitmapPersistence { get; set; } = RdpBitmapPersistence._Default;
        public RdpNetworkLevelAuthentication NetworkLevelAuthentication { get; set; } = RdpNetworkLevelAuthentication._Default;
        public RdpUserAuthenticationBehavior UserAuthenticationBehavior { get; set; } = RdpUserAuthenticationBehavior._Default;
        public RdpRedirectClipboard RedirectClipboard { get; set; } = RdpRedirectClipboard._Default;
        public RdpRedirectPrinter RedirectPrinter { get; set; } = RdpRedirectPrinter._Default;
        public RdpRedirectSmartCard RedirectSmartCard { get; set; } = RdpRedirectSmartCard._Default;
        public RdpRedirectPort RedirectPort { get; set; } = RdpRedirectPort._Default;
        public RdpRedirectDrive RedirectDrive { get; set; } = RdpRedirectDrive._Default;
        public RdpRedirectDevice RedirectDevice { get; set; } = RdpRedirectDevice._Default;
        public RdpHookWindowsKeys HookWindowsKeys { get; set; } = RdpHookWindowsKeys._Default;

        public Transport.TransportType TransportType { get; set; } = Transport.TransportType._Default;

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

    public enum RdpRedirectPrinter
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpRedirectSmartCard
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpRedirectPort
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpRedirectDrive
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpRedirectDevice
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
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
        Never = 0,
        Always = 1,
        FullScreenOnly = 2,


        [Browsable(false)]
        _Default = FullScreenOnly
    }
}
