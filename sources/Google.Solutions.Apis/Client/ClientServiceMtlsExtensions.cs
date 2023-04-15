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

using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Solutions.Common.Util;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Apis.Client
{
    public static class ClientServiceMtlsExtensions
    {
        /// <summary>
        /// Enable mTLS/device certificate authentication.
        /// </summary>
        public static void EnableDeviceCertificateAuthentication(
            this BaseClientService.Initializer initializer,
            string mtlsBaseUrl,
            X509Certificate2 deviceCertificate)
        {
            Precondition.ExpectNotNull(initializer, nameof(initializer));
            Precondition.ExpectNotEmpty(mtlsBaseUrl, nameof(mtlsBaseUrl));
            Precondition.ExpectNotNull(deviceCertificate, nameof(deviceCertificate));
            Debug.Assert(mtlsBaseUrl.Contains(".mtls."));

            if (HttpClientHandlerExtensions.IsClientCertificateSupported)
            {
                ApiTraceSources.Google.TraceInformation(
                    "Enabling MTLS for {0}",
                    mtlsBaseUrl);

                //
                // Switch to mTLS endpoint.
                //
                initializer.BaseUri = mtlsBaseUrl;

                //
                // Add client certificate.
                //
                initializer.HttpClientFactory = new MtlsHttpClientFactory(deviceCertificate);
            }
        }

        /// <summary>
        /// Enable mTLS/device certificate authentication.
        /// </summary>
        public static void EnableDeviceCertificateAuthentication(
            this AuthorizationCodeFlow.Initializer initializer,
            X509Certificate2 deviceCertificate)
        {
            Precondition.ExpectNotNull(initializer, nameof(initializer));
            Precondition.ExpectNotNull(deviceCertificate, nameof(deviceCertificate));

            if (HttpClientHandlerExtensions.IsClientCertificateSupported)
            {
                ApiTraceSources.Google.TraceInformation("Enabling MTLS for OAuth");

                //
                // Add client certificate.
                //
                initializer.HttpClientFactory = new MtlsHttpClientFactory(deviceCertificate);
            }
        }

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

        //---------------------------------------------------------------------
        // Helper classes.
        //---------------------------------------------------------------------

        internal class MtlsHttpClientFactory : HttpClientFactory
        {
            private readonly X509Certificate2 clientCertificate;

            public MtlsHttpClientFactory(X509Certificate2 clientCertificate)
            {
                this.clientCertificate = clientCertificate;
            }

            protected override HttpClientHandler CreateClientHandler()
            {
                var handler = base.CreateClientHandler();
                var added = handler.TryAddClientCertificate(this.clientCertificate);
                Debug.Assert(added);
                return handler;
            }
        }
    }
}
