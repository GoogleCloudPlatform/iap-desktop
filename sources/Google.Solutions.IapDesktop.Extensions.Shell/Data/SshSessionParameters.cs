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

using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using System;
using System.Globalization;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Data
{
    /// <summary>
    /// Parameters for establishing an SSH session.
    /// </summary>
    public struct SshSessionParameters
    {
        /// <summary>
        /// Key to authenticate with.
        /// </summary>
        public AuthorizedKeyPair AuthorizedKey { get; }

        /// <summary>
        /// Terminal locale.
        /// </summary>
        public CultureInfo Language { get; }

        /// <summary>
        /// Timeout to use for SSH connections.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; }

        public SshSessionParameters(
            AuthorizedKeyPair authorizedKey,
            CultureInfo language,
            TimeSpan connectionTimeout)
        {
            this.AuthorizedKey = authorizedKey;
            this.Language = language;
            this.ConnectionTimeout = connectionTimeout;
        }
    }
}
