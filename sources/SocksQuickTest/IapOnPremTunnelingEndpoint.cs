using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Net;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocksQuickTest
{
    public class IapOnPremTunnelingEndpoint : ISshRelayEndpoint
    {
        private const string TlsBaseUri = "wss://tunnel.cloudproxy.app/v4/";
        private const string MtlsBaseUri = "wss://mtls.tunnel.cloudproxy.app/v4/";
        private const string SubprotocolName = "relay.tunnel.cloudproxy.app";
        private const string Origin = "bot:iap-tunneler";
        public const string DefaultNetworkInterface = "nic0";

        // Cf. https://developers.google.com/identity/protocols/googlescopes#iapv1
        public const string RequiredScope = "https://www.googleapis.com/auth/cloud-platform";

        public NetworkEndpointLocator NetworkEndpoint { get; }

        public ushort Port { get; }

        public string Interface { get; }

        public UserAgent UserAgent { get; }

        public bool IsMutualTlsEnabled => this.clientCertificate != null;

        private readonly ICredential credential;

        private readonly X509Certificate2 clientCertificate;

        private Uri CreateUri(
            string sid,
            ulong ack)
        {
            var urlParams = new Dictionary<string, string>
            {
                { "project", this.NetworkEndpoint.ProjectId },
                { "network", this.NetworkEndpoint.Network },
                { "region", this.NetworkEndpoint.Region },
                { "host", this.NetworkEndpoint.Name },
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
                (this.clientCertificate == null ? TlsBaseUri : MtlsBaseUri) +
                (sid == null ? "connect" : "reconnect") +
                "?" +
                queryString);
        }

        public IapOnPremTunnelingEndpoint(
            ICredential credential,
            NetworkEndpointLocator locator,
            ushort port,
            string nic,
            UserAgent userAgent,
            X509Certificate2 clientCertificate)
        {
            this.credential = credential;
            this.NetworkEndpoint = locator;
            this.Port = port;
            this.Interface = nic;
            this.UserAgent = userAgent;
            this.clientCertificate = clientCertificate;
        }

        public IapOnPremTunnelingEndpoint(
            ICredential credential,
            NetworkEndpointLocator locator,
            ushort port,
            string nic,
            UserAgent userAgent)
            : this(credential, locator, port, nic, userAgent, null)
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
            }

            if (this.clientCertificate != null)
            {
                websocket.Options.ClientCertificates.Add(this.clientCertificate);
            }

            try
            {
                await websocket.ConnectAsync(
                    CreateUri(sid, ack),
                    token).ConfigureAwait(false);

                return new WebSocketStream(websocket, (int)ushort.MaxValue);
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
    }
}
