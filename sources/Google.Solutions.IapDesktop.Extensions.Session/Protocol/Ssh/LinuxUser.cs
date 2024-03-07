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

using Google.Solutions.Apis.Auth;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    internal class LinuxUser
    {
        //
        // NB. This is based on the IEEE Std 1003.1-2017 for "3.437 User Name"
        //     which permits any character in the Portable Filename Character Set
        //     as long as the hyphen-minus character is not the first character.
        //
        private static readonly Regex posixUsernamePattern = new Regex("^[a-zA-Z0-9_.][a-zA-Z0-9_.-]*$");
        private const int MaxUsernameLength = 32;


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

        public static string SuggestUsername(IAuthorization authorization)
        {
            return SuggestUsername(authorization.Session);
        }

        public static string SuggestUsername(IOidcSession session)
        {
            //
            // 1. Remove all characters following and including '@'.
            // 2. Lowercase all alpha characters.
            // 3. Replace all non-alphanum characters with '_'.
            //
            var username = new string(session.Username
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

            return username;
        }
    }
}
