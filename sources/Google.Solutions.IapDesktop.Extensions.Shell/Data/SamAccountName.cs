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

using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Core.Auth;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Data
{
    internal static class SamAccountName
    {
        public static string SuggestFromGoogleEmailAddress(IAuthorization authorization)
        {
            //
            // Criteria for local Windows user accounts:
            // (1) cannot be identical to any other user or group name on the computer
            // (2) can contain up to 20 uppercase or lowercase characters
            // (3) must not contain " / \ [ ] : ; | = , + * ? < >
            // (4) must not consist solely of periods(.) or spaces.
            //
            // See https://docs.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2003/cc783323(v=ws.10)
            //
            // Rules for Google email addresses:
            // (1) must be 6-30 characters in length
            // (2) must only contain a-z, A-Z, 0-9, ., _
            //
            // That means:
            // - All Google-allowed characters are also ok for usernames.
            // - We must truncate to 20 chars.
            //

            var email = authorization.Email;

            int atIndex;
            if (!string.IsNullOrEmpty(email) && (atIndex = email.IndexOf('@')) > 0)
            {
                return email.Substring(0, Math.Min(atIndex, 20));
            }
            else
            {
                //
                // Such an email should never surface, but revert
                // to using the local Windows username then.
                //
                return Environment.UserName;
            }
        }
    }
}
