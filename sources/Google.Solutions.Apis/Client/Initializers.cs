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
using Google.Apis.Services;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Auth.OAuth2.Flows;
using System;
using static Google.Solutions.Apis.Auth.SignInClient;
using System.Net;

namespace Google.Solutions.Apis.Client
{
    internal static class Initializers 
    {
        /// <summary>
        /// Create an initializer for API services that configures PSC
        /// and mTLS.
        /// </summary>
        public static BaseClientService.Initializer CreateServiceInitializer(
            IServiceEndpoint endpoint,
            IAuthorization authorization,
            UserAgent userAgent)
        {
            Precondition.ExpectNotNull(endpoint, nameof(endpoint));
            Precondition.ExpectNotNull(authorization, nameof(authorization));
            Precondition.ExpectNotNull(userAgent, nameof(userAgent));

            ApiTraceSources.Default.TraceInformation(
                "Initializing client for endpoint {0}",
                endpoint);

            var directions = endpoint.GetDirections(
                authorization.DeviceEnrollment?.State ?? DeviceEnrollmentState.NotEnrolled);

            return new BaseClientService.Initializer()
            {
                BaseUri = directions.BaseUri.ToString(),
                ApplicationName = userAgent.ToApplicationName(),
                HttpClientInitializer = new PscAwareHttpClientInitializer(
                    directions,
                    authorization.Credential),
                HttpClientFactory = new MtlsAwareHttpClientFactory(
                    directions,
                    authorization.DeviceEnrollment)
            };
        }

        /// <summary>
        /// Create an initializer for OAuth that configures PSC and mTLS.
        /// </summary>
        public static OpenIdInitializer CreateOpenIdInitializer(
            ServiceEndpoint<AuthorizationClient> accountsEndpoint,
            ServiceEndpoint<OAuthClient> oauthEndpoint,
            ServiceEndpoint<OpenIdClient> openIdEndpoint,
            IDeviceEnrollment enrollment)
        {
            Precondition.ExpectNotNull(accountsEndpoint, nameof(accountsEndpoint));
            Precondition.ExpectNotNull(oauthEndpoint, nameof(oauthEndpoint));
            Precondition.ExpectNotNull(openIdEndpoint, nameof(openIdEndpoint));
            Precondition.ExpectNotNull(enrollment, nameof(enrollment));

            ApiTraceSources.Default.TraceInformation(
                "Initializing client for endpoints {0}, {1}, {2}",
                accountsEndpoint,
                oauthEndpoint,
                openIdEndpoint);

            var accountDirections = accountsEndpoint.GetDirections(
                enrollment?.State ?? DeviceEnrollmentState.NotEnrolled);

            var oauthDirections = oauthEndpoint.GetDirections(
                enrollment?.State ?? DeviceEnrollmentState.NotEnrolled);

            var openIdDirections = openIdEndpoint.GetDirections(
                enrollment?.State ?? DeviceEnrollmentState.NotEnrolled);

            return new OpenIdInitializer(
                new Uri(accountDirections.BaseUri, "/o/oauth2/v2/auth"),
                new Uri(oauthDirections.BaseUri, "/token"),
                new Uri(oauthDirections.BaseUri, "/revoke"),
                new Uri(openIdDirections.BaseUri, "/v1/userinfo"))
            {
                HttpClientFactory = new MtlsAwareHttpClientFactory(
                    accountDirections,
                    enrollment)
            };
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public class OpenIdInitializer : GoogleAuthorizationCodeFlow.Initializer
        {
            public Uri UserInfoUrl { get; }

            public OpenIdInitializer(
                Uri authorizationServerUrl, 
                Uri tokenServerUrl,
                Uri revokeTokenUrl,
                Uri userInfoUrl) 
                : base(
                      authorizationServerUrl.ToString(),
                      tokenServerUrl.ToString(), 
                      revokeTokenUrl.ToString())
            {
                this.UserInfoUrl = userInfoUrl;
            }
        }

        /// <summary>
        /// Client factory that enables client certificate authenticateion
        /// if the device is enrolled.
        /// </summary>
        private class MtlsAwareHttpClientFactory : HttpClientFactory
        {
            private readonly ServiceEndpointDirections directions;
            private readonly IDeviceEnrollment deviceEnrollment;

            public MtlsAwareHttpClientFactory(
                ServiceEndpointDirections directions,
                IDeviceEnrollment deviceEnrollment)
            {
                this.directions = directions.ExpectNotNull(nameof(directions));
                this.deviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            }

            protected override HttpClientHandler CreateClientHandler()
            {
                var handler = base.CreateClientHandler();

                if (this.directions.UseClientCertificate &&
                    HttpClientHandlerExtensions.CanUseClientCertificates)
                {
                    Debug.Assert(this.deviceEnrollment.State == DeviceEnrollmentState.Enrolled);
                    Debug.Assert(this.deviceEnrollment.Certificate != null);

                    ApiTraceSources.Default.TraceInformation("Enabling MTLS");

                    var added = handler.TryAddClientCertificate(this.deviceEnrollment.Certificate);
                    Debug.Assert(added);
                }

                return handler;
            }
        }

        /// <summary>
        /// Client initializer that injects a Host header if PSC is enabled.
        /// </summary>
        private class PscAwareHttpClientInitializer
            : IConfigurableHttpClientInitializer, IHttpExecuteInterceptor
        {
            private readonly ServiceEndpointDirections directions;
            private readonly ICredential credential;

            public PscAwareHttpClientInitializer(
                ServiceEndpointDirections directions,
                ICredential credential)
            {
                this.directions = directions.ExpectNotNull(nameof(directions));
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
                if (this.directions.Type == ServiceEndpointType.PrivateServiceConnect)
                {
                    Debug.Assert(!string.IsNullOrEmpty(this.directions.Host));

                    //
                    // We're using PSC, thw so hostname we're using to connect is
                    // different than what the server expects.
                    //
                    Debug.Assert(request.RequestUri.Host != this.directions.Host);

                    //
                    // Inject the normal hostname so that certificate validation works.
                    //
                    request.Headers.Host = this.directions.Host;
                }

                return Task.CompletedTask;
            }
        }
    }
}
