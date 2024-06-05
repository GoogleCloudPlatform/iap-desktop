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

using Google.Solutions.Platform.Security.Cryptography;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Platform.Test.Security.Cryptography
{
    [TestFixture]
    public class TestCertificateStore
    {
        //
        // Self-signed certificate, created using:
        //   New-SelfSignedCertificate -Subject"CertStoreTest" `
        //     -CertStoreLocation Cert:\CurrentUser\My\ -NotAfter 01/01/2030
        // then exported as PFX, and base64-encoded:
        //   & certutil.exe -encode .\cert.pfx cert.pfx.txt
        //
        private const string ExampleCertitficateSubject = "CN=CertStoreTest";
        private static readonly X509Certificate2 ExampleCertificate =
            new X509Certificate2(Convert.FromBase64String(
                    @"MIIJzAIBAzCCCYgGCSqGSIb3DQEHAaCCCXkEggl1MIIJcTCCBgAGCSqGSIb3DQEH
                    AaCCBfEEggXtMIIF6TCCBeUGCyqGSIb3DQEMCgECoIIE/jCCBPowHAYKKoZIhvcN
                    AQwBAzAOBAjixWwGNukb6wICB9AEggTYHFQsrLH71H9sSzPDsQGGNX1gsi6JK8s9
                    CV247o93isdlolga5ij48ECQD+7gIjhJWbPgoYH4Xpmsyf67s3iIn5oMJpbY64Xl
                    TKY4hZdg/07BorA8UtX/GBRh7ku+MqL42v6ug2NraEQgpPRrz0ht7FowGmi1aPkP
                    bkMS+/tJ9MU8a1kAc5hPlqcQY2OkK2xHTdJoZMLL67fD5blrs+SRrYYv5S/+cmev
                    zDo+yj86aCp/LmTATf3ogHJ6rxN2duCh+7725r5jnQrskIR7r+9pXPt0Fb3Nfn+C
                    M2LK8hu94oz5guXxcV3AAs/Cf54rRFDh5vfde0haljFPH3wTki6lq/fXjndq8I0S
                    SHcJzu38wd85qbtyra/EeVUCC8nSFVUWiSacRSrsIYzwyk4WPnCzNYc65K3L0jpk
                    ahr/kvu1NRatXSokD/Fg6t1ERJV7aMuBdku71ncnBdNAL6SKCVd7eNha+TVYY0G6
                    7+GbnNph50+oJDiIx+44NafZY0ojkvPl1qT1dEisH4Bmj2yN3IBLem4Q3dk6TIVK
                    NnPzw4PxL13n/OAmXR+lD8vCCeYtDWGV2hRNLepXs1D2Ts8CFB6MQmEMFZ3pppSy
                    byUH1ceCJHANjpgOhUAgQtNA4l7Wg61FR6YD+U2a6DudqyBmXijCZoGs9TLleVb5
                    cpQ/OtOOG290ao3vMdhwEliLN7NHzVh87B5pUx1K5P+fHkCpu1IClkA03qM3CD9j
                    Odsk8gXmv/0vM9t8LkGVjx9QxVZWDSPaPDhHumMBSu3sNr+u7VKjPbY0MgSLFTxz
                    e9+xOD0SybcmLewQdQ4JBsggpZsfXTK1WklYOU4LfkrG7HNwoICL4qEWkkrUNBZf
                    yYjxi1niu5HQk7q4IZyMlOhKxybkENdYVzpeo8sFhaN1ZcCLTslIieLDNLg26SFx
                    IoOpNUO1FQn+ZrF6QPRoD5ALxZUascUsDf/H5QbGZEedZCREJ+OxdQWv7VtFRBjx
                    IFQdV1+cM+Nrxyc7BV6Lq5kUHrHBt4QbcYxbeKjQKMbFmq59FNAFGagOQHdOtTAb
                    dDxUd621eGlWAkqMUOTS13yb1uBsb9ezTxu2CptmLGicvjA5a1ixBfZt+c7XrROt
                    1QnuJbXHQEXajWJiJBVlp6Kzeo32bmgS3MVsET/PIduiIdEDF55aW37jrxxSoPuB
                    uCX0h3qJD1/mDnB6CkOg7DvIh9YB3+6Q3DipbOlD8Q9hufLJPuE55FCMI2U20fSi
                    KdgB9NlChu77G1tsUI3eT687iw9l9TvYpjJEmmCTDPBLDGjf5Msa0UBkfziqFbqU
                    hxk1Z4ibQL9wP9bfTPn8uPVvAzhOsKLGw40mOAqBZZlvS2kEML/iTFjMBqQhv7+k
                    7FN1kLYq2cXZGXglYLqro+8LSaJvwQohWpJLdsI/MwGvVL4+NLrOZ2QpdxxikiY5
                    8UYwT7ZpZgIiXdXc/STV7Jfkm89uMMaEO+42v7KCzr1YdQT/Sr9QzA1/LoMlPS+g
                    wrzh+nVevCFllxtLafBFzBfoZk37fGFVOEtI4Muu/GGRH7i1PYM5FQ2ow6u0XkFB
                    VlasIopZkwXoCzaSCvfgWH1jDgmJ2ViuEEgi1oaNGiol8n7pTV3lkOGdchaWvvIy
                    s4NlEF3LGtXM6zly3+wPozGB0zATBgkqhkiG9w0BCRUxBgQEAQAAADBdBgkqhkiG
                    9w0BCRQxUB5OAHQAZQAtAGIAOQA1ADMANwBlAGUAYwAtADQAMgA5AGQALQA0ADAA
                    NwAzAC0AYgAwAGYANgAtAGUAZABjADkANgA1ADUAYQBmADYAZQBmMF0GCSsGAQQB
                    gjcRATFQHk4ATQBpAGMAcgBvAHMAbwBmAHQAIABTAG8AZgB0AHcAYQByAGUAIABL
                    AGUAeQAgAFMAdABvAHIAYQBnAGUAIABQAHIAbwB2AGkAZABlAHIwggNpBgkqhkiG
                    9w0BBwGgggNaBIIDVjCCA1IwggNOBgsqhkiG9w0BDAoBA6CCAyYwggMiBgoqhkiG
                    9w0BCRYBoIIDEgSCAw4wggMKMIIB8qADAgECAhAiTP/UOBmPp0S1URNN3Y9eMA0G
                    CSqGSIb3DQEBCwUAMBgxFjAUBgNVBAMMDUNlcnRTdG9yZVRlc3QwHhcNMjAxMDE2
                    MDU1NTI1WhcNMjkxMjMxMjIwMDAwWjAYMRYwFAYDVQQDDA1DZXJ0U3RvcmVUZXN0
                    MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA4C0ZVppgOe3rNFr/7gDr
                    wJzu+5HhzwKHRBPnwpyFj1Qd+lHdVWIFZj6ivjCbfbg5ARsKaOFoSZ7pME1BV36H
                    3bJe1om8CVkc3LBlmsec7dDuqvEsC3W6hUbTRcNQR67G312IXPRgJeU0k5juxFg6
                    SX2+TfxBR6t6jnq4eBWdh0bSNpiqVEsPZ1qGr/5c33Op00WaTtY2r6oTwU7Xbtei
                    FXgfUksiKh2/9Ld8czZE72SagWJj5nTFp2rdhe2Hc6OjxYGw89874qHCk7l+fRPS
                    0d7luENmeuqCe4y3b+/e6KfRkPlzgeAOUrPPFIbFlyvN2HBOipb7BugnizvuI7p/
                    uQIDAQABo1AwTjAOBgNVHQ8BAf8EBAMCBaAwHQYDVR0lBBYwFAYIKwYBBQUHAwIG
                    CCsGAQUFBwMBMB0GA1UdDgQWBBR9660fT3J2ov9KShUw/reBnDvxoDANBgkqhkiG
                    9w0BAQsFAAOCAQEACb+g3VZnZsP2AMtjYIrWfXL6nQvwDr8j8v8BY3d9HPgMoG4z
                    ymwYZB//o45Us27FK0b+Bm5LHMfk2ahWIiOkvcVhSFtDsiOfuRmLhDhwu67nKnqt
                    dwa8ICMj/19g87lzl7LUDaYTp7qKK38q75vT64asbkvzD+AOPMZQkmSCHJ61CkbI
                    d8Kqd9YqWmiudRUMSG/Y0CTQnqoUMg4lmYX9EpLzOhma5qQD/S55eP7zYUO4bzB2
                    8NhTlvyxK4cdpgXaXhr28GqXX9KttnsxPnOXWQTf1uZhZ7kIC/nE8zGEIODlLbuS
                    t3rrQe4VwU22F5Hud/pDxM09CHoAp6s5Xk4i5TEVMBMGCSqGSIb3DQEJFTEGBAQB
                    AAAAMDswHzAHBgUrDgMCGgQUvimjWhbfkj8p2K4QIulCZWCmly8EFFi+fHv0Hz2l
                    08dwKJjVk0fmFAt8AgIH0A==
                    "),
                "password");

        [SetUp]
        public void SetUp()
        {
            CertificateStore.RemoveUserCertitficate(ExampleCertificate);
        }

        //---------------------------------------------------------------------
        // ListUserCertificates.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPredicateMatches_ThenListUserCertificatesReturnsUserCertificate()
        {
            CertificateStore.AddUserCertitficate(ExampleCertificate);

            var store = new CertificateStore();
            var certificates = store.ListUserCertificates(
                cert => cert.Thumbprint == ExampleCertificate.Thumbprint);

            Assert.IsNotNull(certificates);
            Assert.AreEqual(1, certificates.Count());
            Assert.AreEqual(
                ExampleCertificate.Thumbprint,
                certificates.First().Thumbprint);
            Assert.AreEqual(
                ExampleCertitficateSubject,
                certificates.First().Subject);
        }

        [Test]
        public void WhenPredicateDoesNotMatch_ThenListUserCertificatesReturnsEmpty()
        {
            CertificateStore.AddUserCertitficate(ExampleCertificate);

            var store = new CertificateStore();
            var certificates = store.ListUserCertificates(cert => false);

            Assert.IsNotNull(certificates);
            CollectionAssert.IsEmpty(certificates);
        }

        //---------------------------------------------------------------------
        // ListComputerCertificates.
        //---------------------------------------------------------------------

        [Test]
        public void ListComputerCertificatesDoesNotReturnUserCertificate()
        {
            CertificateStore.AddUserCertitficate(ExampleCertificate);

            var store = new CertificateStore();
            var certificates = store.ListMachineCertificates(
                cert => cert.Thumbprint == ExampleCertificate.Thumbprint);

            Assert.IsNotNull(certificates);
            CollectionAssert.IsEmpty(certificates);
        }

        [Test]
        public void WhenPredicateDoesNotMatch_ThenListComputerCertificatesReturnsEmpty()
        {
            CertificateStore.AddUserCertitficate(ExampleCertificate);

            var store = new CertificateStore();
            var certificates = store.ListMachineCertificates(cert => false);

            Assert.IsNotNull(certificates);
            CollectionAssert.IsEmpty(certificates);
        }
    }
}
