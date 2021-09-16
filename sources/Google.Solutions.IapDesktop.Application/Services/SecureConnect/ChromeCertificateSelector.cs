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
    /// 
    /// In addition to the standard SUBJECT and ISSUER filters, this implementation
    /// supports a THUMBPRINT filter that lets you select a specific certificate.
    /// </summary>
    internal class ChromeCertificateSelector
    {
        public ChromePolicyUrlPattern Pattern { get; }
        public CertificateFilter Filter { get; }

        [JsonConstructor]
        public ChromeCertificateSelector(
            [JsonProperty("pattern")] string pattern,
            [JsonProperty("filter")] CertificateFilter filter)
        {
            this.Pattern = ChromePolicyUrlPattern.Parse(pattern ?? ChromePolicyUrlPattern.All);
            this.Filter = filter ?? new CertificateFilter();
        }

        public static ChromeCertificateSelector Parse(string json)
        {
            Utilities.ThrowIfNullOrEmpty(json, nameof(json));
            return JsonConvert.DeserializeObject<ChromeCertificateSelector>(json);
        }

        public static bool TryParse(string json, out ChromeCertificateSelector selector)
        {
            try
            {
                selector = Parse(json);
                return true;
            }
            catch
            {
                selector = null;
                return false;
            }
        }

        public bool IsMatch(
            Uri uri,
            X500DistinguishedName issuer,
            X500DistinguishedName subject,
            string thumbprint)
        {
            return
                this.Pattern.IsMatch(uri) &&
                (this.Filter.Issuer == null || this.Filter.Issuer.IsMatch(issuer)) &&
                (this.Filter.Subject == null || this.Filter.Subject.IsMatch(subject)) &&
                (this.Filter.Thumbprint == null || this.Filter.Thumbprint.Equals(
                        thumbprint, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsMatch(
            Uri uri,
            X509Certificate2 certificate)
            => IsMatch(
                uri, 
                certificate.IssuerName, 
                certificate.SubjectName, 
                certificate.Thumbprint);

        //---------------------------------------------------------------------
        // Inner classes for deserialization.
        //---------------------------------------------------------------------

        public class CertificateFilter
        {
            [JsonConstructor]
            public CertificateFilter(
                [JsonProperty("ISSUER")] DistinguishedNameFilter issuer,
                [JsonProperty("SUBJECT")] DistinguishedNameFilter subject,
                [JsonProperty("THUMBPRINT")] string thumbprint)
            {
                this.Issuer = issuer;
                this.Subject = subject;
                this.Thumbprint = thumbprint;
            }

            public CertificateFilter() : this(null, null, null)
            {
            }

            [JsonProperty("ISSUER")]
            public DistinguishedNameFilter Issuer { get; }
            
            [JsonProperty("SUBJECT")]
            public DistinguishedNameFilter Subject { get; }

            [JsonProperty("THUMBPRINT")]
            public string Thumbprint { get; }
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
                this.CommonName = commonName;
                this.Location = location;
                this.Organization = organization;
                this.OrgUnit = orgUnit;
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
