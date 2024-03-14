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

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    public class AppProtocolParameters
    {
        internal AppProtocolParameters()
        {
        }

        /// <summary>
        /// Timeout for connecting transport.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Preferred username.
        /// </summary>
        public string? PreferredUsername { get; set; } = null;

        /// <summary>
        /// Determines whether to use Windows authentication/network level
        /// authentication. Supported by some client apps such as SSMS,
        /// other client apps might ignore this setting.
        /// </summary>
        public AppNetworkLevelAuthenticationState NetworkLevelAuthentication { get; set; }
            = AppNetworkLevelAuthenticationState._Default;
    }

    //-------------------------------------------------------------------------
    // Enums.
    //
    // NB. Numeric values must be kept unchanged as they are persisted as settings.
    //-------------------------------------------------------------------------

    public enum AppNetworkLevelAuthenticationState
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Enabled
    }
}
