using Google.Apis.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.SecureConnect
{
    /// <summary>
    /// Certificate selector as defined in
    /// https://chromeenterprise.google/policies/?policy=AutoSelectCertificateForUrls.
    /// </summary>
    internal class ChromeCertificateSelector
    {
        public ChromeMatchPattern Pattern { get; }
        public CertificateFilter Filter { get; }

        [JsonConstructor]
        public ChromeCertificateSelector(
            [JsonProperty("pattern")] string pattern,
            [JsonProperty("filter")] CertificateFilter filter)
        {
            this.Pattern = ChromeMatchPattern.Parse(pattern ?? ChromeMatchPattern.AllUrls);
            this.Filter = filter ?? new CertificateFilter();
        }

        public static ChromeCertificateSelector Parse(string json)
        {
            Utilities.ThrowIfNullOrEmpty(json, nameof(json));
            return JsonConvert.DeserializeObject<ChromeCertificateSelector>(json);
        }

        public bool IsMatch(
            Uri uri,
            X500DistinguishedName issuer,
            X500DistinguishedName subject)
        {
            return
                this.Pattern.IsMatch(uri) &&
                (this.Filter.Issuer == null || this.Filter.Issuer.IsMatch(issuer)) &&
                (this.Filter.Subject == null || this.Filter.Subject.IsMatch(subject));
        }

        public bool IsMatch(
            Uri uri,
            X509Certificate2 certificate)
            => IsMatch(uri, certificate.IssuerName, certificate.SubjectName);

        //---------------------------------------------------------------------
        // Inner classes for deserialization.
        //---------------------------------------------------------------------

        public class CertificateFilter
        {
            [JsonConstructor]
            public CertificateFilter(
                [JsonProperty("ISSUER")] DistinguishedNameFilter issuer,
                [JsonProperty("SUBJECT")] DistinguishedNameFilter subject)
            {
                Issuer = issuer;
                Subject = subject;
            }

            public CertificateFilter() : this(null, null)
            {
            }

            [JsonProperty("ISSUER")]
            public DistinguishedNameFilter Issuer { get; }
            
            [JsonProperty("SUBJECT")]
            public DistinguishedNameFilter Subject { get; }
        }

        public class DistinguishedNameFilter
        {
            [JsonConstructor]
            public DistinguishedNameFilter(
                [JsonProperty("CN")] string commonName,
                [JsonProperty("L")] string location,
                [JsonProperty("O")] string organization,
                [JsonProperty("OU")] string orgUnit)
            {
                CommonName = commonName;
                Location = location;
                Organization = organization;
                OrgUnit = orgUnit;
            }

            [JsonProperty("CN")]
            public string CommonName { get; }

            [JsonProperty("L")]
            public string Location { get; }

            [JsonProperty("O")]
            public string Organization { get; }

            [JsonProperty("OU")]
            public string OrgUnit { get; }

            public bool IsMatch(X500DistinguishedName dn)
            {
                var components = dn
                    .Format(true)
                    .Split('\n')
                    .Select(s => s.Trim())
                    .ToList();

                return
                    (this.CommonName == null || components.Contains($"CN={this.CommonName}")) &&
                    (this.Location == null || components.Contains($"L={this.Location}")) &&
                    (this.Organization == null || components.Contains($"O={this.Organization}")) &&
                    (this.OrgUnit == null || components.Contains($"OU={this.OrgUnit}"));
            }
        }
    }
}
