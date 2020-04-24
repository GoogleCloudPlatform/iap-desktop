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
using Google.Solutions.IapDesktop.Application.Settings;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Google.Solutions.IapDesktop.Application.Util
{
    /// <summary>
    /// Represents an iap-rdp:/// URI.
    /// 
    /// Rules:
    /// * The host part is empty, so a URL has to start with iap-rdp:/ or iap-rdp:///
    /// </summary>
    public class IapRdpUrl
    {
        public const string Scheme = "iap-rdp";

        private static Regex ProjectPattern = new Regex(@"(?:(?:[-a-z0-9]{1,63}\.)*(?:[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?):)?(?:[0-9]{1,19}|(?:[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?))");
        private static Regex ZonePattern = new Regex(@"[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?");
        private static Regex InstanceNamePattern = new Regex(@"[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?|[1-9][0-9]{0,19}");


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

            Debug.Assert(pathComponents[0] == string.Empty);

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

            if (uri.Host != string.Empty)
            {
                throw new IapRdpUrlFormatException($"Host part not empty: {uri.Host}");
            }

            // Consider extracting settings from query string.
            return FromInstanceReference(CreateVmInstanceReferenceFromPath(uri.AbsolutePath));
        }

        public static IapRdpUrl FromInstanceReference(VmInstanceReference instanceRef)
        {
            return new IapRdpUrl(
                instanceRef,
                new VmInstanceSettings()
                {
                    InstanceName = instanceRef.InstanceName
                });
        }

        public override string ToString()
        {
            return $"{Scheme}:///{this.Instance.ProjectId}/{this.Instance.Zone}/{this.Instance.InstanceName}";
        }
    }

    public class IapRdpUrlFormatException : UriFormatException
    {
        public IapRdpUrlFormatException(string message) : base(message)
        {
        }
    }
}
