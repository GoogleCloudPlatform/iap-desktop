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
using System.Net;

namespace Google.Solutions.IapDesktop.Application.Host
{
    /// <summary>
    /// Query capabilities of host OS.
    /// </summary>
    public static class OSCapabilities
    {
        private static readonly Version Windows10_1703 = new Version(10, 0, 15063, 0);
        private static readonly Version Windows11 = new Version(10, 0, 22000, 0);
        private static readonly Version WindowsServer2022 = new Version(10, 0, 20348, 0);

        public static bool IsGdiScalingSupported
        {
            get => Environment.OSVersion.Version >= Windows10_1703;
        }

        public static SecurityProtocolType SupportedTlsVersions
        {
            get
            {
                if (Environment.OSVersion.Version >= Windows11 ||
                Environment.OSVersion.Version >= WindowsServer2022)
                {
                    //
                    // Windows 2022 and Windows 11 fully support TLS 1.3:
                    // https://docs.microsoft.com/en-us/windows/win32/secauthn/protocols-in-tls-ssl--schannel-ssp-
                    //
                    return
                        SecurityProtocolType.Tls12 |
                        SecurityProtocolType.Tls11 |
                        (SecurityProtocolType)0x3000; // TLS 1.3
                }
                else
                {
                    //
                    // Windows 10 and below don't properly support TLS 1.3 yet.
                    //
                    return
                        SecurityProtocolType.Tls12 |
                        SecurityProtocolType.Tls11;
                }
            }
        }
    }
}
