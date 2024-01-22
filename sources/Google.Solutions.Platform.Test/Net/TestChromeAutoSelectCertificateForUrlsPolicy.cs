//
// Copyright 2021 Google LLC
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

using Google.Solutions.Platform.Net;
using Google.Solutions.Testing.Apis.Cryptography;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Platform.Test.Net
{
    [TestFixture]
    public class TestChromeAutoSelectCertificateForUrlsPolicy
    {
        private const string TestKeyPath = @"Software\Google\__Test";

        private RegistryKey? key;

        //
        // New-SelfSignedCertificate `
        //  -DnsName "example.org" `
        //  -CertStoreLocation Cert:\CurrentUser\My\ `
        //  -NotAfter 01/01/2030
        //
        private static readonly X509Certificate2 ExampleOrgCertificate =
            CertificateUtil.CertificateFromPem(
                @"-----BEGIN CERTIFICATE-----
                MIIDHjCCAgagAwIBAgIQSm95EZzky4xExS0A4QJgVzANBgkqhkiG9w0BAQsFADAW
                MRQwEgYDVQQDDAtleGFtcGxlLm9yZzAeFw0yMTA4MzAxMTU0MzdaFw0yOTEyMzEy
                MjAwMDBaMBYxFDASBgNVBAMMC2V4YW1wbGUub3JnMIIBIjANBgkqhkiG9w0BAQEF
                AAOCAQ8AMIIBCgKCAQEAznTOAej1mw2nACXxR6KCEdqw03pZXN9DtISihS0z/TjF
                lsZIfpzR7Pe8i7XDFdaKSOVz4sJ/tjEj0VlGtqMN3g9ypbVCNDPYAPnqOGip0t30
                dffIvvI94p/n1neJvWB3nALKEzw4oVpGiOrVU30eP2iWMxXzaYEqvn+hq+480U7X
                iPIaoSjqIeIoSdAw1iUh7WoZROEwmIYVEENknmFLpmGG42mSV4y6LmuULJpUBDgF
                mtqrDl/+kIM4KA5+1ir2ySCXGdisCxnE9dHjL9JIrvpoirc9QiCTB/QnNJDD7IZJ
                jeHjlTjv9UmwU1QDqTEGqj7v6HUiU0XwVZaFw/tDiQIDAQABo2gwZjAOBgNVHQ8B
                Af8EBAMCBaAwHQYDVR0lBBYwFAYIKwYBBQUHAwIGCCsGAQUFBwMBMBYGA1UdEQQP
                MA2CC2V4YW1wbGUub3JnMB0GA1UdDgQWBBQ7WZjNqXwgqMbQRbaeZq2iHn/RJTAN
                BgkqhkiG9w0BAQsFAAOCAQEAZjUsVWjBSyzraoAbkL0+D0x6cX9E/tFoS9JkBAPC
                ndwr1TdQ4ZgaVMAdROG2dJt1rNVaxPjw9A2pLluruFkrkmCgWWjiUBB572ZqDJ8E
                2plgGRJtOH/9lsKQCxO/7MA3ZJ4I4GLzFUKHk41CeUYwcSniu/a8/ilh8G7GM9IH
                Yy5xTCgiH//ZFB0qwZTvvpEqFW4Pzvo/GIn/QetWMbIqGI00nLzcja4e0JrohBkj
                Z/1ZVGpDAvfGbOMZAKOgyT/Uej2egjM+gcoaAxzZWgJztjXhGJ6KoqzqqWSeFR0y
                IqnurqU2oBP8YmhdlX349p57B08sK5EfVgsZQt3ZpKjP3Q==
                -----END CERTIFICATE-----
                ");

        [SetUp]
        public void SetUp()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            this.key = hkcu.CreateSubKey(TestKeyPath);
        }

        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyIsNull_ThenBuilderCreatesEmptyPolicy()
        {
            var policy = new ChromeAutoSelectCertificateForUrlsPolicy.Builder()
                .AddGroupPolicy((RegistryKey?)null)
                .Build();

            Assert.IsNotNull(policy);
            Assert.IsFalse(policy.Entries.Any());
        }

        [Test]
        public void WhenPolicyEmpty_ThenNoCertificatesMatch()
        {
            Assert.IsFalse(new ChromeAutoSelectCertificateForUrlsPolicy.Builder()
                .Build()
                .IsApplicable(new Uri("https://example.org"), ExampleOrgCertificate));
        }

        [Test]
        public void WhenKeyContainsJunkValues_ThenBuilderIgnoresJunkValues()
        {
            this.key!.SetValue("1", "{'pattern': 'https://[*.]example.org', 'filter':{}}");
            this.key.SetValue("2", "{'pattern': 'https://[*.]example.com', 'filter':{}}");
            this.key.SetValue("junk", 1);
            this.key.SetValue("-3", "junk");

            var policy = new ChromeAutoSelectCertificateForUrlsPolicy.Builder()
                .AddGroupPolicy(this.key)
                .Build();
            Assert.AreEqual(2, policy.Entries.Count);
        }

        [Test]
        public void WhenKeyContainsMalformedValues_ThenBuilderIgnoresJunkValues()
        {
            this.key!.SetValue("1", "{'pattern': 'https://[*.]example.org', 'filter':{}}");
            this.key.SetValue("2", "{'pattern': 'https://[*.]example.com', 'filter':{"); // Syntax error.

            var policy = new ChromeAutoSelectCertificateForUrlsPolicy.Builder()
                .AddGroupPolicy(this.key)
                .Build();
            Assert.AreEqual(1, policy.Entries.Count);
        }

        [Test]
        public void WhenKeyContainsSelectors_ThenBuilderEvaluatesSelectors()
        {
            this.key!.SetValue("11",
                "{'pattern': 'https://[*.]example.org', 'filter':{'SUBJECT': {'CN': 'example.org'}}}");
            this.key.SetValue("20",
                "{'pattern': 'https://[*.]example.com', 'filter':{'SUBJECT': {'CN': 'example.com'}}}");

            var policy = new ChromeAutoSelectCertificateForUrlsPolicy.Builder()
                .AddGroupPolicy(this.key)
                .Build();

            Assert.IsTrue(policy.IsApplicable(new Uri("https://www.example.org"), ExampleOrgCertificate));
            Assert.IsFalse(policy.IsApplicable(new Uri("https://www.example.com"), ExampleOrgCertificate));
        }
    }
}
