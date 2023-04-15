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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.SecureConnect
{
    public class SecureConnectEnrollment : IDeviceEnrollment
    {
        //
        // Pseudo-selector for the EV certificate installed by the
        // SecureConnect native helper.
        //
        internal const string DefaultDeviceCertificateSelector =
            @"{
                'filter':{
                    'ISSUER': {
                        'CN': 'Google Endpoint Verification'
                    },
                    'SUBJECT': {
                        'CN': 'Google Endpoint Verification'
                    }
                }
            }";

        //
        // URL to select a device certificate, cf. go/ec-sc-design.
        //
        private static readonly Uri CertificateSelectorUrl
            = new Uri("https://secureconnect-pa.mtls.clients6.google.com/");

        private const string EnhancedKeyUsageOid = "2.5.29.37";
        private const string ClientAuthenticationKeyUsageOid = "1.3.6.1.5.5.7.3.2";

        private SecureConnectEnrollment(
            DeviceEnrollmentState state,
            X509Certificate2 certificate)
        {
            this.State = state;
            this.Certificate = certificate;
        }

        private static bool IsCertificateUsableForClientAuthentication(
            X509Certificate2 certificate)
        {
            return certificate.Extensions
                .OfType<X509EnhancedKeyUsageExtension>()
                .Where(ext => ext.Oid.Value == EnhancedKeyUsageOid)
                .SelectMany(ext => ext.EnhancedKeyUsages.Cast<Oid>())
                .Any(oid => oid.Value == ClientAuthenticationKeyUsageOid);
        }

        //---------------------------------------------------------------------
        // IDeviceEnrollment.
        //---------------------------------------------------------------------

        public DeviceEnrollmentState State { get; private set; }
        public X509Certificate2 Certificate { get; private set; }

        public Task RefreshAsync() // TODO: Remove
        {
            return Task.CompletedTask;
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public static SecureConnectEnrollment Create(
            ICertificateStoreAdapter certificateStore,
            ApplicationSettingsRepository applicationSettingsRepository)
        {
            certificateStore.ExpectNotNull(nameof(certificateStore));
            applicationSettingsRepository.ExpectNotNull(nameof(applicationSettingsRepository));

            var settings = applicationSettingsRepository.GetSettings();
            if (!settings.IsDeviceCertificateAuthenticationEnabled.BoolValue)
            {
                return new SecureConnectEnrollment(DeviceEnrollmentState.Disabled, null);
            }

            //
            // Find the right client certificate for mTLS.
            //
            // Candidates are (in order or priority):
            //  1. Custom certiticate (from settings)
            //  2. EV default certiticate
            //  3. Enterprise certificate (from group policy)
            //
            // NB. Even if we find a certificiate, it might not be
            // associated with the signed-on user. But finding out
            // would require interacting with the undocumented APIs
            // of the native helper. False positives are harmless,
            // so err on assuming that the device is enrolled.
            //
            // NB. When looking for client certificates, Chrome only
            // considers the current user's certificate store:
            // https://source.chromium.org/chromium/chromium/src/+/main:net/ssl/client_cert_store_win.cc;l=252?q=certopenstore&ss=chromium.
            // 
            // A client certificate that only exists in the machine
            // certificate store won't be picked up by Chrome and the
            // EV extension, and thus won'e be usuable for mTLS.
            //
            // EV-provisioned certificates are also placed in the user's
            // certificate store. 
            //
            var policyBuilder = new ChromeAutoSelectCertificateForUrlsPolicy.Builder();

            //
            // Consider custom configuration and EV default certificate. These
            // take precedence over group policies.
            //
            if (ChromeCertificateSelector.TryParse(
                settings.DeviceCertificateSelector.StringValue,
                out var selector))
            {
                policyBuilder.Add(selector);
            }

            //
            // Consider group policies.
            //
            var policy = policyBuilder
                .AddGroupPoliciesForCurrentUser()
                .Build();

            //
            // Try to find a certificate that satisfies the policy.
            //
            var deviceCertificate = certificateStore.ListUserCertitficates()
                .Where(IsCertificateUsableForClientAuthentication)
                .Where(cert => policy.IsApplicable(CertificateSelectorUrl, cert))
                .FirstOrDefault();

            if (deviceCertificate != null)
            {
                ApplicationTraceSources.Default.TraceInformation(
                    "Device certificate found: {0}", 
                    deviceCertificate.Subject);

                return new SecureConnectEnrollment(DeviceEnrollmentState.Enrolled, deviceCertificate);
            }
            else
            {
                return new SecureConnectEnrollment(DeviceEnrollmentState.NotEnrolled, null);
            }
        }
    }
}
