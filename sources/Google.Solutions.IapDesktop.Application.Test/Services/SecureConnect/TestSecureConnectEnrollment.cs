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

using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.SecureConnect
{
    [TestFixture]
    public class TestSecureConnectEnrollment : ApplicationFixtureBase
    {
        //
        // New-SelfSignedCertificate `
        //   -Type Custom `
        //   -Subject "CN=Example" `
        //   -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
        //   -KeyUsage DigitalSignature `
        //   -KeyAlgorithm RSA `
        //   -KeyLength 1024 `
        //   -CertStoreLocation Cert:\CurrentUser\My\ `
        //   -NotAfter 01/01/2030
        //
        private readonly X509Certificate2 CustomCertificateForClientAuth =
            CertificateUtil.CertificateFromPem(
                @"-----BEGIN CERTIFICATE-----
                MIIB7zCCAVigAwIBAgIQGZbilgXuAIBAGsCHQmoLUzANBgkqhkiG9w0BAQsFADAS
                MRAwDgYDVQQDDAdFeGFtcGxlMB4XDTIxMDgwNDE0MDYzN1oXDTI5MTIzMTIyMDAw
                MFowEjEQMA4GA1UEAwwHRXhhbXBsZTCBnzANBgkqhkiG9w0BAQEFAAOBjQAwgYkC
                gYEAyiSVqIQQHnp2eWXeMk9b99mI1uaLezmEDxLfpRKfmYjmCk6vxSEfP3X0Yjn/
                GtpUa6i2QteFFRlXa6fakQ/+624N4de1h6ivfHYLYt3aQ8kQzGZdbDZKOx5H6hOg
                nJxxwj/1gKG21zggIU12OISLOCqQTRUOV4w7TT6jT2fh0zECAwEAAaNGMEQwDgYD
                VR0PAQH/BAQDAgeAMBMGA1UdJQQMMAoGCCsGAQUFBwMCMB0GA1UdDgQWBBRpFdCK
                bmk/vmNypIFu+LX80eG9+zANBgkqhkiG9w0BAQsFAAOBgQAFPZflBc8PM7ti6peY
                sC750w9kZatTWU3R2Aog/8cxgT0Gqmw5YoykwoteH79QhuRbl+vBJWV7/5EIf08p
                H/hTi2UiUx9pdTdSBU+ZWx518igf6asXyAHkE7xCCbtyTIw86T7NDSFSzjf2r755
                xCeL91zP2UUgSWkYWzJFxHXsuQ==
                -----END CERTIFICATE-----");

        //
        // New-SelfSignedCertificate `
        //     -Type Custom `
        //     -Subject "CN=Example" `
        //     -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1") `
        //     -KeyUsage DigitalSignature `
        //     -KeyAlgorithm RSA `
        //     -KeyLength 1024 `
        //     -CertStoreLocation Cert:\CurrentUser\My\ `
        //     -NotAfter 01/01/2030
        //
        private readonly X509Certificate2 CustomCertificateForServerAuth =
            CertificateUtil.CertificateFromPem(
                @"-----BEGIN CERTIFICATE-----
                MIIB7zCCAVigAwIBAgIQFZEofVzvrbBEPLoXkVQdTDANBgkqhkiG9w0BAQsFADAS
                MRAwDgYDVQQDDAdFeGFtcGxlMB4XDTIxMDgwNDE0MTAwNFoXDTI5MTIzMTIyMDAw
                MFowEjEQMA4GA1UEAwwHRXhhbXBsZTCBnzANBgkqhkiG9w0BAQEFAAOBjQAwgYkC
                gYEAwEpyeVqiki6PPvNTx269ht1Re0tDjpLSKshC2s010D/vL9Lz8eI1AKHwcjr3
                8SLFaQLHMUYzOycNytlucklVrzZeqAS2VDXTPld7MvqBCF/Yzdp/LQIuNBFhBmPs
                /eRRQIHLxeHcrkgxIhfqPLMB+JyuLbV7gYwSIuoysEMhTzUCAwEAAaNGMEQwDgYD
                VR0PAQH/BAQDAgeAMBMGA1UdJQQMMAoGCCsGAQUFBwMBMB0GA1UdDgQWBBTgsp6E
                gohGE7Q9f/KXWpVKUXVATzANBgkqhkiG9w0BAQsFAAOBgQCD2qd//Vb+jIuZUnON
                GfddhRoj8T8P1m0f1INtimGLgCadzE3MSx6sh62jvoX4om8pA3SBwwSgTvcu7ykS
                POfcworiaxQhTawWnXI+YrtzlgEUtgWDux6mbQyg0cY//qkDfJ9SxkljW02AQrjU
                dMXBlI/FWQFjjIr4X/oGqwEijA==
                -----END CERTIFICATE-----");
        
        //
        // New-SelfSignedCertificate `
        //     -Type Custom `
        //     -Subject "CN=Google Endpoint Verification" `
        //     -DnsName "CN=Google Endpoint Verification" `
        //     -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
        //     -KeyUsage DigitalSignature `
        //     -KeyAlgorithm RSA `
        //     -KeyLength 1024 `
        //     -CertStoreLocation Cert:\CurrentUser\My\ `
        //     -NotAfter 01/01/2030
        //
        private readonly X509Certificate2 EndpointVerificationCertificate =
            CertificateUtil.CertificateFromPem(
                @"-----BEGIN CERTIFICATE-----
                MIICRTCCAa6gAwIBAgIQHCcsk4KAQYZMDN2kkJHX3jANBgkqhkiG9w0BAQsFADAn
                MSUwIwYDVQQDDBxHb29nbGUgRW5kcG9pbnQgVmVyaWZpY2F0aW9uMB4XDTIxMDgw
                NDE0MTIzMVoXDTI5MTIzMTIyMDAwMFowJzElMCMGA1UEAwwcR29vZ2xlIEVuZHBv
                aW50IFZlcmlmaWNhdGlvbjCBnzANBgkqhkiG9w0BAQEFAAOBjQAwgYkCgYEAyQ+3
                2Ikn6lvjbvPjRZOhXnugVqal8b4ik6th1C+1Pdr2G58CQ5ukQujLbgndAYU4Nzu3
                NOjMyoDRHL4VgdnlX88THLh9UGVn/djOg5eclmxKFrHV6C3YumnwA3q4RrTsQZNl
                E7PoOkXT0rBah9mROgYnKM5apH1ukqCWIGLXMcUCAwEAAaNyMHAwDgYDVR0PAQH/
                BAQDAgeAMCoGA1UdEQQjMCGCH0NOPUdvb2dsZSBFbmRwb2ludCBWZXJpZmljYXRp
                b24wEwYDVR0lBAwwCgYIKwYBBQUHAwIwHQYDVR0OBBYEFLcmz0PgiP2nhXhIPsV1
                Jar6VRHIMA0GCSqGSIb3DQEBCwUAA4GBADa0ecz9BxKAI5a6kQ0IkU748qO8++9/
                mPFMXNPy6T5aJu6lUVSkWk3h8za4d2usrHijFkBSB9aZJpUTfQm/M4KfCsXs2Ww5
                Jj6M3Q4DyrRWuPMHjm0LWfF9bd7AcMfSW/to+NEICmAm6FHCKQLxmJK4tGunirSm
                QMtVvGZ0U6Ra
                -----END CERTIFICATE-----");

        private const string TestKeyPath = @"Software\Google\__Test";
        private ApplicationSettingsRepository settingsRepository;
        private const string SampleUserId = "unused";

        [SetUp]
        public void SetUp()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var settingsKey = hkcu.CreateSubKey(TestKeyPath);

            this.settingsRepository = new ApplicationSettingsRepository(settingsKey, null, null);
        }

        //---------------------------------------------------------------------
        // DCA disabled.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDcaIsDisabledInSettings_ThenStateIsNotInstalled()
        {
            // Disable DCA.
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            var chromePolicy = new Mock<IChromePolicy>();
            chromePolicy.Setup(p => p.GetAutoSelectCertificateForUrlsPolicy(
                    It.IsAny<Uri>()))
                .Returns(cert => false);

            var certificateStore = new Mock<ICertificateStoreAdapter>();
            var enrollment = await SecureConnectEnrollment.GetEnrollmentAsync(
                    certificateStore.Object,
                    chromePolicy.Object,
                    this.settingsRepository,
                    SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.Disabled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);

            certificateStore.Verify(s => s.ListComputerCertitficates(), Times.Never);
            certificateStore.Verify(s => s.ListUserCertitficates(), Times.Never);
        }


        [Test]
        public async Task WhenDisablingDca_ThenRefreshUpdatesStateToDisabled()
        {
            // Enable DCA.
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var chromePolicy = new Mock<IChromePolicy>();
            chromePolicy.Setup(p => p.GetAutoSelectCertificateForUrlsPolicy(
                    It.IsAny<Uri>()))
                .Returns(cert => false);

            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListUserCertitficates())
                .Returns(new[] { EndpointVerificationCertificate });

            var enrollment = await SecureConnectEnrollment.GetEnrollmentAsync(
                    certificateStore.Object,
                    chromePolicy.Object,
                    this.settingsRepository,
                    SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.Enrolled, enrollment.State);
            Assert.IsNotNull(enrollment.Certificate);
            Assert.AreEqual("CN=Google Endpoint Verification", enrollment.Certificate.Subject);

            // Disable DCA.
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = false;
            this.settingsRepository.SetSettings(settings);

            await enrollment.RefreshAsync(SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.Disabled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }

        //---------------------------------------------------------------------
        // Default certificate selector.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsingDefaultCertificateSelectorButNoCertificateInStore_ThenStateIsNotEnrolled()
        {
            // Enable DCA.
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var chromePolicy = new Mock<IChromePolicy>();
            chromePolicy.Setup(p => p.GetAutoSelectCertificateForUrlsPolicy(
                    It.IsAny<Uri>()))
                .Returns(cert => false);

            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListUserCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());
            certificateStore.Setup(s => s.ListComputerCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());

            var enrollment = await SecureConnectEnrollment.GetEnrollmentAsync(
                    certificateStore.Object,
                    chromePolicy.Object,
                    this.settingsRepository,
                    SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.NotEnrolled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }

        [Test]
        public async Task WhenUsingDefaultCertificateSelectorAndCertificateFoundInUserStore_ThenStateIsEnrolled()
        {
            // Enable DCA.
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var chromePolicy = new Mock<IChromePolicy>();
            chromePolicy.Setup(p => p.GetAutoSelectCertificateForUrlsPolicy(
                    It.IsAny<Uri>()))
                .Returns(cert => false);

            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListComputerCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());
            certificateStore.Setup(s => s.ListUserCertitficates())
                .Returns(new[] { EndpointVerificationCertificate });

            var enrollment = await SecureConnectEnrollment.GetEnrollmentAsync(
                    certificateStore.Object,
                    chromePolicy.Object,
                    this.settingsRepository,
                    SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.Enrolled, enrollment.State);
            Assert.IsNotNull(enrollment.Certificate);
            Assert.AreEqual("CN=Google Endpoint Verification", enrollment.Certificate.Subject);
        }

        [Test]
        public async Task WhenUsingDefaultCertificateSelectorAndCertificateOnlyInComputerStore_ThenStateIsNotEnrolled()
        {
            // Enable DCA.
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var chromePolicy = new Mock<IChromePolicy>();
            chromePolicy.Setup(p => p.GetAutoSelectCertificateForUrlsPolicy(
                    It.IsAny<Uri>()))
                .Returns(cert => false);

            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListComputerCertitficates())
                .Returns(new[] { EndpointVerificationCertificate });
            certificateStore.Setup(s => s.ListUserCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());

            var enrollment = await SecureConnectEnrollment.GetEnrollmentAsync(
                    certificateStore.Object,
                    chromePolicy.Object,
                    this.settingsRepository,
                    SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.NotEnrolled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }

        //---------------------------------------------------------------------
        // Custom certificate selector.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsingCustomCertificateSelectorButNoCertificateInStore_ThenStateIsNotEnrolled()
        {
            // Enable DCA.
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            settings.DeviceCertificateSelector.StringValue =
                @"{
                    'filter':{
                        'SUBJECT': {
                            'CN': 'Foo'
                        }
                    }
                }";
            this.settingsRepository.SetSettings(settings);

            var chromePolicy = new Mock<IChromePolicy>();
            chromePolicy.Setup(p => p.GetAutoSelectCertificateForUrlsPolicy(
                    It.IsAny<Uri>()))
                .Returns(cert => false);

            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListUserCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());
            certificateStore.Setup(s => s.ListComputerCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());

            var enrollment = await SecureConnectEnrollment.GetEnrollmentAsync(
                    certificateStore.Object,
                    chromePolicy.Object,
                    this.settingsRepository,
                    SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.NotEnrolled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }

        [Test]
        public async Task WhenUsingCustomCertificateSelectorButCertificateDoesNotPermitClientAuth_ThenStateIsNotEnrolled()
        {
            // Enable DCA.
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            settings.DeviceCertificateSelector.StringValue =
                @"{
                    'filter':{
                        'SUBJECT': {
                            'CN': 'Example'
                        }
                    }
                }";
            this.settingsRepository.SetSettings(settings);

            var chromePolicy = new Mock<IChromePolicy>();
            chromePolicy.Setup(p => p.GetAutoSelectCertificateForUrlsPolicy(
                    It.IsAny<Uri>()))
                .Returns(cert => false);

            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListUserCertitficates())
                .Returns(new[] { CustomCertificateForServerAuth });
            certificateStore.Setup(s => s.ListComputerCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());

            var enrollment = await SecureConnectEnrollment.GetEnrollmentAsync(
                    certificateStore.Object,
                    chromePolicy.Object,
                    this.settingsRepository,
                    SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.NotEnrolled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }

        [Test]
        public async Task WhenUsingCustomCertificateSelectorAndCertificateFoundInUserStore_ThenStateIsEnrolled()
        {
            // Enable DCA.
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            settings.DeviceCertificateSelector.StringValue =
                @"{
                    'filter':{
                        'SUBJECT': {
                            'CN': 'Example'
                        }
                    }
                }";
            this.settingsRepository.SetSettings(settings);

            var chromePolicy = new Mock<IChromePolicy>();
            chromePolicy.Setup(p => p.GetAutoSelectCertificateForUrlsPolicy(
                    It.IsAny<Uri>()))
                .Returns(cert => false);

            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListComputerCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());
            certificateStore.Setup(s => s.ListUserCertitficates())
                .Returns(new[] { CustomCertificateForClientAuth });

            var enrollment = await SecureConnectEnrollment.GetEnrollmentAsync(
                    certificateStore.Object,
                    chromePolicy.Object,
                    this.settingsRepository,
                    SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.Enrolled, enrollment.State);
            Assert.IsNotNull(enrollment.Certificate);
            Assert.AreEqual("CN=Example", enrollment.Certificate.Subject);
        }

        //---------------------------------------------------------------------
        // Chrome policies.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenChromePolicySetAndCertificateFoundInUserStore_ThenStateIsEnrolled()
        {
            // Enable DCA.
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var chromePolicy = new Mock<IChromePolicy>();
            chromePolicy.Setup(p => p.GetAutoSelectCertificateForUrlsPolicy(
                    It.Is<Uri>(url => url.ToString() == "https://secureconnect-pa.mtls.clients6.google.com/")))
                .Returns(cert => cert.Subject == "CN=Example");

            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListComputerCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());
            certificateStore.Setup(s => s.ListUserCertitficates())
                .Returns(new[] { CustomCertificateForClientAuth });

            var enrollment = await SecureConnectEnrollment.GetEnrollmentAsync(
                    certificateStore.Object,
                    chromePolicy.Object,
                    this.settingsRepository,
                    SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.Enrolled, enrollment.State);
            Assert.IsNotNull(enrollment.Certificate);
            Assert.AreEqual("CN=Example", enrollment.Certificate.Subject);
        }

        [Test]
        public async Task WhenNoCustomCertificateSelectorAndChromePolicyNotFound_ThenStateIsNotEnrolled()
        {
            // Enable DCA.
            var settings = this.settingsRepository.GetSettings();
            settings.IsDeviceCertificateAuthenticationEnabled.BoolValue = true;
            this.settingsRepository.SetSettings(settings);

            var chromePolicy = new Mock<IChromePolicy>();
            chromePolicy.Setup(p => p.GetAutoSelectCertificateForUrlsPolicy(
                    It.Is<Uri>(url => url.ToString() == "https://secureconnect-pa.mtls.clients6.google.com/")))
                .Returns(cert => cert.Subject == "CN=ThisDoesNotExistInStore");

            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListComputerCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());
            certificateStore.Setup(s => s.ListUserCertitficates())
                .Returns(Enumerable.Empty<X509Certificate2>());

            var enrollment = await SecureConnectEnrollment.GetEnrollmentAsync(
                    certificateStore.Object,
                    chromePolicy.Object,
                    this.settingsRepository,
                    SampleUserId)
                .ConfigureAwait(true);

            Assert.AreEqual(DeviceEnrollmentState.NotEnrolled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }
    }
}
