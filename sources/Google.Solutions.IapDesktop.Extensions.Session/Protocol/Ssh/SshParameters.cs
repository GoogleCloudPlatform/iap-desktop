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

using System;
using System.ComponentModel;
using System.Globalization;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    public class SshParameters : SessionParametersBase
    {
        internal const ushort DefaultPort = 22;
        internal static readonly TimeSpan DefaultPublicKeyValidity = TimeSpan.FromDays(30);
        internal static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Terminal locale.
        /// </summary>
        public CultureInfo Language { get; set; } = null;

        /// <summary>
        /// Timeout to use for SSH connections.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = DefaultConnectionTimeout;

        /// <summary>
        /// Port to connect to (default: 22).
        /// </summary>
        public ushort Port { get; set; } = DefaultPort;

        /// <summary>
        /// POSIX username to log in with, only applicable when
        /// using metadata-based keys.
        /// </summary>
        public string PreferredUsername { get; set; } = null;

        /// <summary>
        /// Validity to apply when authorizing the public key.
        /// </summary>
        public TimeSpan PublicKeyValidity { get; set; } = DefaultPublicKeyValidity;
    }

    //-------------------------------------------------------------------------
    // Enums.
    //
    // NB. Numeric values must be kept unchanged as they are persisted
    // as settings.
    //
    //-------------------------------------------------------------------------

    public enum SshPublicKeyAuthentication
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Enabled
    }
}
