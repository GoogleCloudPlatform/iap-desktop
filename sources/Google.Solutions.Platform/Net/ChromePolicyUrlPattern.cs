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
using System;

namespace Google.Solutions.Platform.Net
{
    /// <summary>
    /// Content settings pattern as defined in 
    /// https://chromeenterprise.google/policies/url-patterns/ and
    /// implemented in
    /// /components/content_settings/core/common/content_settings_pattern.cc
    /// /components/content_settings/core/common/content_settings_pattern_parser.cc
    /// in the Chromium sources.
    /// 
    /// Note: These patterns differ from Extensions URL patterns
    /// defined in https://developer.chrome.com/docs/extensions/mv3/match_patterns/.
    /// 
    /// file:/// URLs are not supported and are always considered
    /// non-matching.
    /// </summary>
    internal class ChromePolicyUrlPattern
    {
        public const string All = "*";

        private const string SubdomainWilcard = "[*.]";

        private readonly string? scheme;
        private readonly ushort? port;
        private readonly string? domain;
        private readonly bool includeSubdomains;

        private static ushort? DefaultPortForScheme(string? scheme)
        {
            return scheme switch
            {
                "http" => (ushort?)80,
                "https" => (ushort?)443,
                _ => null,// any.
            };
        }

        private ChromePolicyUrlPattern(
            string? scheme,
            ushort? port,
            string? domain,
            bool includeSubdomains)
        {
            this.scheme = scheme;
            this.port = port;
            this.domain = domain;
            this.includeSubdomains = includeSubdomains;
        }

        public bool IsMatch(Uri input)
        {
            if (this.scheme == "file")
            {
                return false;
            }

            if (input.Scheme != "http" && input.Scheme != "https")
            {
                return false;
            }
            else if (this.scheme != null && this.scheme != input.Scheme)
            {
                return false;
            }

            if (this.port != null && this.port.Value != input.Port)
            {
                return false;
            }

            return
                this.domain == null ||
                this.domain == input.Host ||
                this.includeSubdomains && input.Host.EndsWith("." + this.domain);
        }

        public bool IsMatch(string input) => IsMatch(new Uri(input));

        public static ChromePolicyUrlPattern Parse(string pattern)
        {
            Precondition.ExpectNotEmpty(pattern, nameof(pattern));

            pattern = pattern.ToLower();

            if (pattern == All)
            {
                return new ChromePolicyUrlPattern(
                    null,   // All schemes.
                    null,   // All ports.
                    null,   // All domains.
                    true);  // All subdomains.
            }

            var colonDoubleSlashIndex = pattern.IndexOf("://");
            string? scheme;
            string patternWithoutScheme;
            if (colonDoubleSlashIndex == -1)
            {
                scheme = null;
                patternWithoutScheme = pattern;
            }
            else if (colonDoubleSlashIndex == 1 && pattern[0] == '*')
            {
                scheme = null;
                patternWithoutScheme = pattern.Substring(4);
            }
            else
            {
                scheme = pattern.Substring(0, colonDoubleSlashIndex);
                patternWithoutScheme = pattern.Substring(colonDoubleSlashIndex + 3);
            }

            var slashIndex = patternWithoutScheme.IndexOf('/');
            var domainAndPort = slashIndex >= 0
                ? patternWithoutScheme.Substring(0, slashIndex)
                : patternWithoutScheme;

            var ipv6endBracket = domainAndPort.IndexOf(']');
            var colonIndex = domainAndPort.IndexOf(':', ipv6endBracket + 1);
            var port = colonIndex >= 0
                ? domainAndPort.Substring(colonIndex + 1) == "*"
                    ? null
                    : (ushort?)ushort.Parse(domainAndPort.Substring(colonIndex + 1))
                : DefaultPortForScheme(scheme);
            var domain = colonIndex >= 0
                ? domainAndPort.Substring(0, colonIndex)
                : domainAndPort;

            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentException("Pattern is empty", nameof(pattern));
            }

            var includeSubdomains = domain.StartsWith(SubdomainWilcard);

            return new ChromePolicyUrlPattern(
                scheme,
                port,
                includeSubdomains
                    ? domain.Substring(SubdomainWilcard.Length)
                    : domain,
                includeSubdomains);
        }
    }
}
