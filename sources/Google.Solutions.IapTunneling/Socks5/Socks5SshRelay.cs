using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapTunneling.Iap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Socks5
{
    public interface ISocks5RelayContext
    {
        Task<ISshRelayEndpoint> CreateEndpointAsync(
            string destinationDomain,
            CancellationToken cancellationToken);
    }

    public class Socks5SshRelay : ISocks5Relay
    {
        private readonly ISocks5RelayContext context;
        private readonly ISshRelayPolicy policy;

        public async Task<ushort> CreateRelayPortAsync(
            IPEndPoint clientEndpoint, 
            string destinationHost, 
            CancellationToken cancellationToken)
        {
            //
            // Check if the client is allowed at all.
            //
            if (this.policy.IsClientAllowed(clientEndpoint))
            {
                IapTraceSources.Default.TraceInformation(
                    "Connection from {0} to {1} allowed by policy",
                    clientEndpoint,
                    destinationHost);
            }
            else
            {
                IapTraceSources.Default.TraceWarning(
                    "Connection from {0} to {1} rejected by policy", 
                    clientEndpoint,
                    destinationHost);
                throw new UnauthorizedException("Connection rejected by policy");
            }

            //
            // Resolve the SOCKS-style domain name to an actual endpoint.
            // 
            var endpoint = await this.context
                .CreateEndpointAsync(destinationHost, cancellationToken)
                .ConfigureAwait(false);

            //
            // Create a new listener and keep it alive for a single connection.
            //
            // Use the same policy so that the client is checked again
            // when connecting to the listener.
            //
            var relayListener = SshRelayListener.CreateLocalListener(
                endpoint,
                this.policy);
            relayListener.ClientAcceptLimit = 1;

            #pragma warning disable CS4014 // Call not awaited
            relayListener.ListenAsync(cancellationToken)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        IapTraceSources.Default.TraceError(
                            "Socks5SshRelay: Connection failed", t.Exception);
                    }
                });
            #pragma warning restore CS4014 

            return (ushort)relayListener.LocalPort;
        }
    }
}
