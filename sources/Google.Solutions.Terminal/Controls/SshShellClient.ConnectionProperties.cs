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

using Google.Solutions.Ssh;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;

namespace Google.Solutions.Terminal.Controls
{
    public partial class SshShellClient
    {
        protected const string SshCategory = "SSH";

        private IPEndPoint? serverEndpoint;
        private ISshCredential? credential;
        private string? banner;
        private TimeSpan connectionTimeout = TimeSpan.FromSeconds(30);
        private CultureInfo? locale;
        private IKeyboardInteractiveHandler keyboardInteractiveHandler
            = new DefaultKeyboardInteractiveHandler();

        /// <summary>
        /// Endpoint to connect to.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public IPEndPoint? ServerEndpoint
        {
            get => this.serverEndpoint;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.serverEndpoint = value;
            }
        }

        /// <summary>
        /// User credential to authenticate with.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public virtual ISshCredential? Credential
        {
            get => this.credential;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.credential = value;
            }
        }

        /// <summary>
        /// Handler for password/input prompts.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public IKeyboardInteractiveHandler KeyboardInteractiveHandler
        {
            get => this.keyboardInteractiveHandler;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.keyboardInteractiveHandler = value;
            }
        }

        /// <summary>
        /// Client banner to send to server.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public string? Banner
        {
            get => this.banner;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.banner = value;
            }
        }

        /// <summary>
        /// Timeout for establishing an SSH connection.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public TimeSpan ConnectionTimeout
        {
            get => this.connectionTimeout;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.connectionTimeout = value;
            }
        }

        /// <summary>
        /// LC_ALL locale.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public CultureInfo? Locale
        {
            get => this.locale;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.locale = value;
            }
        }

        /// <summary>
        /// Type of terminal ($TERM) to use.
        /// </summary>
        public string TerminalType
        {
            get; set;
        } = "xterm";

        //---------------------------------------------------------------------
        // Inner types.
        //---------------------------------------------------------------------

        /// <summary>
        /// Default handler, cancels all input.
        /// </summary>
        private class DefaultKeyboardInteractiveHandler : IKeyboardInteractiveHandler
        {
            public string? Prompt(string caption, string instruction, string prompt, bool echo)
            {
                throw new OperationCanceledException();
            }

            public IPasswordCredential PromptForCredentials(string username)
            {
                throw new OperationCanceledException();
            }
        }
    }
}
