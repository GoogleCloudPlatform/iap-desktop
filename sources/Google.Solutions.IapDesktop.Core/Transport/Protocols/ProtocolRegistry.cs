using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Transport.Protocols
{
    public class AppProtocolRegistry
    {
        public IEnumerable<AppProtocol> Protocols { get; }

        public IProtocol GetAvailableProtocols(ITransportTarget target)
        {
            throw new NotImplementedException();
        }

        public void RegisterProtocol( /*...*/ )
        {
            throw new NotImplementedException();
        }
    }
}
