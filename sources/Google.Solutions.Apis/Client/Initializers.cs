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
    internal static class Initializers
    {
        /// <summary>
        /// Create an initializer for API services.
        /// </summary>
        public static BaseClientService.Initializer CreateServiceInitializer(
            IServiceEndpoint endpoint,
            IAuthorization authorization,
            UserAgent userAgent)
        {
            Precondition.ExpectNotNull(endpoint, nameof(endpoint));
            Precondition.ExpectNotNull(authorization, nameof(authorization));
            Precondition.ExpectNotNull(userAgent, nameof(userAgent));

            var endpointDetails = endpoint.GetDetails(
                authorization.DeviceEnrollment?.State ?? DeviceEnrollmentState.NotEnrolled);

            return new BaseClientService.Initializer()
            {
                BaseUri = endpointDetails.BaseUri.ToString(),
                ApplicationName = userAgent.ToApplicationName(),
                HttpClientInitializer = new PscAwareHttpClientInitializer(
                    endpointDetails,
                    authorization.Credential),
                HttpClientFactory = new MtlsAwareHttpClientFactory(
                    endpointDetails,
                    authorization.DeviceEnrollment)
            };
        }
    }
}
