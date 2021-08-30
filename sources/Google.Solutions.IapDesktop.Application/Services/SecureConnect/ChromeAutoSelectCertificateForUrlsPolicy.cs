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

using Google.Solutions.Common.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.SecureConnect
{
    /// <summary>
    /// A collection of certificate selectors as defined by the Chrome
    /// AutoSelectCertificateForUrls policy. See
    /// https://chromeenterprise.google/policies/#AutoSelectCertificateForUrls
    /// </summary>
    internal class ChromeAutoSelectCertificateForUrlsPolicy
    {
        private const string KeyPath = @"SOFTWARE\Policies\Google\Chrome\AutoSelectCertificateForUrls";
        
        public static ChromeAutoSelectCertificateForUrlsPolicy Default =
            new ChromeAutoSelectCertificateForUrlsPolicy(new ChromeCertificateSelector[0]);

        public IReadOnlyCollection<ChromeCertificateSelector> Entries { get; }

        private ChromeAutoSelectCertificateForUrlsPolicy(
            IReadOnlyCollection<ChromeCertificateSelector> entries)
        {
            this.Entries = entries;
        }

        public Func<X509Certificate2, bool> CreateMatcher(Uri uri)
        {
            var selectors = this.Entries.Where(e => e.Pattern.IsMatch(uri));
            if (selectors == null)
            {
                //
                // No selector => no certificate matches.
                //
                return _ => false;
            }
            else
            {
                //
                // At least one selector found => try them (in order) to match certificates.
                //
                return certificate => selectors.Any(s => s.IsMatch(uri, certificate));
            }
        }

        private static IEnumerable<ChromeCertificateSelector> LoadEntries(RegistryKey registryKey)
        {
            //
            // The key contains any number of values named "1", "2", ...
            //
            var sortedValueNames = registryKey
                .GetValueNames()
                .EnsureNotNull()
                .Where(name => uint.TryParse(name, out var _))
                .Select(name => uint.Parse(name))
                .OrderBy(name => name)
                .ToList();

            foreach (var valueName in sortedValueNames)
            {
                if (registryKey.GetValue(valueName.ToString(), null) is string value)
                {
                    yield return ChromeCertificateSelector.Parse(value);
                }
            }
        }

        public static ChromeAutoSelectCertificateForUrlsPolicy FromKey(RegistryKey registryKey)
        {
            return registryKey == null
                ? Default
                : new ChromeAutoSelectCertificateForUrlsPolicy(LoadEntries(registryKey).ToList());
        }

        public static ChromeAutoSelectCertificateForUrlsPolicy FromKey(RegistryHive hive)
        {
            using (var hiveKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
            using (var key = hiveKey.OpenSubKey(KeyPath))
            {
                return FromKey(key);
            }
        }
    }
}
