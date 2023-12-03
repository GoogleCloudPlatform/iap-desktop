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

using Google.Solutions.Common.Util;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Cryptography;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// A platform-managed SSH credential.
    /// </summary>
    public class PlatformCredential : IAsymmetricKeyCredential
    {
        public KeyAuthorizationMethods AuthorizationMethod { get; }

        internal PlatformCredential(
            IAsymmetricKeySigner signer,
            KeyAuthorizationMethods method,
            string posixUsername)
        {
            Debug.Assert(LinuxUser.IsValidUsername(posixUsername));
            Debug.Assert(method.IsSingleFlag());

            this.Signer = signer;
            this.AuthorizationMethod = method;
            this.Username = posixUsername;
        }

        //---------------------------------------------------------------------
        // IAsymmetricKeyCredential.
        //---------------------------------------------------------------------

        public string Username { get; }

        public IAsymmetricKeySigner Signer { get; }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string ToString()
        {
            return $"{this.Username} (using {this.Signer.PublicKey.Type}, " +
                $"authorized using {this.AuthorizationMethod})";
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.Signer.Dispose();
        }
    }
}
