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

using Google.Solutions.Common.Security;
using Google.Solutions.Ssh;
using Google.Solutions.Terminal.Controls;
using Microsoft.VisualBasic;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.TestApp
{
    public static class Program
    {
        /// <summary>
        /// SSH client that uses password authentication.
        /// </summary>
        private class SimpleSshShellClient : SshShellClient, IKeyboardInteractiveHandler
        {
            public SimpleSshShellClient()
            {
                this.KeyboardInteractiveHandler = this;
            }

            [Browsable(true)]
            [Category(SshCategory)]
            public string? Username { get; set; }

            [Browsable(true)]
            [Category(SshCategory)]
            public string? Password { get; set; }

            public override ISshCredential? Credential 
            { 
                get => this.Username != null
                    ? new StaticPasswordCredential(
                        this.Username, 
                        SecureStringExtensions.FromClearText(this.Password))
                    : null;
                set => throw new InvalidOperationException();
            }

            public string? Prompt(string caption, string instruction, string prompt, bool echo)
            {
                return Interaction.InputBox(
                    $"Caption: {caption}\nPrompt: {prompt}",
                    caption);
            }

            public IPasswordCredential PromptForCredentials(string username)
            {
                var password = Interaction.InputBox($"Enter password for {username}");
                return new StaticPasswordCredential(username, password);
            }
        }

        private static ClientBase CreateClient(string[] args)
        {
            if (args.FirstOrDefault() == "/rdp")
            {
                var client = new RdpClient();
                if (args.Skip(1).FirstOrDefault() is string server)
                {
                    client.Server = server;
                }

                return client;
            }
            else if (args.FirstOrDefault() == "/ssh")
            {
                var client = new SimpleSshShellClient();
                if (args.Skip(1).FirstOrDefault() is string server)
                {
                    client.ServerEndpoint = new IPEndPoint(
                        IPAddress.Parse(server),
                        22);
                }

                return client;
            }
            else
            {
                var psClient = new LocalShellClient("powershell.exe");
                psClient.Terminal.ForeColor = Color.LightGray;
                psClient.Terminal.BackColor = Color.Black;

                return psClient;
            }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            using (var form = new ClientDiagnosticsWindow<ClientBase>(CreateClient(args)))
            {
                Application.Run(form);
            }
        }
    }
}