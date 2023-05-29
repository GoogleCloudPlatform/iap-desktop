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
using Google.Solutions.Ssh.Auth;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Google.Solutions.IapDesktop.Extensions.Session.Services.Ssh
{
    /// <summary>
    /// SSH key pair for which the public key has been authorized.
    /// </summary>
    public sealed class AuthorizedKeyPair : IDisposable
    {
        //
        // NB. This is the pattern used by Debian's shadow-utils.
        //
        private static readonly Regex posixUsernamePattern = new Regex("^[a-z_][a-z0-9_-]*$");
        private const int MaxUsernameLength = 32;

        public KeyAuthorizationMethods AuthorizationMethod { get; }
        public ISshKeyPair KeyPair { get; }
        public string Username { get; }

        private AuthorizedKeyPair(
            ISshKeyPair keyPair,
            KeyAuthorizationMethods method,
            string posixUsername)
        {
            Debug.Assert(IsValidUsername(posixUsername));
            Debug.Assert(method.IsSingleFlag());

            this.KeyPair = keyPair;
            this.AuthorizationMethod = method;
            this.Username = posixUsername;
        }

        private static bool IsAsciiLetter(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z');
        }

        private static bool IsAsciiLetterOrNumber(char c)
        {
            return (c >= '0' && c <= '9') || IsAsciiLetter(c);
        }

        internal static bool IsValidUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(username) &&
                username.Length > 0 &&
                username.Length <= MaxUsernameLength &&
                posixUsernamePattern.IsMatch(username);
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        public static AuthorizedKeyPair ForOsLoginAccount(
            ISshKeyPair key,
            PosixAccount posixAccount)
        {
            Precondition.ExpectNotNull(key, nameof(key));
            Precondition.ExpectNotNull(posixAccount, nameof(posixAccount));

            Debug.Assert(IsValidUsername(posixAccount.Username));

            return new AuthorizedKeyPair(
                key,
                KeyAuthorizationMethods.Oslogin,
                posixAccount.Username);
        }

        public static AuthorizedKeyPair ForMetadata(
            ISshKeyPair key,
            string preferredUsername,
            bool useInstanceKeySet,
            IAuthorization authorization)
        {
            Precondition.ExpectNotNull(key, nameof(key));

            if (preferredUsername != null)
            {
                if (!IsValidUsername(preferredUsername))
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
                // from the user's email address:
                //
                // 1. Remove all characters following and including '@'.
                // 2. Lowercase all alpha characters.
                // 3. Replace all non-alphanum characters with '_'.
                //
                var username = new string(authorization.Email
                    .Split('@')[0]
                    .ToLower()
                    .Select(c => IsAsciiLetterOrNumber(c) ? c : '_')
                    .ToArray());

                //
                // 4. Prepend with 'g' if the username does not start with an alpha character.
                //
                if (!IsAsciiLetter(username[0]))
                {
                    username = "g" + username;
                }

                //
                // 5. Truncate the username to 32 characters.
                //
                username = username.Substring(0, Math.Min(MaxUsernameLength, username.Length));

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
