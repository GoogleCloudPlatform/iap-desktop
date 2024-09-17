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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Linq;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Platform.Net
{
    /// <summary>
    /// A collection of certificate selectors as defined by the Chrome
    /// AutoSelectCertificateForUrls policy. See
    /// https://chromeenterprise.google/policies/#AutoSelectCertificateForUrls
    /// </summary>
    public class ChromeAutoSelectCertificateForUrlsPolicy : IChromeAutoSelectCertificateForUrlsPolicy
    {
        private const string KeyPath = @"SOFTWARE\Policies\Google\Chrome\AutoSelectCertificateForUrls";

        internal IReadOnlyCollection<ChromeCertificateSelector> Entries { get; }

        private ChromeAutoSelectCertificateForUrlsPolicy(
            IReadOnlyCollection<ChromeCertificateSelector> entries)
        {
            this.Entries = entries;
        }

        //---------------------------------------------------------------------
        // IChromeAutoSelectCertificateForUrlsPolicy.
        //---------------------------------------------------------------------

        public bool IsApplicable(Uri uri, X509Certificate2 clientCertificate)
        {
            var selectors = this.Entries.Where(e => e.Pattern.IsMatch(uri));
            if (selectors == null)
            {
                //
                // No selector => no certificate matches.
                //
                return false;
            }
            else
            {
                //
                // At least one selector found => try them (in order) to match certificates.
                //
                return selectors.Any(s => s.IsMatch(uri, clientCertificate));
            }
        }

        //---------------------------------------------------------------------
        // Builder.
        //---------------------------------------------------------------------

        public class Builder
        {
            private readonly LinkedList<ChromeCertificateSelector> entries
                = new LinkedList<ChromeCertificateSelector>();

            public Builder Add(ChromeCertificateSelector entry)
            {
                this.entries.AddLast(entry);
                return this;
            }

            /// <summary>
            /// Add entries from a specific group policy key.
            /// </summary>
            public Builder AddGroupPolicy(RegistryKey? registryKey)
            {
                if (registryKey == null)
                {
                    return this;
                }

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
                        try
                        {
                            var selector = ChromeCertificateSelector.Parse(value);
                            if (selector != null)
                            {
                                this.entries.AddLast(selector);
                            }
                        }
                        catch (JsonException e)
                        {
                            //
                            // Malformed entry, ignore.
                            //
                            PlatformTraceSource.Log.TraceVerbose(
                                "Encountered malformed AutoSelectCertificateForUrls policy entry '{0}': {1}",
                                value,
                                e.Message);
                        }
                    }
                }

                return this;
            }

            /// <summary>
            /// Add entries from default group policy key in the given hive.
            /// </summary>
            public Builder AddGroupPolicy(RegistryHive hive)
            {
                using (var hiveKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
                using (var key = hiveKey.OpenSubKey(KeyPath))
                {
                    return AddGroupPolicy(key);
                }
            }

            /// <summary>
            /// Add entries from all group policies applying to the current user.
            /// </summary>
            public Builder AddGroupPoliciesForCurrentUser()
            {
                AddGroupPolicy(RegistryHive.LocalMachine);
                AddGroupPolicy(RegistryHive.CurrentUser);
                return this;
            }

            public ChromeAutoSelectCertificateForUrlsPolicy Build()
            {
                return new ChromeAutoSelectCertificateForUrlsPolicy(this.entries);
            }
        }
    }
}
