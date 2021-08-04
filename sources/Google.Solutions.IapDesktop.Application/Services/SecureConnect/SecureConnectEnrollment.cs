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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
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
        // By default, use the certificate provisioned by the
        // SecureConnect native helper.
        //
        public const string DefaultDeviceCertificateSelector =
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
        // URL Endpoint Verification uses to select a device certificate,
        // cf. go/ec-sc-design.
        //
        private static readonly Uri CertificateSelectorUrl 
            = new Uri("https://secureconnect-pa.mtls.clients6.google.com/");

        private const string EnhancedKeyUsageOid = "2.5.29.37";
        private const string ClientAuthenticationKeyUsageOid = "1.3.6.1.5.5.7.3.2";

        private readonly ApplicationSettingsRepository applicationSettingsRepository;
        private readonly ICertificateStoreAdapter certificateStore;

        private static bool IsCertificateUsableForClientAuthentication(X509Certificate2 certificate)
        {
            return certificate.Extensions
                .OfType<X509EnhancedKeyUsageExtension>()
                .Where(ext => ext.Oid.Value == EnhancedKeyUsageOid)
                .SelectMany(ext => ext.EnhancedKeyUsages.Cast<Oid>())
                .Any(oid => oid.Value == ClientAuthenticationKeyUsageOid);
        }

        private SecureConnectEnrollment(
            ICertificateStoreAdapter certificateStore,
            ApplicationSettingsRepository applicationSettingsRepository)
        {
            this.applicationSettingsRepository = applicationSettingsRepository;
            this.certificateStore = certificateStore;

            // Initialize to a default state. The real initialization
            // happens in RefreshAsync().

            this.State = DeviceEnrollmentState.Disabled;
            this.Certificate = null;
        }

        private void Refresh()
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                if (!this.applicationSettingsRepository.GetSettings()
                    .IsDeviceCertificateAuthenticationEnabled.BoolValue)
                {
                    this.State = DeviceEnrollmentState.Disabled;
                    this.Certificate = null;
                    return;
                }

                //
                // Get the selector that determines how to find the device
                // certificate.
                //
                if (ChromeCertificateSelector.TryParse(
                    this.applicationSettingsRepository.GetSettings().DeviceCertificateSelector.StringValue,
                    out var selector))
                {
                    //
                    // Check if there is a certificate in the certificate store.
                    // Check computer certificates first, then user certificates.
                    //
                    var certificate = this.certificateStore.ListComputerCertitficates()
                        .Concat(this.certificateStore.ListUserCertitficates())
                        .Where(IsCertificateUsableForClientAuthentication)
                        .Where(cert => selector.IsMatch(CertificateSelectorUrl, cert))
                        .FirstOrDefault();

                    if (certificate != null)
                    {
                        //
                        // Certificate found - this does not necessarily mean that it's
                        // associated with the signed-on user, but finding out
                        // would require interacting with the undocumented APIs
                        // of the native helper. 
                        //
                        // False positives are harmless, so assume the device is enrolled.
                        //

                        ApplicationTraceSources.Default.TraceInformation("Device certificate found " +
                            "in certificate store, assuming device to be enrolled");

                        this.State = DeviceEnrollmentState.Enrolled;
                        this.Certificate = certificate;
                        return;
                    }
                }

                //
                // No certiticate found, so the device cannot be enrolled.
                // 
                ApplicationTraceSources.Default.TraceInformation(
                    "No suitable device certificate found " +
                    "in certificate store, device not enrolled");

                this.State = DeviceEnrollmentState.NotEnrolled;
                this.Certificate = null;
            }
        }

        //---------------------------------------------------------------------
        // IDeviceEnrollment.
        //---------------------------------------------------------------------

        public DeviceEnrollmentState State { get; private set; }
        public X509Certificate2 Certificate { get; private set; }

        public Task RefreshAsync(string userId)
        {
            return Task.Run(() => Refresh());
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public static async Task<SecureConnectEnrollment> GetEnrollmentAsync(
            ICertificateStoreAdapter certificateStore,
            ApplicationSettingsRepository applicationSettingsRepository,
            string userId)
        {
            var enrollment = new SecureConnectEnrollment(
                certificateStore,
                applicationSettingsRepository);
            await enrollment.RefreshAsync(userId)
                .ConfigureAwait(false);
            return enrollment;
        }
    }
}
