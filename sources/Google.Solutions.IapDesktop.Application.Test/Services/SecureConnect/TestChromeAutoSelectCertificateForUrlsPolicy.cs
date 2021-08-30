using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.SecureConnect
{
    [TestFixture]
    public class TestChromeAutoSelectCertificateForUrlsPolicy : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";

        private RegistryKey key;

        //
        // New-SelfSignedCertificate `
        //  -DnsName "example.org" `
        //  -CertStoreLocation Cert:\CurrentUser\My\ `
        //  -NotAfter 01/01/2030
        //
        private readonly X509Certificate2 ExampleOrgCertificate =
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
        public void WhenKeyIsNull_ThenFromKeyReturnsDefaultPolicy()
        {
            var policy = ChromeAutoSelectCertificateForUrlsPolicy.FromKey(null);

            Assert.IsNotNull(policy);
            Assert.AreSame(ChromeAutoSelectCertificateForUrlsPolicy.Default, policy);
        }

        [Test]
        public void WhenPolicyEmpty_ThenNoCertificatesMatch()
        {
            var matcher = ChromeAutoSelectCertificateForUrlsPolicy
                .Default
                .CreateMatcher(new Uri("https://example.org"));

            Assert.IsNotNull(matcher);
            Assert.IsFalse(matcher(ExampleOrgCertificate));
        }

        [Test]
        public void WhenKeyContainsJunkValues_ThenFromKeyIgnoresJunkValues()
        {
            this.key.SetValue("1", "{'pattern': 'https://[*.]example.org', 'filter':{}}");
            this.key.SetValue("2", "{'pattern': 'https://[*.]example.com', 'filter':{}}");
            this.key.SetValue("junk", 1);
            this.key.SetValue("-3", "junk");

            var policy = ChromeAutoSelectCertificateForUrlsPolicy.FromKey(this.key);
            Assert.AreEqual(2, policy.Entries.Count);
        }

        [Test]
        public void WhenKeyContainsSelectors_ThenCreateMatchersEvaluatesSelectors()
        {
            this.key.SetValue("11", 
                "{'pattern': 'https://[*.]example.org', 'filter':{'SUBJECT': {'CN': 'example.org'}}}");
            this.key.SetValue("20", 
                "{'pattern': 'https://[*.]example.com', 'filter':{'SUBJECT': {'CN': 'example.com'}}}");

            var policy = ChromeAutoSelectCertificateForUrlsPolicy
                .FromKey(this.key);

            Assert.IsTrue(policy.CreateMatcher(new Uri("https://www.example.org"))(ExampleOrgCertificate));
            Assert.IsFalse(policy.CreateMatcher(new Uri("https://www.example.com"))(ExampleOrgCertificate));
        }
    }
}
