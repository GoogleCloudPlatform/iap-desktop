//
// Copyright 2019 Google LLC
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
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap
{
    /// <summary>
    /// IAP target for a specific VM instance.
    /// </summary>
    public class IapInstanceTarget : ISshRelayTarget
    {
        private const string SubprotocolName = "relay.tunnel.cloudproxy.app";
        private const string Origin = "bot:iap-tunneler";

        // Cf. https://developers.google.com/identity/protocols/googlescopes#iapv1
        public const string RequiredScope = "https://www.googleapis.com/auth/cloud-platform";

        public InstanceLocator VmInstance { get; }

        public ushort Port { get; }

        public string Interface { get; }

        public UserAgent UserAgent { get; }

        public bool IsMutualTlsEnabled => this.endpointDetails.UseClientCertificate;

        private readonly ServiceEndpointDetails endpointDetails;
        private readonly ICredential credential;

        internal X509Certificate2 ClientCertificate { get; }

        private Uri CreateConnectUri()
        {
            var urlParams = new Dictionary<string, string>
            {
                { "project", this.VmInstance.ProjectId },
                { "zone", this.VmInstance.Zone },
                { "instance", this.VmInstance.Name },
                { "interface", this.Interface },
                { "port", this.Port.ToString() },
                { "_", Guid.NewGuid().ToString() }  // Cache buster.
            };
            var queryString = string.Join(
                "&",
                urlParams.Select(kvp => kvp.Key + "=" + WebUtility.UrlEncode(kvp.Value)));

            return new Uri(this.endpointDetails.BaseUri, "connect?" + queryString);
        }

        private Uri CreateReconnectUri(
            string sid,
            ulong ack)
        {
            var urlParams = new Dictionary<string, string>
            {
                { "sid", sid },
                { "ack", ack.ToString() },
                { "zone", this.VmInstance.Zone },
                { "_", Guid.NewGuid().ToString() }  // Cache buster.
            };

            var queryString = string.Join(
                "&",
                urlParams.Select(kvp => kvp.Key + "=" + WebUtility.UrlEncode(kvp.Value)));

            return new Uri(this.endpointDetails.BaseUri, "reconnect?" + queryString);
        }

        private async Task<INetworkStream> ConnectOrReconnectAsync(
            Uri requestUri,
            CancellationToken token)
        {
            //
            // Configure web socket.
            //
            var accessToken = await this.credential
                .GetAccessTokenForRequestAsync(null, token)
                .ConfigureAwait(false);

            var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(SubprotocolName);
            websocket.Options.SetRequestHeader("Authorization", "Bearer " + accessToken);
            websocket.Options.SetRequestHeader("Origin", Origin);
            websocket.Options.KeepAliveInterval = TimeSpan.FromMinutes(1);

            if (this.endpointDetails.Type == ServiceEndpointType.PrivateServiceConnect)
            {
                Debug.Assert(!string.IsNullOrEmpty(this.endpointDetails.Host));
                Debug.Assert(requestUri.Host != this.endpointDetails.Host);

                //
                // We're using PSC, so hostname we're sending the request to isn't
                // the hostname that the server expects to see in the Host header.
                //
                // Sadly, the ClientWebSocket doesn't let us specify a Host header
                // (the unrestricted header trick won't work here), so we have to rely
                // on another patch.
                //

                Debug.Assert(SystemPatch.SetUsernameAsHostHeaderForWssRequests.IsInstalled);
                if (!SystemPatch.SetUsernameAsHostHeaderForWssRequests.IsInstalled)
                {
                    throw new InvalidOperationException(
                        "This system does not support IAP over private service connect");
                }

                requestUri = new UriBuilder(requestUri)
                {
                    //
                    // Stash the hostname as username and rely on the system patch
                    // to take and apply it as Host header.
                    //
                    UserName = this.endpointDetails.Host
                }.Uri;
            }

            try
            {
                Debug.Assert(SystemPatch.UnrestrictUserAgentHeader.IsInstalled);

                //
                // NB. User-Agent is a restricted header, so this call fails
                // unless un-restricted using UnrestrictUserAgentHeader.
                //
                websocket.Options.SetRequestHeader("User-Agent", this.UserAgent.ToString());
            }
            catch (ArgumentException)
            {
                IapTraceSources.Default.TraceWarning("Failed to set User-Agent header");
            }

            if (this.endpointDetails.UseClientCertificate)
            {
                Debug.Assert(this.ClientCertificate != null);
                websocket.Options.ClientCertificates.Add(this.ClientCertificate);
            }

            try
            {
                await websocket
                    .ConnectAsync(requestUri, token)
                    .ConfigureAwait(false);

                return new WebSocketStream(websocket);
            }
            catch (WebSocketException e)
                when (e.InnerException is WebException webException &&
                      webException?.Response is HttpWebResponse httpResponse &&
                      httpResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                //
                // Connection rejected for reasons unrelated to IAM/access levels,
                // such as a proxy rejecting WebSocket connections.
                //
                throw new WebSocketConnectionDeniedException();
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal IapInstanceTarget(
            ServiceEndpointDetails endpointDetails,
            ICredential credential,
            InstanceLocator vmInstance,
            ushort port,
            string nic,
            UserAgent userAgent,
            X509Certificate2 clientCertificate)
        {
            this.endpointDetails = endpointDetails;
            this.credential = credential;
            this.VmInstance = vmInstance;
            this.Port = port;
            this.Interface = nic;
            this.UserAgent = userAgent;
            this.ClientCertificate = clientCertificate;
        }

        internal IapInstanceTarget(
            ServiceEndpointDetails endpointDetails,
            ICredential credential,
            InstanceLocator vmInstance,
            ushort port,
            string nic,
            UserAgent userAgent)
            : this(endpointDetails, credential, vmInstance, port, nic, userAgent, null)
        { }

        /// <summary>
        /// Perform a probe to check whether the instance can be reached,
        /// and whether access is permitted.
        /// </summary>
        public async Task ProbeAsync(TimeSpan timeout)
        {
            using (var stream = new SshRelayStream(this))
            {
                await stream
                    .ProbeConnectionAsync(timeout)
                    .ConfigureAwait(false);
            }
        }

        //---------------------------------------------------------------------
        // ISshRelayEndpoint.
        //---------------------------------------------------------------------

        public Task<INetworkStream> ConnectAsync(CancellationToken token)
        {
            return ConnectOrReconnectAsync(CreateConnectUri(), token);
        }

        public Task<INetworkStream> ReconnectAsync(
            string sid,
            ulong ack,
            CancellationToken token)
        {
            return ConnectOrReconnectAsync(CreateReconnectUri(sid, ack), token);
        }
    }
}
