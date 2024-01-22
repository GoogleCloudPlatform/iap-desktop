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

using Google.Solutions.Common;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Client
{
    public class RestClient : IDisposable
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

        public RestClient(UserAgent userAgent)
            : this(
                  new HttpClient(),
                  userAgent)
        {
        }

        /// <summary>
        /// User agent to add to HTTP requests.
        /// </summary>
        public UserAgent UserAgent { get; }

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
                if (this.UserAgent != null)
                {
                    request.Headers.UserAgent.ParseAdd(this.UserAgent.ToString());
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
                        response.EnsureSuccessStatusCode();

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
}
