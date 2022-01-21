using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Socks5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocksQuickTest
{
    class Program : ISshRelayEndpointResolver
    {
        static void Main(string[] args)
        {
            var listener = new Socks5Listener(
                new Program(),
                new AllowAllRelayPolicy(),
                1080);
            listener.ListenAsync(CancellationToken.None).Wait();
        }

        public Task<ISshRelayEndpoint> ResolveEndpointAsync(
            string destinationDomain,
            ushort destinationPort,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ISshRelayEndpoint> ResolveEndpointAsync(
            IPAddress destination,
            ushort destinationPort,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
