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
using Google.Apis.Util;
using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Util;
using Google.Solutions.Ssh;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh
{
    public sealed class AuthorizedKey : IDisposable
    {
        //
        // NB. This is the pattern used by Debian's shadow-utils.
        //
        private static readonly Regex posixUsernamePattern = new Regex("^[a-z_][a-z0-9_-]*$");
        private const int MaxUsernameLength = 32;

        public AuthorizeKeyMethods AuthorizationMethod { get; }
        public ISshKey Key { get; }
        public string Username { get; }

        private AuthorizedKey(
            ISshKey key,
            AuthorizeKeyMethods method,
            string posixUsername)
        {
            Debug.Assert(IsValidUsername(posixUsername));
            Debug.Assert(method.IsSingleFlag());

            this.Key = key;
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

        private static bool IsValidUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(username) &&
                username.Length > 0 &&
                username.Length <= MaxUsernameLength &&
                posixUsernamePattern.IsMatch(username);
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        public static AuthorizedKey ForOsLoginAccount(
            ISshKey key,
            PosixAccount posixAccount)
        {
            Utilities.ThrowIfNull(key, nameof(key));
            Utilities.ThrowIfNull(posixAccount, nameof(posixAccount));

            Debug.Assert(IsValidUsername(posixAccount.Username));

            return new AuthorizedKey(
                key,
                AuthorizeKeyMethods.Oslogin,
                posixAccount.Username);
        }

        public static AuthorizedKey ForMetadata(
            ISshKey key,
            string preferredUsername,
            bool useInstanceKeySet,
            IAuthorization authorization)
        {
            Utilities.ThrowIfNull(key, nameof(key));

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
                    return new AuthorizedKey(
                        key,
                        useInstanceKeySet 
                            ? AuthorizeKeyMethods.InstanceMetadata
                            : AuthorizeKeyMethods.ProjectMetadata,
                        preferredUsername);
                }
            }
            else
            {
                Utilities.ThrowIfNull(authorization, nameof(authorization));

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

                return new AuthorizedKey(
                    key,
                    useInstanceKeySet
                        ? AuthorizeKeyMethods.InstanceMetadata
                        : AuthorizeKeyMethods.ProjectMetadata,
                    username);
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.Key.Dispose();
        }
    }
}
