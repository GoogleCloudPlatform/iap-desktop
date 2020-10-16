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

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.IapDesktop.Application.Util
{
    internal static class X509CertificateExtensions
    {
        /// <summary>
        /// Calculate the SHA-256 thumbprint (X509Certificate2.Thumbprint
        /// creates the SHA-1 thumbprint)
        /// </summary>
        public static string ThumbprintSha256(this X509Certificate certificate)
        {
            using (var sha256 = new SHA256Managed())
            {
                var hash = sha256.ComputeHash(certificate.GetRawCertData());

                // NB. The native helper uses base 64 without padding, so remove
                // any trailing '=' is present.
                return Convert.ToBase64String(hash).Replace("=", string.Empty);
            }
        }
    }
}
