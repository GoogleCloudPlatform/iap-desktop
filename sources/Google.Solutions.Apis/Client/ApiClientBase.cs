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

using Google.Apis.Services;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;

namespace Google.Solutions.Apis.Client
{
    /// <summary>
    /// Base class for Google API clients.
    /// </summary>
    public abstract class ApiClientBase : IClient
    {
        protected ApiClientBase(
            IServiceEndpoint endpoint,
            IAuthorization authorization,
            UserAgent userAgent)
        {
            Precondition.ExpectNotNull(endpoint, nameof(endpoint));
            Precondition.ExpectNotNull(authorization, nameof(authorization));
            Precondition.ExpectNotNull(userAgent, nameof(userAgent));

            var directions = endpoint.GetDirections(
                authorization.DeviceEnrollment?.State ?? DeviceEnrollmentState.NotEnrolled);

            ApiTraceSources.Default.TraceInformation(
                "Using endpoint {0}",
                directions);

            this.Endpoint = endpoint;
            this.Initializer = new BaseClientService.Initializer()
            {
                BaseUri = directions.BaseUri.ToString(),
                HttpClientFactory = new PscAndMtlsAwareHttpClientFactory(
                    directions,
                    authorization,
                    userAgent)
            };
        }

        public IServiceEndpoint Endpoint { get; }

        /// <summary>
        /// Initializer for creating XxxService objects.
        /// </summary>
        protected internal BaseClientService.Initializer Initializer { get; }
    }
}
