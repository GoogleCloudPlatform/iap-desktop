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
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap
{
    /// <summary>
    /// IAP endpoint for a specific VM instance.
    /// </summary>
    public class IapInstanceEndpoint : ISshRelayEndpoint //TODO: Rename to IapInstanceTarget / SshRelayTarget
    {
        private const string SubprotocolName = "relay.tunnel.cloudproxy.app";
        private const string Origin = "bot:iap-tunneler";
        public const string DefaultNetworkInterface = "nic0"; // TODO: Move to IapClient

        // Cf. https://developers.google.com/identity/protocols/googlescopes#iapv1
        public const string RequiredScope = "https://www.googleapis.com/auth/cloud-platform";// TODO: Move to IapClient

        public InstanceLocator VmInstance { get; }

        public ushort Port { get; }

        public string Interface { get; }

        public UserAgent UserAgent { get; }

        public bool IsMutualTlsEnabled => this.ClientCertificate != null;

        private readonly Uri baseUri;
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

            return new Uri(this.baseUri, "connect?" + queryString);
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

            return new Uri(this.baseUri, "reconnect?" + queryString);
        }

        private async Task<INetworkStream> ConnectOrReconnectAsync(
            Uri endpoint,
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

            // TODO: Inject Host header

            try
            {
                // NB. User-Agent is a restricted header, so this call will fail
                // unless un-restricted using RestrictedHeaderConfigPatch.
                websocket.Options.SetRequestHeader("User-Agent", this.UserAgent.ToString());
            }
            catch (ArgumentException)
            {
                IapTraceSources.Default.TraceWarning("Failed to set User-Agent header");
            }

            if (this.ClientCertificate != null)
            {
                websocket.Options.ClientCertificates.Add(this.ClientCertificate);
            }

            try
            {
                await websocket
                    .ConnectAsync(endpoint, token)
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

        internal IapInstanceEndpoint(
            Uri baseUri,
            ICredential credential,
            InstanceLocator vmInstance,
            ushort port,
            string nic,
            UserAgent userAgent,
            X509Certificate2 clientCertificate)
        {
            this.baseUri = baseUri;
            this.credential = credential;
            this.VmInstance = vmInstance;
            this.Port = port;
            this.Interface = nic;
            this.UserAgent = userAgent;
            this.ClientCertificate = clientCertificate;
        }

        internal IapInstanceEndpoint(
            Uri baseUri,
            ICredential credential,
            InstanceLocator vmInstance,
            ushort port,
            string nic,
            UserAgent userAgent)
            : this(baseUri, credential, vmInstance, port, nic, userAgent, null)
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
