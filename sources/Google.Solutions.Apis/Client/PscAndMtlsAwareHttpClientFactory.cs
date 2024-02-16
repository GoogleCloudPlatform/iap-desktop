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
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System.Diagnostics;
using System.Net;
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
        : IHttpClientFactory, IHttpExecuteInterceptor, IHttpUnsuccessfulResponseHandler
    {
        private readonly ServiceEndpointDirections directions;
        private readonly IDeviceEnrollment deviceEnrollment;
        private readonly ICredential? credential;
        private readonly UserAgent userAgent;

        public PscAndMtlsAwareHttpClientFactory(
            ServiceEndpointDirections directions,
            IAuthorization authorization,
            UserAgent userAgent)
        {
            this.directions = directions;
            this.deviceEnrollment = authorization.DeviceEnrollment.ExpectNotNull(nameof(this.deviceEnrollment));
            this.credential = authorization.Session.ApiCredential;
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));
        }

        public PscAndMtlsAwareHttpClientFactory(
            ServiceEndpointDirections directions,
            IDeviceEnrollment deviceEnrollment,
            UserAgent userAgent)
        {
            this.directions = directions;
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
            httpClient.MessageHandler.AddUnsuccessfulResponseHandler(this);

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

            ApiEventSource.Log.HttpRequestInitiated(
                request.Method.ToString(),
                request.RequestUri.ToString(),
                this.directions.Type.ToString(),
                this.directions.Host);

            return Task.CompletedTask;
        }

        public Task<bool> HandleResponseAsync(HandleUnsuccessfulResponseArgs args)
        {
            ApiEventSource.Log.HttpRequestFailed(
                args.Request.Method.ToString(),
                args.Request.RequestUri.ToString(),
                (int)args.Response.StatusCode);

            return Task.FromResult(false);
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
                this.directions = directions;
                this.deviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            }

            protected override HttpClientHandler CreateClientHandler()
            {
                var handler = new NtlmResilientWebRequestHandler()
                {
                    Proxy = WebRequest.DefaultWebProxy,
                };

                //
                // Bypass proxy for accessing PSC endpoint.
                //
                if (this.directions.Type == ServiceEndpointType.PrivateServiceConnect)
                {
                    handler.UseProxy = false;

                    ApiTraceSource.Log.TraceVerbose(
                        "Bypassing proxy for for endpoint {0} (Host:{1})",
                        this.directions.BaseUri,
                        this.directions.Host);
                }

                if (this.directions.UseClientCertificate &&
                    this.deviceEnrollment.Certificate != null)
                {
                    Debug.Assert(this.deviceEnrollment.State == DeviceEnrollmentState.Enrolled);

                    handler.ClientCertificates.Add(this.deviceEnrollment.Certificate);

                    ApiTraceSource.Log.TraceVerbose(
                        "Using client certificate {0} for endpoint {1} (Host:{2})",
                        this.deviceEnrollment.Certificate,
                        this.directions.BaseUri,
                        this.directions.Host);
                }

                return handler;
            }
        }

        private class NtlmResilientWebRequestHandler : WebRequestHandler
        {
            /// <summary>
            /// Number of retries to perform if NTLM proxy authentication
            /// fails.
            /// </summary>
            public static ushort ProxyAuthenticationRetries { get; set; } = 1;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                //
                // The System.Net stack supports NTLM proxy authentication and
                // works reliably when requests are submitted sequentially.
                // However, when we're sending multiple requests in parallel,
                // authentication occasionally fails with SEC_E_INVALID_TOKEN in
                // NTAuthentication.GetOutgoingBlob.
                //
                // This error seems to be caused by a race condition in the BCL,
                // but there's little we can do to avoid it. 
                //
                // When we see a 407 error with an NTLM challenge-header, we
                // are either (a) hitting the aforementioned issue or (b),
                // our credentials were simply invalid.
                //
                // If it's (a), there's a good chance that a retry helps. If it's
                // (b), a retry at least won't hurt.
                //
                for (var attempt = 0; ; attempt++)
                {
                    try
                    {
                        return await base
                            .SendAsync(request, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (HttpRequestException e) when (
                        e.InnerException is WebException webException &&
                        webException.Response is HttpWebResponse webResponse &&
                        this.UseProxy &&
                        this.Proxy?.Credentials != null &&
                        IsNtlmProxyAuthenticationRequiredResponse(webResponse))
                    {
                        var message = e.FullMessage();
                        ApiTraceSource.Log.TraceWarning(
                            "NTLM proxy authentication failed (attempt {0}/{1})): {2}",
                            attempt,
                            ProxyAuthenticationRetries,
                            message);
                        ApiEventSource.Log.HttpNtlmProxyRequestFailed(
                            webResponse.ResponseUri.AbsoluteUri,
                            attempt,
                            message);

                        if (attempt < ProxyAuthenticationRetries)
                        {
                            //
                            // Retry request.
                            //
                        }
                        else
                        {
                            //
                            // It's not worth retrying.
                            //
                            throw;
                        }
                    }
                }
            }

            private static bool IsNtlmProxyAuthenticationRequiredResponse(HttpWebResponse response)
            {
                return
                    response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired &&
                    response.Headers.Get("Proxy-Authenticate") is var proxyAuthHeader &&
                    proxyAuthHeader != null &&
                    proxyAuthHeader.StartsWith("NTLM ");
            }
        }
    }
}
