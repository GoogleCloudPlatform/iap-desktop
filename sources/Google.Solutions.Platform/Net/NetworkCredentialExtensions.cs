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

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Google.Solutions.Platform.Net
{
    public static class NetworkCredentialExtensions
    {
        /// <summary>
        /// Normalize username to user@domain or DOMAIN\user format.
        /// </summary>
        public static NetworkCredential Normalize(
            this NetworkCredential credential)
        {
            if (string.IsNullOrEmpty(credential.UserName))
            {
                throw new ArgumentException("username");
            }
            else if (credential.UserName.Contains('@'))
            {
                //
                // UPN format: username@domain.
                //
                // Leave as is, but clear domain field.
                //
                Debug.Assert(string.IsNullOrEmpty(credential.Domain));
                return new NetworkCredential(
                    credential.UserName,
                    credential.Password);
            }
            else if (credential.UserName.Contains('\\'))
            {
                //
                // NetBIOS format: domain\username.
                //
                // Leave as is, but clear domain field.
                //
                Debug.Assert(string.IsNullOrEmpty(credential.Domain));
                return new NetworkCredential(
                    credential.UserName,
                    credential.Password);
            }
            else if (!string.IsNullOrEmpty(credential.Domain))
            {
                //
                // Convert to NetBIOS format: domain\username.
                //
                return new NetworkCredential(
                    $@"{credential.Domain}\{credential.UserName}",
                    credential.Password);
            }
            else
            {
                //
                // Convert to NetBIOS format: domain\username.
                //
                return new NetworkCredential(
                    $@"localhost\{credential.UserName}",
                    credential.Password);
            }
        }

        /// <summary>
        /// Determine if the credential uses user@domain notation.
        /// </summary>
        public static bool IsUpnFormat(this NetworkCredential credential)
        {
            return credential.UserName != null && credential.UserName.Contains("@");
        }

        /// <summary>
        /// Determine if the credential uses DOMAIN\user notation.
        /// </summary>
        public static bool IsNetBiosFormat(this NetworkCredential credential)
        {
            return !IsUpnFormat(credential);
        }

        /// <summary>
        /// Determine if the credential specifies a domain or specific hostname.
        /// </summary>
        public static bool IsDomainOrHostQualified(this NetworkCredential credential)
        {
            var trimmedUsername = credential.UserName?.Trim();
            var trimmedDomain = credential.Domain?.Trim();

            if (trimmedUsername == null || trimmedUsername.Length == 0)
            {
                return false;
            }
            else if (!string.IsNullOrEmpty(trimmedDomain) && trimmedDomain != ".")
            {
                //
                // Domain specified separately.
                //
                return true;
            }
            else if (trimmedUsername.Contains('\\') && trimmedUsername[0] != '.')
            {
                //
                // NetBIOS format: domain\username.
                //
                return true;
            }
            else if (trimmedUsername.Contains('@'))
            {
                //
                // UPN format: username@domain.
                //
                // Leave as is, but clear domain field.
                //
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Return the DNS or NetBios domain.
        /// </summary>
        public static string GetDomainComponent(this NetworkCredential credential)
        {
            if (credential.UserName.IndexOf('@') is var atIndex && atIndex > 0)
            {
                //
                // UPN format: username@domain.
                //
                return credential.UserName.Substring(atIndex + 1);
            }
            else if (credential.UserName.IndexOf('\\') is var backslashIndex && backslashIndex > 0)
            {
                //
                // NetBIOS format: domain\username.
                //
                return credential.UserName.Substring(0, backslashIndex);
            }
            else
            {
                //
                // Decomposed format.
                //
                return credential.Domain;
            }
        }

        /// <summary>
        /// Return the user component of the UPN or NetBios username.
        /// </summary>
        public static string GetUserComponent(this NetworkCredential credential)
        {
            if (credential.UserName.IndexOf('@') is var atIndex && atIndex > 0)
            {
                //
                // UPN format: username@domain.
                //
                return credential.UserName.Substring(0, atIndex);
            }
            else if (credential.UserName.IndexOf('\\') is var backslashIndex && backslashIndex > 0)
            {
                //
                // NetBIOS format: domain\username.
                //
                return credential.UserName.Substring(backslashIndex + 1);
            }
            else
            {
                //
                // Decomposed format.
                //
                return credential.UserName;
            }
        }
    }
}
