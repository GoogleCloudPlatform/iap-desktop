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

using Google.Solutions.Ssh.Cryptography;
using NUnit.Framework;
using System;

namespace Google.Solutions.Ssh.Test
{

    internal class KeyboardInteractiveHandler : IKeyboardInteractiveHandler
    {
        public delegate string? PromptDelegate(
            string name,
            string instruction,
            string prompt,
            bool echo);

        private readonly PromptDelegate prompt;

        public uint PromptCount { get; private set; } = 0;

        public KeyboardInteractiveHandler(
            PromptDelegate prompt)
        {
            this.prompt = prompt;
        }

        public string? Prompt(
            string name,
            string instruction,
            string prompt,
            bool echo)
        {
            this.PromptCount++;
            return this.prompt(name, instruction, prompt, echo);
        }

        public static KeyboardInteractiveHandler Silent = new KeyboardInteractiveHandler(
            (n, i, p, e) =>
            {
                Assert.Fail("Unexpected prompt");
                throw new InvalidOperationException("Unexpected prompt");
            });
    }


    internal sealed class StaticAsymmetricKeyCredential : IAsymmetricKeyCredential
    {
        public StaticAsymmetricKeyCredential(string username, IAsymmetricKeySigner signer)
        {
            this.Username = username;
            this.Signer = signer;
        }

        public IAsymmetricKeySigner Signer { get; }

        public string Username { get; }

        public void Dispose()
        {
        }
    }
}
