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
        /// Determine the effective base URI to use.
        /// </summary>
        Uri GetEffectiveUri(
            DeviceEnrollmentState enrollment,
            out ServiceEndpointType endpointType);
    }

    public enum ServiceEndpointType
    {
        Tls,
        MutualTls,
        PrivateServiceConnect
    }

    public interface IEndpointAdapter // TODO: separate file
    {
        IServiceEndpoint Endpoint { get; }
    }

    public class ServiceEndpoint<T> : IServiceEndpoint
        where T : IEndpointAdapter
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
                      Host = tlsUri.Host.ToLower().Replace(".googleapis.com", ".mtls.googleapis.com"),
                  }.Uri)
        {
        }

        public ServiceEndpoint(string tlsUri)
            : this(new Uri(tlsUri))
        { }

        /// <summary>
        /// Alternate hostname to use for Private Service Connect (PSC).
        /// </summary>
        public string PscEndpointOverride { get; set; }

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

        public Uri GetEffectiveUri(
            DeviceEnrollmentState enrollment,
            out ServiceEndpointType endpointType)
        {
            if (!string.IsNullOrEmpty(this.PscEndpointOverride)) 
            {
                //
                // Use an alternate PSC endpoint.
                //
                // NB. PSC trumps mTLS.
                //
                endpointType = ServiceEndpointType.PrivateServiceConnect;
                return new UriBuilder(this.CanonicalUri)
                {
                    Host = this.PscEndpointOverride
                }.Uri;
            }
            else if (enrollment == DeviceEnrollmentState.Enrolled)
            {
                //
                // Device is enrolled and we have a device certificate -> use mTLS.
                //
                endpointType = ServiceEndpointType.MutualTls;
                return this.MtlsUri;
            }
            else
            {
                //
                // Use the regular endpoint.
                //
                endpointType = ServiceEndpointType.Tls;
                return this.CanonicalUri;
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string ToString()
        {
            return $"{this.CanonicalUri} (mTLS: {this.MtlsUri}, PSC: {this.PscEndpointOverride})";
        }
    }
}
