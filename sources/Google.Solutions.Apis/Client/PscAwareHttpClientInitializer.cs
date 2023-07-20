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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Solutions.Common.Util;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Client
{
    /// <summary>
    /// Client initializer that injects a Host header if PSC is enabled.
    /// </summary>
    internal class PscAwareHttpClientInitializer
        : IConfigurableHttpClientInitializer, IHttpExecuteInterceptor
    {
        private readonly ServiceEndpointDetails endpointDetails;
        private readonly ICredential credential;

        public PscAwareHttpClientInitializer(
            ServiceEndpointDetails endpointDetails,
            ICredential credential)
        {
            this.endpointDetails = endpointDetails.ExpectNotNull(nameof(endpointDetails));
            this.credential = credential;
        }

        public void Initialize(ConfigurableHttpClient httpClient)
        {
            if (this.credential != null)
            {
                this.credential.Initialize(httpClient);
            }

            httpClient.MessageHandler.AddExecuteInterceptor(this);
        }

        public Task InterceptAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (this.endpointDetails.Type == ServiceEndpointType.PrivateServiceConnect)
            {
                Debug.Assert(!string.IsNullOrEmpty(this.endpointDetails.Host));

                //
                // We're using PSC, thw so hostname we're using to connect is
                // different than what the server expects.
                //
                Debug.Assert(request.RequestUri.Host != this.endpointDetails.Host);

                //
                // Inject the normal hostname so that certificate validation works.
                //
                request.Headers.Host = this.endpointDetails.Host;
            }

            return Task.CompletedTask;
        }
    }
}
