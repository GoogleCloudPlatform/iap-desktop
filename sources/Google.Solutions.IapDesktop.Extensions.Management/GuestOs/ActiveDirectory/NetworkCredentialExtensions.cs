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

namespace Google.Solutions.IapDesktop.Extensions.Management.GuestOs.ActiveDirectory
{
    internal static class NetworkCredentialExtensions
    {
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
    }
}
