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

using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Linq;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Google.Solutions.IapDesktop.Application
{
    /// <summary>
    /// Represents an iap-rdp:/// URI.
    /// 
    /// Rules:
    /// * The host part is empty, so a URL has to start with iap-rdp:/ or iap-rdp:///
    /// * The query string may contain settings, but not all settings are supported
    ///   (either for security reasons or because they are just not very relevant).
    /// </summary>
    public class IapRdpUrl
    {
        public const string Scheme = "iap-rdp";

        private static readonly Regex ProjectPattern 
            = new Regex(@"(?:(?:[-a-z0-9]{1,63}\.)*(?:[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?):)?(?:[0-9]{1,19}|(?:[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?))");
        private static readonly Regex ZonePattern 
            = new Regex(@"[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?");
        private static readonly Regex InstanceNamePattern 
            = new Regex(@"[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?|[1-9][0-9]{0,19}");

        /// <summary>
        /// Instance referenced by this URL.
        /// </summary>
        public InstanceLocator Instance { get; }

        /// <summary>
        /// Query string parameters that might contain connection settings.
        /// 
        /// NB. The NameValueCollection is case-insensitive.
        /// </summary>
        public NameValueCollection Parameters { get; }

        public IapRdpUrl(InstanceLocator instance, NameValueCollection parameters)
        {
            this.Instance = instance;
            this.Parameters = parameters;
        }

        //---------------------------------------------------------------------
        // Parsing.
        //---------------------------------------------------------------------

        private static InstanceLocator CreateInstanceLocatorFromPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                throw new IapRdpUrlFormatException($"Path is empty");
            }

            if (!absolutePath.StartsWith("/"))
            {
                throw new IapRdpUrlFormatException($"Path must start with /");
            }

            var pathComponents = absolutePath.Split('/');
            if (pathComponents.Length != 4)
            {
                throw new IapRdpUrlFormatException(
                    $"Path not in format project/zone/instance-name: {absolutePath}");
            }

            Debug.Assert(string.IsNullOrEmpty(pathComponents[0]));

            if (!ProjectPattern.IsMatch(pathComponents[1]))
            {
                throw new IapRdpUrlFormatException($"Invalid project ID");
            }

            if (!ZonePattern.IsMatch(pathComponents[2]))
            {
                throw new IapRdpUrlFormatException($"Invalid zone ID");
            }

            if (!InstanceNamePattern.IsMatch(pathComponents[3]))
            {
                throw new IapRdpUrlFormatException($"Invalid instance name");
            }

            return new InstanceLocator(pathComponents[1], pathComponents[2], pathComponents[3]);
        }

        public static IapRdpUrl FromString(string uri)
        {
            return FromUri(new Uri(uri));
        }

        public static IapRdpUrl FromUri(Uri uri)
        {
            if (uri.Scheme != Scheme)
            {
                throw new IapRdpUrlFormatException($"Invalid scheme: {uri.Scheme}");
            }

            if (!string.IsNullOrEmpty(uri.Host))
            {
                throw new IapRdpUrlFormatException($"Host part not empty: {uri.Host}");
            }

            var instanceRef = CreateInstanceLocatorFromPath(uri.AbsolutePath);

            return new IapRdpUrl(
                instanceRef,
                HttpUtility.ParseQueryString(uri.Query));
        }

        //---------------------------------------------------------------------
        // Formatting.
        //---------------------------------------------------------------------

        public string ToString(bool includeQuery)
        {
            var url = $"{Scheme}:///{this.Instance.ProjectId}/{this.Instance.Zone}/{this.Instance.Name}";

            if (includeQuery)
            {
                var formattedParameters = this.Parameters
                    .ToKeyValuePairs()
                    .Select(p => p.Key + "=" + HttpUtility.UrlEncode(p.Value));
                url += $"?{string.Join("&", formattedParameters)}";
            }

            return url;
        }

        public override string ToString() => ToString(true);
    }

    public class IapRdpUrlFormatException : UriFormatException, IExceptionWithHelpTopic
    {
        public IHelpTopic Help => HelpTopics.BrowserIntegration;

        public IapRdpUrlFormatException(string message) : base(message)
        {
        }
    }
}
