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
using System;
using System.Security;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// Base interface.
    /// </summary>
    public interface ISshUserCredential : IDisposable
    {
    }

    /// <summary>
    /// Authenticator for "publickey" authentication.
    /// </summary>
    public interface IAsymmetricKeyCredential : ISshUserCredential 
    {
        /// <summary>
        /// Public key that corresponds to the signing key.
        /// </summary>
        PublicKey PublicKey { get; }

        /// <summary>
        /// Sign an authentication challenge.
        /// </summary>
        /// <returns>Signature, in the format expected by SSH</returns>
        byte[] Sign(AuthenticationChallenge challenge);
    }

    /// <summary>
    /// Authenticator for "password" authentication.
    /// </summary>
    public interface IPasswordCredential : ISshUserCredential 
    { 
        SecureString Password { get; }
    }

    /// <summary>
    /// Handler for "keyboard-interactive" prompts that might be required
    /// in addition to presenting a credential.
    /// </summary>
    public interface IKeyboardInteractiveHandler
    {
        /// <summary>
        /// Respond to interactive prompt.
        /// </summary>
        string? Prompt(
            string name,
            string instruction,
            string prompt,
            bool echo);
    }
}
