//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Auth;
using System;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// Source of authentication information.
    /// </summary>
    public interface ISshAuthenticator
    {
        /// <summary>
        /// Username.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Key pair for public/private key authentication.
        /// </summary>
        ISshKeyPair KeyPair { get; }

        /// <summary>
        /// Prompt for additional (second factor)
        /// information.
        /// </summary>
        string Prompt(
            string name,
            string instruction,
            string prompt,
            bool echo);
    }

    /// <summary>
    /// Authenticator that uses a public key and doesn't
    /// support 2FA.
    /// </summary>
    public class SshSingleFactorAuthenticator : ISshAuthenticator
    {
        public string Username { get; }

        public ISshKeyPair KeyPair { get; }

        public SshSingleFactorAuthenticator(
            string username,
            ISshKeyPair keyPair)
        {
            this.Username = username.ExpectNotNull(nameof(username));
            this.KeyPair = keyPair.ExpectNotNull(nameof(this.KeyPair));
        }

        public virtual string Prompt(
            string name,
            string instruction,
            string prompt,
            bool echo)
        {
            throw new NotImplementedException();
        }
    }
}
