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

using Google.Solutions.Common.Util;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Client
{
    /// <summary>
    /// Defines the "route" to take for connecting to Google API
    /// endpoints.
    /// </summary>
    public class ServiceRoute
    {
        /// <summary>
        /// Use default endpoints, either via the public internet
        /// of Private Google Access.
        /// </summary>
        public static readonly ServiceRoute Public = new ServiceRoute(null);

        public ServiceRoute(string? endpoint)
        {
            this.Endpoint = endpoint;
        }

        /// <summary>
        /// Determine whether to use Private Service Connect to 
        /// connect to Google API endpoints.
        /// </summary>
        public bool UsePrivateServiceConnect
        {
            get => !string.IsNullOrEmpty(this.Endpoint);
        }

        /// <summary>
        /// Name of IP address of the PSC endpoint. Null for the public route.
        /// </summary>
        public string? Endpoint { get; }

        public override string ToString()
        {
            return this.Endpoint ?? "public";
        }

        /// <summary>
        /// Test if the route works. Can be used to validate a PSC endpoint.
        /// </summary>
        /// <exception>when connecting to the endpoint failed</exception>
        public async Task ProbeAsync(TimeSpan timeout)
        {
            //
            // NB. It doesn't matter much which .googleapis.com endpoint
            // we probe, so we just use the compute one.
            //
            const string apiHostForProbing = "compute.googleapis.com";

            var uri = new UriBuilder()
            {
                Scheme = "https",
                Host = this.UsePrivateServiceConnect
                    ? this.Endpoint
                    : apiHostForProbing,
                Path = "/generate_204"
            }.Uri;

            using (var cts = new CancellationTokenSource())
            using (var handler = new HttpClientHandler()
            {
                //
                // Bypass proxy for accessing PSC endpoint.
                //
                UseProxy = !this.UsePrivateServiceConnect
            })
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            using (var client = new HttpClient(handler))
            {
                cts.CancelAfter(timeout);

                if (this.UsePrivateServiceConnect &&
                    IPAddress.TryParse(this.Endpoint, out var _))
                {
                    //
                    // The PSC endpoint is an IP address (as opposed
                    // to a DNS name like www-endpoint.p.googleapis.com).
                    //
                    // Explicitly set the host header so that certificate
                    // validation works.
                    //
                    request.Headers.Host = apiHostForProbing;
                }

                try
                {
                    var response = await client
                        .SendAsync(
                            request,
                            HttpCompletionOption.ResponseHeadersRead,
                            cts.Token)
                        .ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();
                }
                catch (Exception e)
                {
                    throw new InvalidServiceRouteException(
                        $"Probing the endpoint '{this}' failed", e.Unwrap());
                }
            }
        }
    }

    public class InvalidServiceRouteException : ClientException
    {
        internal InvalidServiceRouteException(
            string message,
            Exception inner)
            : base(message, inner)
        {
        }
    }
}
