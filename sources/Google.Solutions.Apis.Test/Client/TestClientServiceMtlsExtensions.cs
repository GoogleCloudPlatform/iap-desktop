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

using Google.Apis.Compute.v1;
using Google.Solutions.Apis.Client;
using NUnit.Framework;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    public class TestClientServiceMtlsExtensions
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
        private const string SampleDeviceCertificate =
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
            -----END CERTIFICATE-----";

        [Test]
        public void WhenDcaNotEnabled_ThenIsDeviceCertificateAuthenticationEnabledReturnsFalse()
        {
            var client = new ComputeService(new Google.Apis.Services.BaseClientService.Initializer());
            Assert.IsFalse(client.IsDeviceCertificateAuthenticationEnabled());
        }

        [Test]
        public void WhenDcaEnabled_ThenIsDeviceCertificateAuthenticationEnabledReturnsTrue()
        {
            var deviceCertificatePem = Path.GetTempFileName();
            File.WriteAllText(deviceCertificatePem, SampleDeviceCertificate);

            var initializer = new Google.Apis.Services.BaseClientService.Initializer();
            initializer.EnableDeviceCertificateAuthentication(
                new X509Certificate2(deviceCertificatePem));

            var client = new ComputeService(initializer);
            Assert.IsTrue(client.IsDeviceCertificateAuthenticationEnabled());
        }
    }
}
