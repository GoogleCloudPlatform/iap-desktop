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
using Google.Solutions.Common.Util;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;

namespace Google.Solutions.Apis.Client
{
    internal static class ClientServiceMtlsExtensions
    {
        /// <summary>
        /// Check if device certificate authentication is enabled.
        /// </summary>
        public static bool IsDeviceCertificateAuthenticationEnabled(
            this IClientService service)
        {
            Precondition.ExpectNotNull(service, nameof(service));

            var dcaEnabled = IsDcaEnabledForHandler(service.HttpClient.MessageHandler);
            Debug.Assert(dcaEnabled == service.BaseUri.Contains(".mtls."));

            return dcaEnabled;

            bool IsDcaEnabledForHandler(HttpMessageHandler handler)
            {
                if (handler is DelegatingHandler delegatingHandler)
                {
                    return IsDcaEnabledForHandler(delegatingHandler.InnerHandler);
                }
                else if (handler is HttpClientHandler httpHandler)
                {
                    return httpHandler.GetClientCertificates().Any();
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
