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



    public class TransportProfiles
    {
        public IProtocol GetAvailableProfiles(ITransportTarget target)
        {
            throw new NotImplementedException();
        }

    }

    public interface ITransportBroker
    {
        IEnumerable<ITransport> Active { get; }
        Task<ITransport> CreateIapTransportAsync(/*...*/);
        Task<ITransport> CreateVpcTransportAsync(/*...*/);
    }

    public interface ITransport : IDisposable // IapTransport, VpcTransport, IapOnPremTransport
    {
        ISshRelayPolicy Policy { get; }


        TransportFlags Flags { get; }

        TransportStatistics Statistics { get; }

     
        Task Probe(TimeSpan timeout);

        /// <summary>
        /// Endpoint to connect to. This might be a localhost endpoint.
        /// </summary>
        IPEndPoint LocalEndpoint { get; }

    }

    public interface ITransport<TTarget> : ITransport where TTarget : ITransportTarget
    {
        TTarget Target { get; }
    }

    [Flags]
    public enum TransportFlags
    {
        None,
        Mtls
    }

    public struct TransportStatistics
    {
        public ulong BytesReceived;
        public ulong BytesTransmitted;
    }
}
