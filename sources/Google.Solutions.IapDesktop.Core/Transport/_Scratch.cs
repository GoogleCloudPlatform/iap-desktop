using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Transport
{
    /*
        Target + Selector[] => Profile[]
        Target + Profile => Context
        Context => Transport

      
     */



    public interface ITransportBroker : IDisposable
    {
        IEnumerable<ITransport> Active { get; }

        // create IapTransport, VpcTransport, IapOnPremTransport

        Task<ITransport> CreateIapTransportAsync(/*...*/);
        Task<ITransport> CreateVpcTransportAsync(/*...*/);
    }

    public interface ITransport<TTarget> : ITransport where TTarget : ITransportTarget
    {
        TTarget Target { get; }
    }
}
