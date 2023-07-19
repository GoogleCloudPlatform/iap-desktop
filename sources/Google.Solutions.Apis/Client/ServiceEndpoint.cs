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
        /// Determine the effective endpoint to use, given
        /// the device enrollment state.
        /// </summary>
        ServiceEndpointDetails GetDetails(DeviceEnrollmentState enrollment);
    }

    /// <summary>
    /// Effective endpoi nt data to use for a Google API.
    /// </summary>
    public struct ServiceEndpointDetails
    {
        /// <summary>
        /// Type of endpoint.
        /// </summary>
        public ServiceEndpointType Type { get; }

        /// <summary>
        /// Base URI to use for sending requests.
        /// </summary>
        public Uri BaseUri { get; }

        /// <summary>
        /// Host header to inject.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Determines whether a client certificate must be used.
        /// </summary>
        public bool UseClientCertificate => this.Type == ServiceEndpointType.MutualTls;

        internal ServiceEndpointDetails(ServiceEndpointType type, Uri baseUri, string host)
        {
            this.Type = type;
            this.BaseUri = baseUri;
            this.Host = host;
        }
    }

    public enum ServiceEndpointType
    {
        Tls,
        MutualTls,
        PrivateServiceConnect
    }

    public class ServiceEndpoint<T> : IServiceEndpoint
        where T : IClient
    {
        public ServiceEndpoint(Uri tlsUri, Uri mtlsUri)
        {
            this.CanonicalUri = tlsUri.ExpectNotNull(nameof(tlsUri));
            this.MtlsUri = mtlsUri.ExpectNotNull(nameof(mtlsUri));

            Debug.Assert(!tlsUri.Host.Contains("mtls."));
            Debug.Assert(mtlsUri.Host.Contains("mtls."));
        }

        public ServiceEndpoint(Uri tlsUri)
            : this(
                  tlsUri,
                  new UriBuilder(tlsUri)
                  {
                      Host = tlsUri.Host
                          .ToLower()
                          .Replace(".googleapis.com", ".mtls.googleapis.com"),
                  }.Uri)
        {
        }

        public ServiceEndpoint(string tlsUri)
            : this(new Uri(tlsUri))
        { }

        /// <summary>
        /// Alternate hostname to use for Private Service Connect (PSC).
        /// </summary>
        public string PscHostOverride { get; set; }

        /// <summary>
        /// Default URI to use, if neither mTLS or PSC is required.
        /// </summary>
        public Uri CanonicalUri { get; }

        /// <summary>
        /// MTLS variant of the same endpoint.
        /// </summary>
        public Uri MtlsUri { get; }

        //---------------------------------------------------------------------
        // IServiceEndpoint.
        //---------------------------------------------------------------------

        public ServiceEndpointDetails GetDetails(DeviceEnrollmentState enrollment)
        {
            if (!string.IsNullOrEmpty(this.PscHostOverride)) 
            {
                //
                // Use an alternate PSC endpoint.
                //
                // NB. PSC trumps mTLS.
                //
                return new ServiceEndpointDetails(
                    ServiceEndpointType.PrivateServiceConnect,
                    new UriBuilder(this.CanonicalUri)
                    {
                        Host = this.PscHostOverride
                    }.Uri,
                    this.CanonicalUri.Host);
            }
            else if (enrollment == DeviceEnrollmentState.Enrolled)
            {
                //
                // Device is enrolled and we have a device certificate -> use mTLS.
                //
                return new ServiceEndpointDetails(
                    ServiceEndpointType.MutualTls,
                    this.MtlsUri,
                    this.MtlsUri.Host);
            }
            else
            {
                //
                // Use the regular endpoint.
                //
                return new ServiceEndpointDetails(
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
            return $"{this.CanonicalUri} (mTLS: {this.MtlsUri}, PSC: {this.PscHostOverride})";
        }
    }
}
