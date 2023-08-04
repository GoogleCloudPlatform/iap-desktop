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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Client
{
    /// <summary>
    /// Client factory that enables client certificate and adds 
    /// PSC-style Host headers if needed.
    /// </summary>
    internal class PscAndMtlsAwareHttpClientFactory 
        : IHttpClientFactory, IHttpExecuteInterceptor
    {
        private readonly ServiceEndpointDirections directions;
        private readonly IDeviceEnrollment deviceEnrollment;
        private readonly ICredential credential;
        private readonly UserAgent userAgent;

        public PscAndMtlsAwareHttpClientFactory(
            ServiceEndpointDirections directions,
            IAuthorization authorization,
            UserAgent userAgent)
        {
            this.directions = directions.ExpectNotNull(nameof(directions));
            this.deviceEnrollment = authorization.DeviceEnrollment.ExpectNotNull(nameof(this.deviceEnrollment));
            this.credential = authorization.Credential;
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));
        }

        public PscAndMtlsAwareHttpClientFactory(
            ServiceEndpointDirections directions,
            IDeviceEnrollment deviceEnrollment,
            UserAgent userAgent)
        {
            this.directions = directions.ExpectNotNull(nameof(directions));
            this.deviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            this.credential = null;
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));
        }

        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
        {
            if (this.credential != null)
            {
                args.Initializers.Add(this.credential);
            }

            args.ApplicationName = this.userAgent.ToApplicationName();

            var factory = new MtlsAwareHttpClientFactory(this.directions, this.deviceEnrollment);
            var httpClient = factory.CreateHttpClient(args);

            httpClient.MessageHandler.AddExecuteInterceptor(this);

            return httpClient;
        }

        public Task InterceptAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (this.directions.Type == ServiceEndpointType.PrivateServiceConnect)
            {
                Debug.Assert(!string.IsNullOrEmpty(this.directions.Host));

                //
                // We're using PSC, the so hostname we're using to connect is
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

                    var added = handler.TryAddClientCertificate(this.deviceEnrollment.Certificate);
                    Debug.Assert(added);
                }

                return handler;
            }
        }
    }
}
