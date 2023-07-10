//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Google.Solutions.Apis.Client
{
    public class ServiceEndpointResolver
    {
        private readonly ConcurrentDictionary<string, string> pscEndpoints 
            = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Register a private service connect endpoint (for ex, 
        /// compute.p.google.com) that should be used instead of the 
        /// canonical endpoint.
        /// </summary>
        public void AddPrivateServiceEndpoint(
            string canonicalHostname, 
            string overrideHostname)
        {
            canonicalHostname.ExpectNotEmpty(nameof(canonicalHostname));
            overrideHostname.ExpectNotEmpty(nameof(overrideHostname));

            Debug.Assert(canonicalHostname != overrideHostname);

            this.pscEndpoints[canonicalHostname.ToLower()] = overrideHostname.ToLower();
        }

        /// <summary>
        /// Select the right endpoint to use, considering mTLS and PSC.
        /// </summary>
        public ServiceEndpoint ResolveEndpoint(
            ServiceEndpointTemplate template,
            DeviceEnrollmentState enrollment)
        {
            template.ExpectNotNull(nameof(template));

            if (this.pscEndpoints.TryGetValue(
                template.TlsUri.Host.ToLower(),
                out var newHost))
            {
                //
                // Use an alternate PSC endpoint.
                //
                // NB. PSC trumps mTLS.
                //
                return new ServiceEndpoint(
                    new UriBuilder(template.TlsUri)
                    {
                        Host = newHost
                    }.Uri,
                    EndpointType.PrivateServiceConnect);
            }
            else if (enrollment == DeviceEnrollmentState.Enrolled)
            {
                //
                // Device is enrolled and we have a device certificate -> use mTLS.
                //
                return new ServiceEndpoint(template.MtlsUri, EndpointType.MutualTls);
            }
            else
            {
                //
                // Use the regular endpoint.
                //
                return new ServiceEndpoint(template.TlsUri, EndpointType.Tls);
            }
        }
    }

    public class ServiceEndpointTemplate
    {
        public Uri TlsUri { get; }
        public Uri MtlsUri { get; }

        public ServiceEndpointTemplate(Uri tlsUri, Uri mtlsUri)
        {
            this.TlsUri = tlsUri.ExpectNotNull(nameof(tlsUri));
            this.MtlsUri = mtlsUri.ExpectNotNull(nameof(mtlsUri));

            Debug.Assert(mtlsUri.Host.Contains("mtls."));
        }

        public ServiceEndpointTemplate(Uri tlsUri)
            : this(
                  tlsUri,
                  new UriBuilder(tlsUri)
                  {
                      Host = tlsUri.Host.ToLower().Replace(".googleapis.com", ".mtls.googleapis.com"),
                  }.Uri)
        {
        }

        public ServiceEndpointTemplate(string tlsUri)
            : this(new Uri(tlsUri))
        { }
    }
}
