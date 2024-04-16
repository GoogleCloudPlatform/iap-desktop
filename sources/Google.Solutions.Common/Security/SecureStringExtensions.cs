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

using System.Net;
using System.Security;

namespace Google.Solutions.Common.Security
{
    public static class SecureStringExtensions
    {
        public static readonly SecureString Empty = FromClearText(string.Empty);

        /// <summary>
        /// Convert SecureString to plain text.
        /// </summary>
        public static string AsClearText(this SecureString secureString)
        {
            return new NetworkCredential(string.Empty, secureString).Password;
        }

        /// <summary>
        /// Convert a string to a SecureString.
        /// </summary>
        /// <returns>SecureString. Empty string if input was null.</returns>
        public static SecureString FromClearText(string? plaintextString)
        {
            return new NetworkCredential(string.Empty, plaintextString).SecurePassword;
        }
    }
}
