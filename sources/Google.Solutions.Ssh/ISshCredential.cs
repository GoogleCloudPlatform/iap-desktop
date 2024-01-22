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
using System.Net;
using System.Security;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// Base interface.
    /// </summary>
    public interface ISshCredential : IDisposable
    {
        /// <summary>
        /// Name of remote user to authenticate with.
        /// </summary>
        string Username { get; }
    }

    /// <summary>
    /// Authenticator for "publickey" authentication.
    /// </summary>
    public interface IAsymmetricKeyCredential : ISshCredential
    {
        /// <summary>
        /// Signer for handling authentication challenges.
        /// </summary>
        IAsymmetricKeySigner Signer { get; }
    }

    /// <summary>
    /// Authenticator for "password" authentication.
    /// </summary>
    public interface IPasswordCredential : ISshCredential
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
        /// Prompt for keyboard-interactive input.
        /// </summary>
        /// <exception cref="OperationCanceledException">
        /// Thrown when users cancels the operation.
        /// </exception>
        string? Prompt(
            string caption,
            string instruction,
            string prompt,
            bool echo);

        /// <summary>
        /// Perform for password.
        /// </summary>
        /// <returns>
        /// Credential for the same username.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown when users cancels the operation.
        /// </exception>
        IPasswordCredential PromptForCredentials(string username);
    }
}
