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

using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Client
{
    /// <summary>
    /// Client for external REST resources.
    /// </summary>
    public interface IExternalRestClient : IDisposable
    {
        /// <summary>
        /// Perform a GET request and derialize the JSON response.
        /// </summary>
        /// <returns>null if not found or empty</returns>
        Task<TModel?> GetAsync<TModel>(
            Uri url,
            CancellationToken cancellationToken)
            where TModel : class;
    }

    public sealed class ExternalRestClient : IExternalRestClient
    {
        //
        // Use the same client for all connections to benefit
        // from connection pooling.
        //
        private readonly RestClient client = new RestClient(Install.UserAgent);

        public async Task<TModel?> GetAsync<TModel>(Uri url, CancellationToken cancellationToken)
            where TModel : class
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(url))
            {
                return await this.client
                    .GetAsync<TModel>(url.ToString(), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            this.client.Dispose();
        }
    }
}
