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
using Google.Solutions.Common;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Iap
{
    /// <summary>
    /// Cloud IAP endpoint for establishing SSH Relay tunnels.
    /// </summary>
    public class IapTunnelingEndpoint : ISshRelayEndpoint
    {
        private const string BaseUri = "wss://tunnel.cloudproxy.app/v4/";
        private const string SubprotocolName = "relay.tunnel.cloudproxy.app";
        private const string Origin = "bot:iap-tunneler";
        public const string DefaultNetworkInterface = "nic0";

        // Cf. https://developers.google.com/identity/protocols/googlescopes#iapv1
        public const string RequiredScope = "https://www.googleapis.com/auth/cloud-platform";

        public VmInstanceReference VmInstance { get; }

        public ushort Port { get; }

        public string Interface { get; }

        private readonly ICredential credential;

        public IapTunnelingEndpoint(ICredential credential, VmInstanceReference vmInstance, ushort port, string nic)
        {
            this.credential = credential;
            this.VmInstance = vmInstance;
            this.Port = port;
            this.Interface = nic;
        }

        public Task<INetworkStream> ConnectAsync(CancellationToken token)
        {
            return ReconnectAsync(null, 0, token);
        }

        public async Task<INetworkStream> ReconnectAsync(
            string sid,
            ulong ack,
            CancellationToken token)
        {
            var urlParams = new Dictionary<string, string>
            {
                { "project", this.VmInstance.ProjectId },
                { "zone", this.VmInstance.Zone },
                { "instance", this.VmInstance.InstanceName },
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

            var uri = new Uri(
                BaseUri +
                (sid == null ? "connect" : "reconnect") +
                "?" +
                queryString);

            var accessToken = await this.credential.GetAccessTokenForRequestAsync(
                null,
                token).ConfigureAwait(false);

            var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(SubprotocolName);
            websocket.Options.SetRequestHeader("Authorization", "Bearer " + accessToken);
            websocket.Options.SetRequestHeader("Origin", Origin);
            websocket.Options.KeepAliveInterval = TimeSpan.FromMinutes(1);

            await websocket.ConnectAsync(uri, token).ConfigureAwait(false);

            return new WebSocketStream(websocket, (int)DataMessage.MaxTotalLength);
        }
    }
}
