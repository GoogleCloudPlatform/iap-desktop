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
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Net;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Iap
{
    /// <summary>
    /// Cloud IAP endpoint for establishing SSH Relay tunnels.
    /// </summary>
    public class IapTunnelingEndpoint : ISshRelayEndpoint
    {
        private const string TlsBaseUri = "wss://mtls.tunnel.cloudproxy.app/v4/";
        private const string MtlsBaseUri = "wss://mtls.tunnel.cloudproxy.app/v4/";
        private const string SubprotocolName = "relay.tunnel.cloudproxy.app";
        private const string Origin = "bot:iap-tunneler";
        public const string DefaultNetworkInterface = "nic0";

        // Cf. https://developers.google.com/identity/protocols/googlescopes#iapv1
        public const string RequiredScope = "https://www.googleapis.com/auth/cloud-platform";

        public InstanceLocator VmInstance { get; }

        public ushort Port { get; }

        public string Interface { get; }

        public UserAgent UserAgent { get; }

        private readonly ICredential credential;

        private X509Certificate2 ClientCertificate { get; }

        private Uri CreateUri(
            string sid,
            ulong ack)
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

            if (sid != null)
            {
                urlParams["sid"] = sid;
                urlParams["ack"] = ack.ToString();
            }

            var queryString = string.Join(
                "&",
                urlParams.Select(kvp => kvp.Key + "=" + WebUtility.UrlEncode(kvp.Value)));

            return new Uri(
                (this.ClientCertificate == null ? TlsBaseUri : MtlsBaseUri) +
                (sid == null ? "connect" : "reconnect") +
                "?" +
                queryString);
        }

        public IapTunnelingEndpoint(
            ICredential credential,
            InstanceLocator vmInstance,
            ushort port,
            string nic,
            UserAgent userAgent,
            X509Certificate2 clientCertificate)
        {
            this.credential = credential;
            this.VmInstance = vmInstance;
            this.Port = port;
            this.Interface = nic;
            this.UserAgent = userAgent;
            this.ClientCertificate = clientCertificate;
        }

        public IapTunnelingEndpoint(
            ICredential credential,
            InstanceLocator vmInstance,
            ushort port,
            string nic,
            UserAgent userAgent)
            : this(credential, vmInstance, port, nic, userAgent, null)
        { }

        public Task<INetworkStream> ConnectAsync(CancellationToken token)
        {
            return ReconnectAsync(null, 0, token);
        }

        public async Task<INetworkStream> ReconnectAsync(
            string sid,
            ulong ack,
            CancellationToken token)
        {
            //
            // Configure web socket.
            //
            var accessToken = await this.credential.GetAccessTokenForRequestAsync(
                null,
                token).ConfigureAwait(false);

            var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(SubprotocolName);
            websocket.Options.SetRequestHeader("Authorization", "Bearer " + accessToken);
            websocket.Options.SetRequestHeader("Origin", Origin);
            websocket.Options.KeepAliveInterval = TimeSpan.FromMinutes(1);

            try
            {
                // NB. User-Agent is a restricted header, so this call will fail
                // unless un-restricted using RestrictedHeaderConfigPatch.
                websocket.Options.SetRequestHeader("User-Agent", this.UserAgent.ToHeaderValue());
            }
            catch (ArgumentException)
            {
                TraceSources.Compute.TraceWarning("Failed to set User-Agent header");
            }

            if (this.ClientCertificate != null)
            {
                websocket.Options.ClientCertificates.Add(this.ClientCertificate);
            }

            await websocket.ConnectAsync(
                CreateUri(sid, ack),
                token).ConfigureAwait(false);

            return new WebSocketStream(websocket, (int)DataMessage.MaxTotalLength);
        }
    }
}
