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

using Google.Apis.CloudOSLogin.v1.Data;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// SSH key pair for which the public key has been authorized.
    /// </summary>
    public sealed class AuthorizedKeyPair : IDisposable
    {
        public KeyAuthorizationMethods AuthorizationMethod { get; }
        public IAsymmetricKeyCredential KeyPair { get; } //TODO: rename
        public string Username { get; }

        private AuthorizedKeyPair(
            IAsymmetricKeyCredential keyPair,
            KeyAuthorizationMethods method,
            string posixUsername)
        {
            Debug.Assert(LinuxUser.IsValidUsername(posixUsername));
            Debug.Assert(method.IsSingleFlag());

            this.KeyPair = keyPair;
            this.AuthorizationMethod = method;
            this.Username = posixUsername;
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        public static AuthorizedKeyPair ForOsLoginAccount(
            IAsymmetricKeyCredential key,
            PosixAccount posixAccount)
        {
            Precondition.ExpectNotNull(key, nameof(key));
            Precondition.ExpectNotNull(posixAccount, nameof(posixAccount));

            Debug.Assert(LinuxUser.IsValidUsername(posixAccount.Username));

            return new AuthorizedKeyPair(
                key,
                KeyAuthorizationMethods.Oslogin,
                posixAccount.Username);
        }

        public static AuthorizedKeyPair ForMetadata(
            IAsymmetricKeyCredential key,
            string preferredUsername,
            bool useInstanceKeySet,
            IAuthorization authorization)
        {
            Precondition.ExpectNotNull(key, nameof(key));

            if (preferredUsername != null)
            {
                if (!LinuxUser.IsValidUsername(preferredUsername))
                {
                    throw new ArgumentException(
                        $"The username '{preferredUsername}' is not a valid username");
                }
                else
                {
                    //
                    // Use the preferred username.
                    //
                    return new AuthorizedKeyPair(
                        key,
                        useInstanceKeySet
                            ? KeyAuthorizationMethods.InstanceMetadata
                            : KeyAuthorizationMethods.ProjectMetadata,
                        preferredUsername);
                }
            }
            else
            {
                Precondition.ExpectNotNull(authorization, nameof(authorization));

                // 
                // No preferred username provided, so derive one
                // from the user's username:
                //
                var username = LinuxUser.SuggestUsername(authorization);

                return new AuthorizedKeyPair(
                    key,
                    useInstanceKeySet
                        ? KeyAuthorizationMethods.InstanceMetadata
                        : KeyAuthorizationMethods.ProjectMetadata,
                    username);
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.KeyPair.Dispose();
        }
    }
}
