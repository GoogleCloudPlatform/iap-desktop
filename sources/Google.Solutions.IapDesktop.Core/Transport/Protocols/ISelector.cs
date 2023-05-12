using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Transport.Protocols
{
    public interface ISelector
    {
        bool Matches(ITransportTarget target);
    }
}
