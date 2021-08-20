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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.SecureConnect
{
    /// <summary>
    /// Match pattern as defined in 
    /// https://developer.chrome.com/docs/extensions/mv3/match_patterns/.
    /// </summary>
    internal class ChromeMatchPattern
    {
        public const string AllUrls = "<all_urls>";

        private readonly Regex pattern;

        private ChromeMatchPattern(Regex pattern)
        {
            this.pattern = pattern;
        }

        public bool IsMatch(string input)
        {
            return this.pattern.IsMatch(input);
        }

        public bool IsMatch(Uri input)
            => IsMatch(input.ToString());

        public static ChromeMatchPattern Parse(string pattern)
        {
            Utilities.ThrowIfNullOrEmpty(pattern, nameof(pattern));

            //
            // The pattern can contain * wildcards, but the semantics
            // differ on whether the location is in the scheme, host,
            // or path portion of the URL.
            //
            if (pattern == AllUrls)
            {
                return new ChromeMatchPattern(new Regex(".*"));
            }

            var colonIndex = pattern.IndexOf(':');
            if (colonIndex < 1 || colonIndex == pattern.Length -1)
            {
                throw new ArgumentException("Pattern must use syntax scheme://host/path");
            }

            //
            // If the scheme is *, then it matches either http or https, and
            // not file, ftp, or urn.
            //
            var scheme = pattern.Substring(0, colonIndex);
            scheme = scheme == "*" ? "(http|https)" : scheme;

            //
            // If the host is just *, then it matches any host. If the host
            // is *._hostname_, then it matches the specified host or any of its subdomains.
            //
            // In the path section, each '*' matches 0 or more characters. 
            //
            var hostAndPath = pattern.Substring(colonIndex + 1, pattern.Length - colonIndex - 1);
            hostAndPath = hostAndPath
                .Replace(".", "\\.")
                .Replace("*", ".*");

            return new ChromeMatchPattern(new Regex(
                $"^{scheme}:{hostAndPath}$",
                RegexOptions.IgnoreCase));
        }
    }
}
