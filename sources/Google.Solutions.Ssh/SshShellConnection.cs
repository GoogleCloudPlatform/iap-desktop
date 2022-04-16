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

using Google.Solutions.Ssh.Auth;
using Google.Solutions.Ssh.Native;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    public class SshShellConnection : SshConnectionBase
    {
        public const string DefaultTerminal = "xterm";
        public static readonly TerminalSize DefaultTerminalSize = new TerminalSize(80, 24);

        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        private readonly ITextTerminal terminal;
        private readonly TerminalSize initialSize;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SshShellConnection(
            IPEndPoint endpoint,
            ISshAuthenticator authenticator,
            ITextTerminal terminal,
            TerminalSize initialSize)
            : base(
                  endpoint,
                  authenticator,
                  terminal.ToRawTerminal(DefaultEncoding))
        {
            this.terminal = terminal;
            this.initialSize = initialSize;
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override SshChannelBase CreateChannel(SshAuthenticatedSession session)
        {
            IEnumerable<EnvironmentVariable> environmentVariables = null;
            if (this.terminal.Locale != null)
            {
                // Format language so that Linux understands it.
                var languageFormatted = this.terminal.Locale.Name.Replace('-', '_');
                environmentVariables = new[]
                {
                    //
                    // Try to pass locale - but do not fail the connection if
                    // the server rejects it.
                    //
                    new EnvironmentVariable(
                        "LC_ALL",
                        $"{languageFormatted}.UTF-8",
                        false)
                };
            }

            return session.OpenShellChannel(
                LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE,
                this.terminal.TerminalType,
                this.initialSize.Columns,
                this.initialSize.Rows,
                environmentVariables);
        }

        //---------------------------------------------------------------------
        // I/O.
        //---------------------------------------------------------------------

        public Task SendAsync(string data)
        {
            return base.SendAsync(DefaultEncoding.GetBytes(data));
        }

        public Task ResizeTerminalAsync(TerminalSize size)
        {
            return SendAsync(
                channel => ((SshShellChannel)channel).ResizePseudoTerminal(
                    size.Columns,
                    size.Rows));
        }
    }
}
