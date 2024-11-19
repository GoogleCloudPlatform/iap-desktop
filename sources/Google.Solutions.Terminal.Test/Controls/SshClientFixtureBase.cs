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
using NUnit.Framework;
using System.Net;

namespace Google.Solutions.Terminal.Test.Controls
{
    public abstract class SshClientFixtureBase
    {
        private static string? serverAddress;
        private static string? username;
        private static string? password;

        [OneTimeSetUp]
        public static void CollectCredentials()
        {
            serverAddress = Interaction.InputBox("SSH server IP address");
            username = Interaction.InputBox("SSH username");
            password = Interaction.InputBox("SSH password");
        }

        [TearDown]
        public void TearDown()
        {
            //
            // Wait for worker threads to finish so that test caseses
            // don't interfere with another.
            //
            SshWorkerThread.JoinAllWorkerThreadsAsync().Wait();
        }

        private class KeyboardInteractiveHandler : IKeyboardInteractiveHandler
        {
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

        protected static ClientDiagnosticsWindow<TClient> CreateWindow<TClient>()
            where TClient : SshShellClient, new()
        {
            var window = new ClientDiagnosticsWindow<TClient>(new TClient());
            window.Client.ServerEndpoint = new IPEndPoint(
                IPAddress.Parse(serverAddress),
                22);
            window.Client.Credential = new StaticPasswordCredential(
                username ?? "test",
                SecureStringExtensions.FromClearText(password));
            window.Client.KeyboardInteractiveHandler = new KeyboardInteractiveHandler();
            return window;
        }
    }
}
