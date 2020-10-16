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
using Google.Solutions.IapDesktop.Application.Util;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.SecureConnect
{
    [TestFixture]
    public class TestSecureConnectEnrollment : FixtureBase
    {
        //
        // Self-signed certificate, created using:
        //   New-SelfSignedCertificate -Subject "Example" `
        //     -CertStoreLocation Cert:\CurrentUser\My\ -NotAfter 01/01/2030
        // then exported as PFX, and base64-encoded:
        //   & certutil.exe -encode .\cert.pfx cert.pfx.txt
        //
        private const string ExampleCertitficate = @"
            MIIJwAIBAzCCCXwGCSqGSIb3DQEHAaCCCW0EgglpMIIJZTCCBgAGCSqGSIb3DQEH
            AaCCBfEEggXtMIIF6TCCBeUGCyqGSIb3DQEMCgECoIIE/jCCBPowHAYKKoZIhvcN
            AQwBAzAOBAhGyxG118BUAAICB9AEggTYsa/K6uD5Toq8F7bbP+7JGzFdWMSagnwA
            YO8Gh3Y4tSlugypot+V8YXZnB6rYpn/7OCPytfzgt5p6J70dZFENd7DGZq8EFuU+
            bsRT0U1kmEltuUUiQgjOvgQQI/I+2rKXLwa1iSJG+/Hf9ag6/YCSQWj8njaIyyyo
            P2OP8NB3spAdBQVE76Miekj6NJ66cgTFkHEzJBz4Du8g14ZoQwr+RWcPYEbszTJC
            H0A2MGSbhbbhSo/ujUc8C3JSBkj8uC9b7RMBbM6sCnKzZSqRsQzzM8xUkRGFyrAX
            duB4XniiguEfTI16/gasz8fhcVt+tQdU7MM/fDenZShsUSefiofMJxP9w6lWflO7
            FBeyDK+yIhx/IQbKD7Z8mFmS+siVBqv8LmtSV3kqe0ymnpi7tOdDFqluGgpg7nmt
            K5kqRe1BhKIARrOxP59r3tF2dsdcALZBIR1ThbUBrxXsHozFSqR7R0yrroTeaip8
            +9XtT31Ueu//nc8IgD1C6vC5my54N26/XGuqettXb+UfggADCgMApkiETtUyG2l7
            0DhEj5EgO00mca03c3Cj6zN6bZZN1AX29ZgEAZNIYi2acZpTHJWjFzrQ3zX9+Bh5
            V6TgG4GDRrCylg6YsppPtlIxxe5aiU/wXxo6NzKOWi4rCXZT9DbFmFqU8XR9GPaQ
            zSAbvx1BZ4K+kxC/MyEgAILLYTzln/UY6WFms4uwytXJ0IAJoGPXhKj8IvaV0vhd
            FtzB6K6KknNzh0uIKYDrHdlOgeydFSdmFlmKFqmyc+91GWutZxAqQhlncuyNs2hk
            pdjSU+QjYBdHS5ScNiU63ynrrcLxmp6B1J100Bfk5OdflGBOo0FGguyKHx5anmNH
            8YjyN9v4VaxXnbSxsdVe4rfLM5PLpnjyS53HnVXbEFl4XdZYfDdZEM9hHMm14ThL
            JWhJHM/PlBaxkNMHVdRS8D02e6IsWoEqXNtSIjaEnSvbQHptt4MnEQrLOgdZi0co
            MPkOZCX7aZzYO6GI98NEFYoz859Zfx6tdPnPvdsTr20EB131ieRE6pU0n3golv0Q
            /QqbOsCUJErKsrBmgFfMhpaczy6X6mnksfZp6KS/QDcEg/TZqwZWJ1sz6ETp0U/V
            GSEKkuXy6pwoIIefPJNzGGCxoorGZLjaVs1K8mcT1q/arrCSND5EPQPFdSY87eId
            AhUPcNJYOCShhKlkjyCITq0xfgz7etNyBoPTlTgwAwoEX2To7OTu427YL4hv89LY
            hrNbB6zXQDw35w2R2MhBsGR7gjvVrApZiZxrD06jCdadXlP2JXWxIHtlcx0/P8/x
            ZSPuRrrFfj5pDqI8pQunZ6p5lp4WrMYWD/EhxeGWueE5oYRDgNxlzkafQgUkTQMD
            CusRjjY0fRaNEP3P5F97rcoPkoFt83yVDI/Arw9360nW1tZVsnf1zTBxF4yEV1xF
            y5NZFypMYxhRcFh/tS1vwVx29c39EvkNCmDkLw7J3Db4xsEjsHZGILRCJBhMW1FO
            7dBbO0FYXrCzFCXEDpxstHB0FQi4dgIIw1dnRV3Cj4aPHX9bTrrtdqVB1p2xgqkE
            dY5IyuB58luKa18zdfuCkwTKvdwGmTyUx36pTVq6crIP65kHSuc+TBrGQK2xZqNy
            i76ouRCN4EgxwukIBaCFOzGB0zATBgkqhkiG9w0BCRUxBgQEAQAAADBdBgkqhkiG
            9w0BCRQxUB5OAHQAZQAtADYAZgBmADMAZABlADkANgAtAGUANAAzADAALQA0ADYA
            NAAyAC0AYgA1ADgAZAAtADcANgAxADgAMABjAGIAMABkAGIAYgA2MF0GCSsGAQQB
            gjcRATFQHk4ATQBpAGMAcgBvAHMAbwBmAHQAIABTAG8AZgB0AHcAYQByAGUAIABL
            AGUAeQAgAFMAdABvAHIAYQBnAGUAIABQAHIAbwB2AGkAZABlAHIwggNdBgkqhkiG
            9w0BBwGgggNOBIIDSjCCA0YwggNCBgsqhkiG9w0BDAoBA6CCAxowggMWBgoqhkiG
            9w0BCRYBoIIDBgSCAwIwggL+MIIB5qADAgECAhB6xUcyIjnGlk0xkWpVALOuMA0G
            CSqGSIb3DQEBCwUAMBIxEDAOBgNVBAMMB0V4YW1wbGUwHhcNMjAxMDE1MTE0NTI1
            WhcNMjkxMjMxMjIwMDAwWjASMRAwDgYDVQQDDAdFeGFtcGxlMIIBIjANBgkqhkiG
            9w0BAQEFAAOCAQ8AMIIBCgKCAQEA0wDpFeh1jTX1xkLPm44VRKzXy0oy5V+U3p8c
            O1vB1fXyCHRZO/SDDGG8DQBIbyvchjpFL0dn3mLBuFx0DbdGROuLtJsnAdPNOV4x
            wCy66BfbJLKK37SzxO+KlyOubailwSXkeyN+zcdlJvzkJ/hmwR7qCgFfJTtamKcy
            +0yW/hBufsxoybOc2qhO+Bh2RdDJpoxUaAFYw/I7LqdG7mv2XRTIdnc2qPMZJ8+7
            U+0uUbin3KH2QBSRTUfo+UtfUNg2UYMPKwWvTeiuNnwECKqvFyrZ7qCAndD15dHi
            7bZNfJvvpU3EHYSjDsQXCzIrlBvY11ShiWkSDjUHZL7qUZRLoQIDAQABo1AwTjAO
            BgNVHQ8BAf8EBAMCBaAwHQYDVR0lBBYwFAYIKwYBBQUHAwIGCCsGAQUFBwMBMB0G
            A1UdDgQWBBT+w3X6UQ3WW5kTGzAMCiq1CpV1uDANBgkqhkiG9w0BAQsFAAOCAQEA
            RJj/54ctBdSqFNODIPiZad+pMqoACRmSznfKT6bt8ocfk326eXnRGN/BObw+Yr33
            z1TKRjEaPYPBzyzhcxg/VRndW4yGV1zNUU7DtRBP+0iY6JpvoBPA15Qlcc1wu0kx
            Ijj2bfwWvTPTZuHk2fc9jYp4Hi9jmxs+vcDhSPYWoD8GqA9ltg+rOJXvdiLOGVP6
            A29li216Vl94XtxmSWHZHqyrMDUDRBMkCjOaT/CZDmwfvlTY/BTODwpsq45Pbbpm
            kKe5HxLPBf2IlNugltuGHJ9BDrhx2b8Bg7+L+B+4qdnC2mr538HoHZs2XDTryB2c
            5QcupsOHJ9hOby6TKkN1LDEVMBMGCSqGSIb3DQEJFTEGBAQBAAAAMDswHzAHBgUr
            DgMCGgQUvB5xKdRFL/tU9CwlmVl0ULzjVOIEFOWQVSDrZgZyO7TlwcQK+GEH5G16
            AgIH0A==
            ";
        private const string ExampleCertitficateSubject = "CN=Example";
        private readonly X509Certificate2 ExampleCertificate =
            new X509Certificate2(Convert.FromBase64String(ExampleCertitficate), "example");

        [Test]
        public async Task WhenNotInstalled_ThenStateIsNotInstalled()
        {
            var certificateStore = new Mock<ICertificateStoreAdapter>();
            var adapter = new Mock<ISecureConnectAdapter>();
            adapter.SetupGet(a => a.IsInstalled).Returns(false);

            var enrollment = await SecureConnectEnrollment.CreateEnrollmentAsync(
                adapter.Object,
                certificateStore.Object,
                "111");

            Assert.AreEqual(DeviceEnrollmentState.NotInstalled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);

            certificateStore.Verify(s => s.ListCertitficates(
                    It.IsAny<string>(),
                    It.IsAny<string>()), 
                Times.Never);
        }

        [Test]
        public async Task WhenUserIdDoesNotHaveDeviceEnrolled_ThenStateIsNotEnrolled()
        {
            var certificateStore = new Mock<ICertificateStoreAdapter>();
            var adapter = new Mock<ISecureConnectAdapter>();
            adapter.SetupGet(a => a.IsInstalled).Returns(true);
            adapter.Setup(a => a.IsDeviceEnrolledForUser(
                    It.Is<string>(id => id == "111")))
                .Returns(false);

            var enrollment = await SecureConnectEnrollment.CreateEnrollmentAsync(
                adapter.Object,
                certificateStore.Object,
                "111");

            Assert.AreEqual(DeviceEnrollmentState.NotEnrolled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);

            certificateStore.Verify(s => s.ListCertitficates(
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Test]
        public async Task WhenUserIdHasDeviceEnrolledButCertificateMissingInStore_ThenStateIsEnrolledWithoutCertificate()
        {
            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListCertitficates(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Enumerable.Empty<X509Certificate2>());

            var deviceInfo = new Mock<ISecureConnectDeviceInfo>();
            deviceInfo.SetupGet(i => i.CertificateThumbprints)
                .Returns(new[] { "thumb" });

            var adapter = new Mock<ISecureConnectAdapter>();
            adapter.SetupGet(a => a.IsInstalled).Returns(true);
            adapter.Setup(a => a.IsDeviceEnrolledForUser(
                    It.Is<string>(id => id == "111")))
                .Returns(true);
            adapter.SetupGet(a => a.DeviceInfo)
                .Returns(deviceInfo.Object);

            var enrollment = await SecureConnectEnrollment.CreateEnrollmentAsync(
                adapter.Object,
                certificateStore.Object,
                "111");

            Assert.AreEqual(DeviceEnrollmentState.EnrolledWithoutCertificate, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }

        [Test]
        public async Task WhenUserIdHasDeviceEnrolledAndCertificateFoundInStoreButThumbprintMismatches_ThenStateIsEnrolledWithoutCertificate()
        {
            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListCertitficates(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(new[] { ExampleCertificate });

            var deviceInfo = new Mock<ISecureConnectDeviceInfo>();
            deviceInfo.SetupGet(i => i.CertificateThumbprints)
                .Returns(new[] { "nottherealthumbprint" });

            var adapter = new Mock<ISecureConnectAdapter>();
            adapter.SetupGet(a => a.IsInstalled).Returns(true);
            adapter.Setup(a => a.IsDeviceEnrolledForUser(
                    It.Is<string>(id => id == "111")))
                .Returns(true);
            adapter.SetupGet(a => a.DeviceInfo)
                .Returns(deviceInfo.Object);

            var enrollment = await SecureConnectEnrollment.CreateEnrollmentAsync(
                adapter.Object,
                certificateStore.Object,
                "111");

            Assert.AreEqual(DeviceEnrollmentState.EnrolledWithoutCertificate, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }

        [Test]
        public async Task WhenUserIdHasDeviceEnrolledAndCertificateFoundInStore_ThenStateIsEnrolled()
        {
            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListCertitficates(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(new[] { ExampleCertificate });

            var deviceInfo = new Mock<ISecureConnectDeviceInfo>();
            deviceInfo.SetupGet(i => i.CertificateThumbprints)
                .Returns(new[] { ExampleCertificate.ThumbprintSha256() });

            var adapter = new Mock<ISecureConnectAdapter>();
            adapter.SetupGet(a => a.IsInstalled).Returns(true);
            adapter.Setup(a => a.IsDeviceEnrolledForUser(
                    It.Is<string>(id => id == "111")))
                .Returns(true);
            adapter.SetupGet(a => a.DeviceInfo)
                .Returns(deviceInfo.Object);

            var enrollment = await SecureConnectEnrollment.CreateEnrollmentAsync(
                adapter.Object,
                certificateStore.Object,
                "111");

            Assert.AreEqual(DeviceEnrollmentState.Enrolled, enrollment.State);
            Assert.IsNotNull(enrollment.Certificate);
            Assert.AreEqual(ExampleCertitficateSubject, enrollment.Certificate.Subject);
        }

        [Test]
        public async Task WhenSwitchingUserIdsFromKnownToUnknown_ThenRefreshUpdatesStateToNotEnrolled()
        {
            var certificateStore = new Mock<ICertificateStoreAdapter>();
            certificateStore.Setup(s => s.ListCertitficates(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(new[] { ExampleCertificate });

            var deviceInfo = new Mock<ISecureConnectDeviceInfo>();
            deviceInfo.SetupGet(i => i.CertificateThumbprints)
                .Returns(new[] { ExampleCertificate.ThumbprintSha256() });

            var adapter = new Mock<ISecureConnectAdapter>();
            adapter.SetupGet(a => a.IsInstalled).Returns(true);
            adapter.Setup(a => a.IsDeviceEnrolledForUser(
                    It.Is<string>(id => id == "111")))
                .Returns(true);
            adapter.SetupGet(a => a.DeviceInfo)
                .Returns(deviceInfo.Object);

            var enrollment = await SecureConnectEnrollment.CreateEnrollmentAsync(
                adapter.Object,
                certificateStore.Object,
                "111");

            Assert.AreEqual(DeviceEnrollmentState.Enrolled, enrollment.State);
            Assert.IsNotNull(enrollment.Certificate);

            adapter.Setup(a => a.IsDeviceEnrolledForUser(
                    It.Is<string>(id => id == "111")))
                .Returns(false);

            await enrollment.RefreshAsync("1");
            Assert.AreEqual(DeviceEnrollmentState.NotEnrolled, enrollment.State);
            Assert.IsNull(enrollment.Certificate);
        }
    }
}
