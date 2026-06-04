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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Client
{
    public sealed class RestClient : IDisposable
    {
        //
        // Use a custom timeout (default is 100sec).
        //
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        //
        // Underlying HTTP client. We keep using the same client so
        // that we can benefit from the underlying connection pool.
        //
        private readonly HttpClient client;

        internal RestClient(
            HttpClient client,
            UserAgent userAgent)
        {
            this.client = client.ExpectNotNull(nameof(client));
            this.UserAgent = userAgent.ExpectNotNull(nameof(userAgent));

            this.client.Timeout = DefaultTimeout;
        }

        public RestClient(
            UserAgent userAgent,
            ClientSecrets? clientCredentials)
            : this(
                  new HttpClient(),
                  userAgent)
        {
            this.ClientCredentials = clientCredentials;
        }

        /// <summary>
        /// User agent to add to HTTP requests.
        /// </summary>
        public UserAgent UserAgent { get; }

        /// <summary>
        /// Client credentials to pass as Basic authentication header.
        /// </summary>
        public ClientSecrets? ClientCredentials { get; }

        /// <summary>
        /// Perform a GET request.
        /// </summary>
        public async Task<TModel?> GetAsync<TModel>(
            string url,
            CancellationToken cancellationToken)
            where TModel : class
        {
            using (CommonTraceSource.Log.TraceMethod().WithParameters(url))
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.UserAgent.ParseAdd(this.UserAgent.ToString());

                if (this.ClientCredentials != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(
                            Encoding.ASCII.GetBytes(
                                string.Join(
                                    ":",
                                    this.ClientCredentials.ClientId,
                                    this.ClientCredentials.ClientSecret))));
                }

                try
                {
                    using (var response = await this.client
                        .SendAsync(
                            request,
                            HttpCompletionOption.ResponseHeadersRead,
                            cancellationToken)
                        .ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new RestClientException(
                                response.StatusCode,
                                response.ReasonPhrase);
                        }

                        var stream = await response.Content
                            .ReadAsStreamAsync()
                            .ConfigureAwait(false);

                        using (var reader = new StreamReader(stream))
                        using (var jsonReader = new JsonTextReader(reader))
                        {
                            return new JsonSerializer().Deserialize<TModel>(jsonReader);
                        }
                    }
                }
                catch (OperationCanceledException)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    //
                    // NB. SendAsync throws a TaskCanceledException (subclass of OperationCanceledException)
                    // if a timeout occurs.
                    //
                    throw new TimeoutException(
                        $"The request to {url} did not complete within the allotted time");
                }
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.client.Dispose();
        }
    }

    public class RestClientException : HttpRequestException
    {
        public HttpStatusCode StatusCode { get; }

        internal RestClientException(
            HttpStatusCode statusCode, 
            string message) : base(message)
        {
            this.StatusCode = statusCode;
        }
    }
}
