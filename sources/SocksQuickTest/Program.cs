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
    class Program : ISocks5Relay
    {
        static void Main(string[] args)
        {
            var listener = new Socks5Listener(
                new Program(),
                1080);
            listener.ListenAsync(CancellationToken.None).Wait();
        }

        public Task<ushort> CreateRelayPortAsync(
            IPEndPoint clientEndpoint, 
            string destinationHost, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
