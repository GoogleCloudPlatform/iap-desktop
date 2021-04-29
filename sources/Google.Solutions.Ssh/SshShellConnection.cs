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

        private static readonly Encoding Encoding = Encoding.UTF8;

        private readonly string terminal;
        private readonly TerminalSize terminalSize;
        private readonly CultureInfo language;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SshShellConnection(
            string username,
            IPEndPoint endpoint,
            ISshKey key,
            string terminal,
            TerminalSize terminalSize,
            CultureInfo language,
            ReceivedAuthenticationPromptHandler authenticationPromptHandler,
            ReceiveStringDataHandler receiveDataHandler,
            ReceiveErrorHandler receiveErrorHandler)
            : base(
                  username,
                  endpoint,
                  key,
                  authenticationPromptHandler,
                  receiveDataHandler,
                  receiveErrorHandler,
                  Encoding)
        {
            this.terminal = terminal;
            this.terminalSize = terminalSize;
            this.language = language;
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override SshChannelBase CreateChannel(SshAuthenticatedSession session)
        {
            IEnumerable<EnvironmentVariable> environmentVariables = null;
            if (this.language != null)
            {
                // Format language so that Linux understands it.
                var languageFormatted = this.language.Name.Replace('-', '_');
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
                this.terminal,
                this.terminalSize.Columns,
                this.terminalSize.Rows,
                environmentVariables);
        }

        //---------------------------------------------------------------------
        // I/O.
        //---------------------------------------------------------------------

        public Task SendAsync(string data)
        {
            return base.SendAsync(Encoding.GetBytes(data));
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
