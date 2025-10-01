//
// Copyright 2024 Google LLC
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

using Google.Solutions.Platform.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    public partial class RdpClient
    {
        private const string RdpCategory = "Remote Desktop";

        /// <summary>
        /// Server to connect to.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public string? Server
        {
            get => this.client.Server;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.client.Server = value;
            }
        }

        /// <summary>
        /// Server port to connect to.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public ushort ServerPort
        {
            get => (ushort)this.client.AdvancedSettings7.RDPPort;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.RDPPort = value;
            }
        }

        /// <summary>
        /// NetBIOS domain to use for authentication.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public string? Domain
        {
            get => this.client.Domain;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.client.Domain = value;
            }
        }

        /// <summary>
        /// UPN or username to use for authentication.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public string? Username
        {
            get => this.client.UserName;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.client.UserName = value;
            }
        }

        /// <summary>
        /// Password to use for authentication.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public string? Password
        {
            get => "*";
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.ClearTextPassword = value;
            }
        }

        /// <summary>
        /// Connection Timeout, including time allowed to enter user credentials.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public TimeSpan ConnectionTimeout
        {
            get => TimeSpan.FromSeconds(this.clientAdvancedSettings.singleConnectionTimeout);
            set
            {
                ExpectState(ClientState.NotConnected);

                //
                // Apply timeouts. Note that the control might take
                // about twice the configured timeout before sending a 
                // OnDisconnected event.
                //
                this.clientAdvancedSettings.singleConnectionTimeout = (int)value.TotalSeconds;
                this.clientAdvancedSettings.overallConnectionTimeout = (int)value.TotalSeconds;
            }
        }

        /// <summary>
        /// Connect to the server for administrative purposes, equivalent
        /// to running "mstsc /admin". Only relevant for RDS.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableAdminMode
        {
            get => this.clientAdvancedSettings.ConnectToAdministerServer;
            set => this.clientAdvancedSettings.ConnectToAdministerServer = value;
        }

        //---------------------------------------------------------------------
        // Security properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Enable Network Level Authentication (CredSSP).
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableNetworkLevelAuthentication
        {
            get => this.clientAdvancedSettings.EnableCredSspSupport;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.EnableCredSspSupport = value;

                if (!value)
                {
                    //
                    // To disable NLA, we must enable server authentication.
                    //
                    this.clientAdvancedSettings.AuthenticationLevel = 2;
                }
            }
        }

        /// <summary>
        /// Server authentication level.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public uint ServerAuthenticationLevel
        {
            get => this.clientAdvancedSettings.AuthenticationLevel;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.AuthenticationLevel = value;
            }
        }

        /// <summary>
        /// Show a credential prompt if invalid credentials were passed.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableCredentialPrompt
        {
            get => this.clientNonScriptable.AllowPromptingForCredentials;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientNonScriptable.AllowPromptingForCredentials = value;
            }
        }

        /// <summary>
        /// Enable 'restricted admin' mode.
        /// 
        /// Note the following:
        /// 
        /// 1. Restricted admin mode must be enabled on the server first:
        ///    
        ///    New-ItemProperty -Path"HKLM:\System\CurrentControlSet\Control\Lsa" `
        ///      -Name "DisableRestrictedAdmin"
        ///      -Value 0 `
        ///      -PropertyType DWORD -Force
        ///      
        ///    See 'Microsoft Security Advisory 2871997' for details:
        ///    https://learn.microsoft.com/en-us/security-updates/securityadvisories/2016/2871997
        ///    
        ///    Optionally, it can be enforced via GPO:
        ///    
        ///    Computer Configuration\Policies\Administrative Templates\System\Credentials Delegation
        ///    > 'Restrict delegation of credentials to remote servers' = enabled
        ///    
        /// 2. Restricted admin mode only works for users that have local admin
        ///    rights on the target machine.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableRestrictedAdminMode
        {
            get
            {
                try
                {
                    return (bool)this.clientExtendedSettings.get_Property("RestrictedLogon");
                }
                catch (Exception)
                {
                    return false;
                }
            }
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientExtendedSettings.set_Property("RestrictedLogon", value);
            }
        }

        /// <summary>
        /// Enable remote credential guard (RCG).
        /// 
        /// Note the following:
        /// 
        /// 1. Restricted admin mode must be enabled on the server first
        ///    (same as for restricted admin mode).
        ///    
        /// 2. RCG requires Kerberos.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableRemoteCredentialGuard
        {
            get
            {
                try
                {
                    return
                        (bool)this.clientExtendedSettings.get_Property("DisableCredentialsDelegation") &&
                        (bool)this.clientExtendedSettings.get_Property("RedirectedAuthentication");
                }
                catch (Exception)
                {
                    return false;
                }
            }
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientExtendedSettings.set_Property("DisableCredentialsDelegation", value);
                this.clientExtendedSettings.set_Property("RedirectedAuthentication", value);
            }
        }

        //---------------------------------------------------------------------
        // Connection Bar properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Display the (blue) connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableConnectionBar
        {
            get => this.clientAdvancedSettings.DisplayConnectionBar;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.DisplayConnectionBar = value;
            }
        }

        /// <summary>
        /// Display a minimize button in the (blue) connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableConnectionBarMinimizeButton
        {
            get => this.clientAdvancedSettings.ConnectionBarShowMinimizeButton;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.ConnectionBarShowMinimizeButton = value;
            }
        }

        /// <summary>
        /// Pin the (blue) connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableConnectionBarPin
        {
            get => this.clientAdvancedSettings.PinConnectionBar;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.PinConnectionBar = value;
            }
        }

        /// <summary>
        /// Text to show in connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public string ConnectionBarText
        {
            get => this.clientNonScriptable.ConnectionBarText;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientNonScriptable.ConnectionBarText = value;
            }
        }

        //---------------------------------------------------------------------
        // Device properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Redirect the clipboard.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableClipboardRedirection
        {
            get => this.clientAdvancedSettings.RedirectClipboard;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.RedirectClipboard = value;
            }
        }

        /// <summary>
        /// Redirect local printers.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnablePrinterRedirection
        {
            get => this.clientAdvancedSettings.RedirectPrinters;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.RedirectPrinters = value;
            }
        }

        /// <summary>
        /// Redirect local smart cards.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableSmartCardRedirection
        {
            get => this.clientAdvancedSettings.RedirectSmartCards;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.RedirectSmartCards = value;
            }
        }

        /// <summary>
        /// Redirect local drives.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableDriveRedirection
        {
            get => this.clientAdvancedSettings.RedirectDrives;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.RedirectDrives = value;
            }
        }

        /// <summary>
        /// Redirect local devices.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableDeviceRedirection
        {
            get => this.clientAdvancedSettings.RedirectDevices;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.RedirectDevices = value;
            }
        }

        /// <summary>
        /// Redirect local ports.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnablePortRedirection
        {
            get => this.clientAdvancedSettings.RedirectPorts;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.RedirectPorts = value;
            }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableWebAuthnRedirection { get; set; }

        //---------------------------------------------------------------------
        // Hotkey properties.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public Keys FocusHotKey
        {
            //
            // NB. The Ctrl+Alt modifiers are implied by the HotKeyFocusRelease properties.
            //
            get => (Keys)this.clientAdvancedSettings.HotKeyFocusReleaseLeft | Keys.Control | Keys.Alt;
            set
            {
                Debug.Assert(value.HasFlag(Keys.Control));
                Debug.Assert(value.HasFlag(Keys.Alt));

                var keyWithoutModifiers = (int)(value & ~(Keys.Control | Keys.Alt));

                this.clientAdvancedSettings.HotKeyFocusReleaseLeft = keyWithoutModifiers;
                this.clientAdvancedSettings.HotKeyFocusReleaseRight = keyWithoutModifiers;
            }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public Keys FullScreenHotKey
        {
            //
            // NB. The Ctrl+Alt modifiers are implied by the HotKeyFullScreen properties.
            //
            get => (Keys)this.clientAdvancedSettings.HotKeyFullScreen | Keys.Control | Keys.Alt;
            set
            {
                Debug.Assert(value.HasFlag(Keys.Control));
                Debug.Assert(value.HasFlag(Keys.Alt));

                var keyWithoutModifiers = (int)(value & ~(Keys.Control | Keys.Alt));

                this.clientAdvancedSettings.HotKeyFullScreen = keyWithoutModifiers;
                this.clientAdvancedSettings.HotKeyFullScreen = keyWithoutModifiers;
            }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public int KeyboardHookMode
        {
            get => this.clientSecuredSettings.KeyboardHookMode;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientSecuredSettings.KeyboardHookMode = value;
            }
        }

        //---------------------------------------------------------------------
        // Sound and color properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Control where to play audio:
        /// 
        /// 0 - play locally
        /// 1 - play on server
        /// 3 - do not play
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public int AudioRedirectionMode
        {
            get => this.clientSecuredSettings.AudioRedirectionMode;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientSecuredSettings.AudioRedirectionMode = value;
            }
        }

        /// <summary>
        /// Indicates whether the default audio input device is redirected from the 
        /// client to the remote session.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableAudioCaptureRedirection
        {
            get => this.clientAdvancedSettings.AudioCaptureRedirectionMode;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.clientAdvancedSettings.AudioCaptureRedirectionMode = value;
            }
        }

        /// <summary>
        /// Color depth, in bits.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public int ColorDepth
        {
            get => this.client.ColorDepth;
            set
            {
                ExpectState(ClientState.NotConnected);
                this.client.ColorDepth = value;
            }
        }

        /// <summary>
        /// Scale DPI setting of remote session to match local DPI settings.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableDpiScaling { get; set; }

        /// <summary>
        /// Auto-resize remote desktop to fit client size.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(RdpCategory)]
        public bool EnableAutoResize { get; set; } = true;
    }
}
