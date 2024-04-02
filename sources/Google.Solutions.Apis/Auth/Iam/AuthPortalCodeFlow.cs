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
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Http;
using Google.Apis.Util;
using Google.Solutions.Apis.Client;
using System;
using System.Net.Http.Headers;
using System.Text;

namespace Google.Solutions.Apis.Auth.Iam
{
    /// <summary>
    /// Authorization code flow that uses the workforce identity
    /// "auth portal".
    /// </summary>
    internal class AuthPortalCodeFlow : AuthorizationCodeFlow
    {
        private readonly Initializer initializer;

        public AuthPortalCodeFlow(Initializer initializer) : base(initializer)
        {
            this.initializer = initializer;
        }

        public override AuthorizationCodeRequestUrl CreateAuthorizationCodeRequest(string redirectUri)
        {
            return new RequestUrl(new Uri(this.initializer.AuthorizationServerUrl))
            {
                ClientId = this.initializer.ClientId,
                Scope = string.Join(" ", base.Scopes),
                RedirectUri = redirectUri,
                ProviderName = this.initializer.Provider.ToString()
            };
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        internal new class Initializer : AuthorizationCodeFlow.Initializer
        {
            private const string AuthorizationUrl = "https://auth.cloud.google/authorize";

            public WorkforcePoolProviderLocator Provider { get; set; }

            public string ClientId { get; }

            protected Initializer(
                ServiceEndpointDirections directions,
                IDeviceEnrollment deviceEnrollment,
                WorkforcePoolProviderLocator provider,
                ClientSecrets clientSecrets,
                UserAgent userAgent)
                : base(
                      AuthorizationUrl,
                      new Uri(directions.BaseUri, "/v1/oauthtoken").ToString())
            {
                this.Provider = provider;

                //
                // Unlike the Gaia API, the /v1/oauthtoken API expects client
                // credentials to be passed as Basic auth header.
                //
                // Trying to pass client credentials as POST paramarers
                // would cause a HTTP/400 error.
                //
                // Initialize the base class with basic credentials so that it
                // won't inject ant POST parameters, and configure the underlying
                // HTTP client to always inject a Basic auth header instead.
                //
                this.ClientId = clientSecrets.ClientId;
                this.ClientSecrets = new ClientSecrets();

                var clientSecretAuth = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{clientSecrets.ClientId}:{clientSecrets.ClientSecret}"));

                this.HttpClientFactory = new AuthenticatedClientFactory(
                    new PscAndMtlsAwareHttpClientFactory(
                        directions,
                        deviceEnrollment,
                        userAgent),
                    new AuthenticationHeaderValue("Basic", clientSecretAuth));

                ApiTraceSource.Log.TraceInformation(
                    "Using endpoint {0} and client {1}",
                    directions,
                    clientSecrets.ClientId);
            }

            public Initializer(
                ServiceEndpoint<WorkforcePoolClient> endpoint,
                IDeviceEnrollment deviceEnrollment,
                WorkforcePoolProviderLocator provider,
                ClientSecrets clientSecrets,
                UserAgent userAgent)
                : this(
                      endpoint.GetDirections(deviceEnrollment.State),
                      deviceEnrollment,
                      provider,
                      clientSecrets,
                      userAgent)
            {
            }
        }

        private class RequestUrl : AuthorizationCodeRequestUrl
        {
            public RequestUrl(Uri authorizationServerUrl)
                : base(authorizationServerUrl)
            {
            }

            [RequestParameter("provider_name", RequestParameterType.Query)]
            public string? ProviderName { get; set; }
        }

        private class AuthenticatedClientFactory : IHttpClientFactory
        {
            private readonly IHttpClientFactory factory;
            private readonly AuthenticationHeaderValue authenticationHeader;

            public AuthenticatedClientFactory(
                IHttpClientFactory factory,
                AuthenticationHeaderValue authenticationHeader)
            {
                this.factory = factory;
                this.authenticationHeader = authenticationHeader;
            }

            public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
            {
                var client = this.factory.CreateHttpClient(args);
                client.DefaultRequestHeaders.Authorization = this.authenticationHeader;
                return client;
            }
        }
    }
}
