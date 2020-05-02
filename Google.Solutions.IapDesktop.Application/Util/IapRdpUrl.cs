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

using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Web;

namespace Google.Solutions.IapDesktop.Application.Util
{
    /// <summary>
    /// Represents an iap-rdp:/// URI.
    /// 
    /// Rules:
    /// * The host part is empty, so a URL has to start with iap-rdp:/ or iap-rdp:///
    /// * The query string may contains settings, but not all settings are supported
    ///   (either for security reasons or beause they are just not very relevant).
    /// </summary>
    public class IapRdpUrl
    {
        public const string Scheme = "iap-rdp";

        private static readonly Regex ProjectPattern = new Regex(@"(?:(?:[-a-z0-9]{1,63}\.)*(?:[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?):)?(?:[0-9]{1,19}|(?:[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?))");
        private static readonly Regex ZonePattern = new Regex(@"[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?");
        private static readonly Regex InstanceNamePattern = new Regex(@"[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?|[1-9][0-9]{0,19}");

        public VmInstanceReference Instance { get; }
        public VmInstanceSettings Settings { get; }

        public IapRdpUrl(VmInstanceReference instance, VmInstanceSettings settings)
        {
            this.Instance = instance;
            this.Settings = settings;
        }

        private static VmInstanceReference CreateVmInstanceReferenceFromPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                throw new IapRdpUrlFormatException($"Path is empty");
            }

            if (!absolutePath.StartsWith("/"))
            {
                throw new IapRdpUrlFormatException($"Path must start with /");
            }

            string[] pathComponents = absolutePath.Split('/');
            if (pathComponents.Length != 4)
            {
                throw new IapRdpUrlFormatException($"Path not in format project/zone/instance-name: {absolutePath}");
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

            return new VmInstanceReference(pathComponents[1], pathComponents[2], pathComponents[3]);
        }

        private static TEnum GetEnumFromQuery<TEnum>(
            NameValueCollection collection,
            string key,
            TEnum defaultValue) where TEnum : struct
        {
            var value = collection.Get(key);
            if (value != null &&
                Enum.TryParse<TEnum>(value, out TEnum result) &&
                Enum.IsDefined(typeof(TEnum), result))
            {
                return result;
            }
            else
            {
                return defaultValue;
            }
        }

        private static VmInstanceSettings CreateVmInstanceSettingsFromQuery(
            VmInstanceReference instanceRef,
            string queryString)
        {
            var query = HttpUtility.ParseQueryString(queryString);

            return new VmInstanceSettings()
            {
                InstanceName = instanceRef.InstanceName,

                Username = query.Get("Username"),
                Domain = query.Get("Domain"),
                ConnectionBar = GetEnumFromQuery(query, "ConnectionBar", RdpConnectionBarState._Default),
                DesktopSize = GetEnumFromQuery(query, "DesktopSize", RdpDesktopSize._Default),
                AuthenticationLevel = GetEnumFromQuery(query, "AuthenticationLevel", RdpAuthenticationLevel._Default),
                ColorDepth = GetEnumFromQuery(query, "ColorDepth", RdpColorDepth._Default),
                AudioMode = GetEnumFromQuery(query, "AudioMode", RdpAudioMode._Default),
                RedirectClipboard = GetEnumFromQuery(query, "RedirectClipboard", RdpRedirectClipboard._Default)
            };
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

            var instanceRef = CreateVmInstanceReferenceFromPath(uri.AbsolutePath);

            return new IapRdpUrl(
                instanceRef,
                CreateVmInstanceSettingsFromQuery(instanceRef, uri.Query));
        }

        public string ToString(bool includeSettingsAsQuery)
        {
            var url = $"{Scheme}:///{this.Instance.ProjectId}/{this.Instance.Zone}/{this.Instance.InstanceName}";

            if (includeSettingsAsQuery)
            {
                var parameters = new Dictionary<string, string>()
                {
                    { "Username", this.Settings.Username },
                    { "Domain", this.Settings.Domain },
                    { "ConnectionBar", ((int)this.Settings.ConnectionBar).ToString() },
                    { "DesktopSize", ((int)this.Settings.DesktopSize).ToString() },
                    { "AuthenticationLevel", ((int)this.Settings.AuthenticationLevel).ToString() },
                    { "ColorDepth", ((int)this.Settings.ColorDepth).ToString() },
                    { "AudioMode", ((int)this.Settings.AudioMode).ToString() },
                    { "RedirectClipboard", ((int)this.Settings.RedirectClipboard).ToString() },
                };

                var formattedParameters = parameters.Select(p => p.Key + "=" + HttpUtility.UrlEncode(p.Value));
                url += $"?{String.Join("&", formattedParameters)}";
            }

            return url;
        }
        public override string ToString() => ToString(true);
    }

    [Serializable]
    public class IapRdpUrlFormatException : UriFormatException
    {
        protected IapRdpUrlFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public IapRdpUrlFormatException(string message) : base(message)
        {
        }
    }
}
