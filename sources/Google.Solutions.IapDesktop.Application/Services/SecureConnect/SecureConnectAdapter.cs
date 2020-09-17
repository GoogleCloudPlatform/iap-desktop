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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.SecureConnect
{
    public class SecureConnectAdapter
    {
        private const string DeviceCertIssuer = "CN=Google Endpoint Verification";

        private static string CreateSha256Thumbprint(X509Certificate2 certificate)
        {
            // NB. X509Certificate2.Thumbprint returns the SHA1, not SHA-256.
            using (var sha256 = new SHA256Managed())
            {
                var hash = sha256.ComputeHash(certificate.GetRawCertData());

                // NB. The native helper uses base 64 without padding, so remove
                // any trailing '=' is present.
                return Convert.ToBase64String(hash).Replace("=", string.Empty);
            }
        }

        private IEnumerable<X509Certificate2> GetDeviceCertificates()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                // Endpoint verification certificates have Issuer = CN = DeviceCertIssuer.
                return store.Certificates
                    .Cast<X509Certificate2>()
                    .Where(c => c.Issuer == DeviceCertIssuer)
                    .Where(c => c.Subject == DeviceCertIssuer);
            }
        }

        public Task<SecureConnectEnrollmentInfo> GetEnrollmentInfoAsync(string userId)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(userId))
            {
                return Task.Run<SecureConnectEnrollmentInfo>(() =>
                {
                    if (!SecureConnectNativeHelper.IsInstalled)
                    {
                        return SecureConnectEnrollmentInfo.NotEnrolled;
                    }

                    var helper = new SecureConnectNativeHelper();

                    // Ping helper to verify we have the right version installed.
                    helper.Ping();

                    // Check if this device is enrolled at all.
                    var shouldEnroll = helper.ShouldEnrollDevice(userId);
                    if (shouldEnroll == false)
                    {
                        // Get information about certificate.
                        var fingerprints = helper
                            .GetDeviceInfo()
                            .CertificateFingerprints
                            .ToHashSet();

                        var certificate = GetDeviceCertificates()
                            .Where(c => fingerprints.Contains(CreateSha256Thumbprint(c)))
                            .FirstOrDefault();

                        if (certificate != null)
                        {
                            return new SecureConnectEnrollmentInfo(true, certificate);
                        }
                        else
                        {
                            // Device enrolled, but no certificate found - as device
                            // certificates are not a mandatory part of an enrollment,
                            // this is a common case.

                            TraceSources.IapDesktop.TraceInformation(
                                "Device enrolled, but no device certificate provisioned");

                            return SecureConnectEnrollmentInfo.EnrolledWithoutCertificate;
                        }
                    }
                    else
                    {
                        TraceSources.IapDesktop.TraceInformation(
                            "Endpoint Verification installed, but device not enrolled");

                        return SecureConnectEnrollmentInfo.NotEnrolled;
                    }
                });
            }
        }
    }

    public class SecureConnectEnrollmentInfo
    {
        internal static SecureConnectEnrollmentInfo NotEnrolled =
            new SecureConnectEnrollmentInfo(false, null);
        internal static SecureConnectEnrollmentInfo EnrolledWithoutCertificate =
            new SecureConnectEnrollmentInfo(true, null);

        public bool IsEnrolled { get; }

        public X509Certificate2 DeviceCertificate { get; }

        public SecureConnectEnrollmentInfo(
            bool isEnrolled,
            X509Certificate2 deviceCertificate)
        {
            this.IsEnrolled = isEnrolled;
            this.DeviceCertificate = deviceCertificate;
        }
    }
}
