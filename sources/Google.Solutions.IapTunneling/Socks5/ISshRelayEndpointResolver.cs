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
    public interface ISshRelayEndpointResolver
    {
        Task<ISshRelayEndpoint> ResolveEndpointAsync(
            string destinationDomain,
            CancellationToken cancellationToken);
    }
}
