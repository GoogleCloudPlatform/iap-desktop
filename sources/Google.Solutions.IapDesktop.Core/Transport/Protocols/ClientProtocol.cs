using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Transport.Protocols
{
    public class ClientProtocol : IProtocol
    {
        public IEnumerable<ISelector> Selectors { get; }

        //---------------------------------------------------------------------
        // IProtocol.
        //---------------------------------------------------------------------

        public string Name { get; }


        public bool IsAvailable(ITransportTarget target)
        {
            //
            // If any selector matches, then the protocol is available.
            //
            return this.Selectors
                .EnsureNotNull()
                .Any(s => s.Matches(target));
        }

        public Task<IProtocolContext> CreateContextAsync(
            ITransportTarget target,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }

    public class ClientProtocolContext : IProtocolContext
    {
        //---------------------------------------------------------------------
        // IProtocolContext.
        //---------------------------------------------------------------------

        public Task<ITransport> ConnectTransportAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class LaunchableClientProtocolContext : ClientProtocolContext
    {
        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public Task LaunchAppAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
