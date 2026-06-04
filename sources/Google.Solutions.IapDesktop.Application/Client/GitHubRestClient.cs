//
// Copyright 2026 Google LLC
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
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Client
{
    /// <summary>
    /// Client for the GitHub API that makes unauthenticated requests
    /// by default, and authenticated requests when receiving 
    /// 401 or 403 errors.
    /// </summary>
    public class GitHubRestClient : IExternalRestClient
    {
        //
        // Use the same clients for all connections to benefit
        // from connection pooling.
        //
        private readonly RestClient unauthenticatedClient;
        private readonly RestClient authenticatedClient;

        public GitHubRestClient(ClientSecrets clientCredentials)
        {
            this.unauthenticatedClient = new RestClient(
                Install.UserAgent, 
                null);
            this.authenticatedClient = new RestClient(
                Install.UserAgent, 
                clientCredentials);
        }

        public async Task<TModel?> GetAsync<TModel>(
            Uri url, 
            CancellationToken cancellationToken)
            where TModel : class
        {
            try
            {
                //
                // Try request without credentials.
                //
                // NB. By trying without credentials first, we make sure that
                //     credential revocation can't lead to a denial of service.
                //
                using (ApplicationTraceSource.Log.TraceMethod().WithParameters(url))
                {
                    return await this.unauthenticatedClient
                        .GetAsync<TModel>(url.ToString(), cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (RestClientException e) when (
                e.StatusCode == HttpStatusCode.Unauthorized||
                e.StatusCode == HttpStatusCode.Forbidden) 
            {
                //
                // Retry request with credentials.
                //
                using (ApplicationTraceSource.Log.TraceMethod().WithParameters(
                    url, 
                    "authenticated"))
                {
                    return await this.authenticatedClient
                        .GetAsync<TModel>(url.ToString(), cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            this.unauthenticatedClient.Dispose();
            this.authenticatedClient.Dispose();
        }
    }
}
