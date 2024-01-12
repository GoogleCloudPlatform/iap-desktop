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

using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls
{
    public partial class RdpClient
    {
        /// <summary>
        /// Server to connect to.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Server
        {
            get => this.client.Server;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.client.Server = value;
            }
        }

        /// <summary>
        /// Server port to connect to.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ushort ServerPort
        {
            get => (ushort)this.client.AdvancedSettings7.RDPPort;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RDPPort = value;
            }
        }

        /// <summary>
        /// NetBIOS domain to use for authentication.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Domain
        {
            get => this.client.Domain;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.client.Domain = value;
            }
        }

        /// <summary>
        /// UPN or username to use for authentication.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Username
        {
            get => this.client.UserName;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.client.UserName = value;
            }
        }

        /// <summary>
        /// Password to use for authentication.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Password
        {
            get => "*";
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.ClearTextPassword = value;
            }
        }

        /// <summary>
        /// Enable Network Level Authentication (CredSSP).
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableNetworkLevelAuthentication
        {
            get => this.clientAdvancedSettings.EnableCredSspSupport;
            set
            {
                ExpectState(ConnectionState.NotConnected);
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
        public uint ServerAuthenticationLevel
        {
            get => this.clientAdvancedSettings.AuthenticationLevel;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.AuthenticationLevel = value;
            }
        }

        /// <summary>
        /// Show a credential prompt if invalid credentials were passed.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableCredentialPrompt
        {
            get => this.clientNonScriptable.AllowPromptingForCredentials;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientNonScriptable.AllowPromptingForCredentials = value;
            }
        }

        /// <summary>
        /// Connection Timeout, including time allowed to enter user credentials.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimeSpan ConnectionTimeout
        {
            get => TimeSpan.FromSeconds(this.clientAdvancedSettings.singleConnectionTimeout);
            set
            {
                ExpectState(ConnectionState.NotConnected);

                //
                // Apply timeouts. Note that the control might take
                // about twice the configured timeout before sending a 
                // OnDisconnected event.
                //
                this.clientAdvancedSettings.singleConnectionTimeout = (int)value.TotalSeconds;
                this.clientAdvancedSettings.overallConnectionTimeout = (int)value.TotalSeconds;
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
        public bool EnableConnectionBar
        {
            get => this.clientAdvancedSettings.DisplayConnectionBar;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.DisplayConnectionBar = value;
            }
        }

        /// <summary>
        /// Display a minimize button in the (blue) connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableConnectionBarMinimizeButton
        {
            get => this.clientAdvancedSettings.ConnectionBarShowMinimizeButton;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.ConnectionBarShowMinimizeButton = value;
            }
        }

        /// <summary>
        /// Pin the (blue) connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableConnectionBarPin
        {
            get => this.clientAdvancedSettings.PinConnectionBar;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.PinConnectionBar = value;
            }
        }

        /// <summary>
        /// Text to show in connection bar at the top.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ConnectionBarText
        {
            get => this.clientNonScriptable.ConnectionBarText;
            set
            {
                ExpectState(ConnectionState.NotConnected);
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
        public bool EnableClipboardRedirection
        {
            get => this.clientAdvancedSettings.RedirectClipboard;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectClipboard = value;
            }
        }

        /// <summary>
        /// Redirect local printers.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnablePrinterRedirection
        {
            get => this.clientAdvancedSettings.RedirectPrinters;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectPrinters = value;
            }
        }

        /// <summary>
        /// Redirect local smart cards.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableSmartCardRedirection
        {
            get => this.clientAdvancedSettings.RedirectSmartCards;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectSmartCards = value;
            }
        }

        /// <summary>
        /// Redirect local drives.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableDriveRedirection
        {
            get => this.clientAdvancedSettings.RedirectDrives;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectDrives = value;
            }
        }

        /// <summary>
        /// Redirect local devices.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableDeviceRedirection
        {
            get => this.clientAdvancedSettings.RedirectDevices;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectDevices = value;
            }
        }

        /// <summary>
        /// Redirect local ports.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnablePortRedirection
        {
            get => this.clientAdvancedSettings.RedirectPorts;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientAdvancedSettings.RedirectPorts = value;
            }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableWebAuthnRedirection { get; set; }

        //---------------------------------------------------------------------
        // Hotkey properties.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
        public Keys LeaveFullScreenHotKey
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
        public int KeyboardHookMode
        {
            get => this.clientSecuredSettings.KeyboardHookMode;
            set
            {
                ExpectState(ConnectionState.NotConnected);
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
        public int AudioRedirectionMode
        {
            get => this.clientSecuredSettings.AudioRedirectionMode;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.clientSecuredSettings.AudioRedirectionMode = value;
            }
        }

        /// <summary>
        /// Color depth, in bits
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ColorDepth
        {
            get => this.client.ColorDepth;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.client.ColorDepth = value;
            }
        }
    }
}
