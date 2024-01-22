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
using System.Diagnostics;

namespace Google.Solutions.Apis.Client
{
    /// <summary>
    /// An endpoint for a Google API.
    /// </summary>
    public interface IServiceEndpoint
    {
        /// <summary>
        /// Get directions for connecting to the endpoint, considering
        /// mTLS and PSC.
        /// </summary>
        ServiceEndpointDirections GetDirections(
            DeviceEnrollmentState enrollment);
    }

    public class ServiceEndpoint<T> : IServiceEndpoint
        where T : IClient
    {
        private readonly ServiceRoute pscDirections;

        public ServiceEndpoint(
            ServiceRoute pscDirections,
            Uri tlsUri,
            Uri? mtlsUri)
        {
            this.pscDirections = pscDirections.ExpectNotNull(nameof(pscDirections));
            this.CanonicalUri = tlsUri.ExpectNotNull(nameof(tlsUri));
            this.MtlsUri = mtlsUri; // Optional.

            Debug.Assert(!tlsUri.Host.Contains("mtls."));
            Debug.Assert(mtlsUri == null || mtlsUri.Host.Contains("mtls."));
        }

        public ServiceEndpoint(
            ServiceRoute pscDirections,
            Uri tlsUri)
            : this(
                  pscDirections,
                  tlsUri,
                  new UriBuilder(tlsUri)
                  {
                      Host = tlsUri.Host
                          .ToLower()
                          .Replace(".googleapis.com", ".mtls.googleapis.com"),
                  }.Uri)
        {
        }

        public ServiceEndpoint(
            ServiceRoute pscDirections,
            string tlsUri)
            : this(pscDirections, new Uri(tlsUri))
        { }

        /// <summary>
        /// Default URI to use, if neither mTLS or PSC is required.
        /// </summary>
        public Uri CanonicalUri { get; }

        /// <summary>
        /// MTLS variant of the same endpoint.
        /// </summary>
        public Uri? MtlsUri { get; }

        //---------------------------------------------------------------------
        // IServiceEndpoint.
        //---------------------------------------------------------------------

        public ServiceEndpointDirections GetDirections(DeviceEnrollmentState enrollment)
        {
            if (this.pscDirections.UsePrivateServiceConnect)
            {
                //
                // Use an alternate PSC endpoint.
                //
                // NB. PSC trumps mTLS.
                //
                return new ServiceEndpointDirections(
                    ServiceEndpointType.PrivateServiceConnect,
                    new UriBuilder(this.CanonicalUri)
                    {
                        Host = this.pscDirections.Endpoint
                    }.Uri,
                    this.CanonicalUri.Host);
            }
            else if (enrollment == DeviceEnrollmentState.Enrolled && this.MtlsUri != null)
            {
                //
                // Device is enrolled and we have a device certificate -> use mTLS.
                //
                return new ServiceEndpointDirections(
                    ServiceEndpointType.MutualTls,
                    this.MtlsUri,
                    this.MtlsUri.Host);
            }
            else
            {
                //
                // Use the regular endpoint.
                //
                return new ServiceEndpointDirections(
                    ServiceEndpointType.Tls,
                    this.CanonicalUri,
                    this.CanonicalUri.Host);
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string ToString()
        {
            var psc = this.pscDirections.UsePrivateServiceConnect
                ? this.pscDirections.Endpoint
                : "off";
            return $"{this.CanonicalUri} (mTLS: {this.MtlsUri}, PSC: {psc})";
        }
    }
}
