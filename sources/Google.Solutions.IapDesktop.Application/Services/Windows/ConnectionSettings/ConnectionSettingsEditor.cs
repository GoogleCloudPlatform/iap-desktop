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

using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Windows.SettingsEditor;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.ComponentModel;
using System.Security;

namespace Google.Solutions.IapDesktop.Application.Services.Windows.ConnectionSettings
{
    public class ConnectionSettingsEditor : ISettingsObject
    {
        private readonly ConnectionSettingsEditor parent;
        private readonly ConnectionSettingsBase settings;
        private readonly Action<ConnectionSettingsBase> saveSettings;

        public ConnectionSettingsEditor(
            ConnectionSettingsBase settings,
            Action<ConnectionSettingsBase> saveSettings,
            ConnectionSettingsEditor parent)
        {
            this.settings = settings;
            this.saveSettings = saveSettings;
            this.parent = parent;
        }

        //---------------------------------------------------------------------
        // ISettingsObject.
        //---------------------------------------------------------------------

        public void SaveChanges()
        {
            this.saveSettings(this.settings);
        }

        public virtual string InformationText { get; set; }


        //---------------------------------------------------------------------
        // 
        //---------------------------------------------------------------------

        public VmInstanceConnectionSettings CreateConnectionSettings(
            string instanceName)
        {
            return new VmInstanceConnectionSettings()
            {
                InstanceName = instanceName,
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
        }

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

        public SecureString Password
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
}
