using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Transport
{
    /// <summary>
    /// Factory for IAP transports.
    /// 
    /// The factory maintains a pool of tunnels (implemented using
    /// SSH relay listeners). Whenever possible, the factory tries
    /// to reuse pooled tunnels.
    /// 
    /// Callers must dispose transports when they're done. Once there
    /// is no more transport that references a specific tunnel, the
    /// tunnel is closed too.
    /// 
    /// 
    /// Transport (used by a single client)
    ///   n |
    ///     |    1
    ///     +-----> Tunnel (shared)
    ///                |     1
    ///                +------> SshRelay
    ///                
    /// </summary>
    public class IapTransportFactory : IIapTransportFactory
    {
        private readonly object tunnelPoolLock;
        private readonly IDictionary<TunnelSpecification, Task<Tunnel>> tunnelPool;

        private readonly IAuthorization authorization;
        private readonly UserAgent userAgent;
        private readonly TunnelFactory tunnelFactory;

        private Task<Tunnel> GetPooledTunnelAsync(
            TunnelSpecification specification,
            TimeSpan probeTimeout,
            CancellationToken cancellationToken)
        {
            specification.Policy.ExpectNotNull(nameof(specification));
            specification.Protocol.ExpectNotNull(nameof(specification));
            specification.TargetInstance.ExpectNotNull(nameof(specification));

            lock (this.tunnelPoolLock)
            {
                if (this.tunnelPool.TryGetValue(specification, out var tunnelTask) &&
                    !tunnelTask.IsFaulted)
                {
                    //
                    // Found matching tunnel, and it looks okay.
                    //
                    CoreTraceSources.Default.TraceInformation(
                        "Using pooled tunnel for {0}", specification);
                }
                else
                {
                    //
                    // Initiate the creation of a new tunnel and hand out the task.
                    // If multiple callers request the same tunnel, they can await
                    // the same task.
                    //
                    CoreTraceSources.Default.TraceInformation(
                        "Creating new tunnel for {0}", specification);

                    tunnelTask = this.tunnelFactory.CreateTunnelAsync(
                        this.authorization,
                        this.userAgent,
                        specification,
                        probeTimeout,
                        cancellationToken);
                    this.tunnelPool[specification] = tunnelTask;
                }

                Debug.Assert(tunnelTask != null);
                return tunnelTask;
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal IapTransportFactory(
            IAuthorization authorization,
            UserAgent userAgent,
            TunnelFactory tunnelFactory)
        {
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));
            this.tunnelFactory = tunnelFactory.ExpectNotNull(nameof(tunnelFactory));

            this.tunnelPoolLock = new object();
            this.tunnelPool = new Dictionary<TunnelSpecification, Task<Tunnel>>();
        }

        //---------------------------------------------------------------------
        // IIapTransportFactory.
        //---------------------------------------------------------------------

        //TODO: Expose active tunnels

        public async Task<ITransport> CreateIapTransportAsync(
            IProtocol protocol,
            ISshRelayPolicy policy,
            InstanceLocator targetInstance,
            ushort targetPort,
            IPEndPoint localEndpoint,
            TimeSpan probeTimeout,
            CancellationToken cancellationToken)
        {
            if (localEndpoint == null)
            {
                localEndpoint = new IPEndPoint(
                    IPAddress.Loopback,
                    PortFinder.FindFreeLocalPort());
            }

            var specification = new TunnelSpecification(
                protocol,
                policy,
                targetInstance,
                targetPort,
                localEndpoint);

            var tunnel = await GetPooledTunnelAsync(
                    specification,
                    probeTimeout,
                    cancellationToken)
                .ConfigureAwait(false);

            return new Transport(
                tunnel,
                protocol,
                tunnel.IsMtlsEnabled ? TransportFlags.Mtls : TransportFlags.None);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        /// <summary>
        /// Defines all the parameters that define a tunnel. If the specification
        /// is the same, a tunnel can be shared.
        /// </summary>
        internal class TunnelSpecification : IEquatable<TunnelSpecification> 
        {
            public IProtocol Protocol { get; }
            public ISshRelayPolicy Policy { get; }
            public InstanceLocator TargetInstance { get; }
            public ushort TargetPort { get; }
            public IPEndPoint LocalEndpoint { get; }

            public TunnelSpecification(
                IProtocol protocol,
                ISshRelayPolicy policy,
                InstanceLocator targetInstance,
                ushort targetPort,
                IPEndPoint localEndpoint)
            {
                this.Policy = policy.ExpectNotNull(nameof(policy));
                this.Protocol = protocol.ExpectNotNull(nameof(protocol));
                this.TargetInstance = targetInstance.ExpectNotNull(nameof(targetInstance));
                this.TargetPort = targetPort;
                this.LocalEndpoint = localEndpoint.ExpectNotNull(nameof(localEndpoint));
            }

            public override int GetHashCode()
            {
                return
                    this.Policy.Id.GetHashCode() ^
                    this.Protocol.Id.GetHashCode() ^
                    this.TargetInstance.GetHashCode() ^
                    this.TargetPort ^
                    this.LocalEndpoint.GetHashCode();
            }

            public bool Equals(TunnelSpecification other)
            {
                return other != null &&
                    Equals(this.Policy.Id, other.Policy.Id) &&
                    Equals(this.Protocol.Id, other.Protocol.Id) &&
                    Equals(this.TargetInstance, other.TargetInstance) &&
                    this.TargetPort == other.TargetPort &&
                    Equals(this.LocalEndpoint, other.LocalEndpoint);
            }

            public override bool Equals(object obj)
            {
                return Equals((TunnelSpecification)obj);
            }

            public static bool operator ==(TunnelSpecification obj1, TunnelSpecification obj2)
            {
                if (obj1 is null)
                {
                    return obj2 is null;
                }

                return obj1.Equals(obj2);
            }

            public static bool operator !=(TunnelSpecification obj1, TunnelSpecification obj2)
            {
                return !(obj1 == obj2);
            }

            public override string ToString()
            {
                return $"{this.TargetInstance}, port: {this.TargetPort}, " +
                       $"protocol: {this.Protocol.Id}, policy: {this.Policy.Id}";
            }
        }

        /// <summary>
        /// A sharable tunnel that uses an IAP relay listener.
        /// </summary>
        internal class Tunnel : ReferenceCountedDisposableBase
        {
            private readonly CancellationTokenSource stopListenerSource;
            private readonly ISshRelayListener listener;
            private readonly Task listenTask;

            internal Tunnel(
                ISshRelayListener listener, 
                IPEndPoint localEndpoint,
                bool mtlsEnabled)
            {
                this.listener = listener.ExpectNotNull(nameof(listener));
                this.stopListenerSource = new CancellationTokenSource();
                this.listenTask = this.listener.ListenAsync(this.stopListenerSource.Token);
                this.LocalEndpoint = localEndpoint;
                this.IsMtlsEnabled = mtlsEnabled;

                Debug.Assert(localEndpoint.Port == listener.LocalPort);
            }

            internal bool IsMtlsEnabled { get; }

            internal TransportStatistics Statistics
            {
                get
                {
                    var stats = this.listener.Statistics;
                    return new TransportStatistics()
                    {
                        BytesReceived = stats.BytesReceived,
                        BytesTransmitted = stats.BytesTransmitted
                    };
                }
            }

            internal IPEndPoint LocalEndpoint { get; }

            internal Task CloseAsync()
            {
                this.stopListenerSource.Cancel();
                return this.listenTask;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    //
                    // Last reference is gone, stop the listener.
                    //
                    this.stopListenerSource.Cancel();
                }

                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Factory for tunnels (required for testability).
        /// </summary>
        internal class TunnelFactory
        {
            public async virtual Task<Tunnel> CreateTunnelAsync( // TODO: Add Integration test
                IAuthorization authorization,
                UserAgent userAgent,
                TunnelSpecification specification,
                TimeSpan probeTimeout,
                CancellationToken cancellationToken)
            {
                using (CoreTraceSources.Default.TraceMethod().WithParameters(specification))
                {
                    if (specification.LocalEndpoint.Address != IPAddress.Loopback)
                    {
                        throw new NotImplementedException(
                            "This implementation only supports loopback tunnels");
                    }

                    var clientCertificate =
                            (authorization.DeviceEnrollment != null &&
                            authorization.DeviceEnrollment.State == DeviceEnrollmentState.Enrolled)
                        ? authorization.DeviceEnrollment.Certificate
                        : null;

                    if (clientCertificate != null)
                    {
                        CoreTraceSources.Default.TraceInformation(
                            "Using client certificate (valid till {0})", clientCertificate.NotAfter);
                    }

                    var client = new IapTunnelingEndpoint(
                        authorization.Credential,
                        specification.TargetInstance,
                        specification.TargetPort,
                        IapTunnelingEndpoint.DefaultNetworkInterface,
                        userAgent,
                        clientCertificate);

                    //
                    // Check if we can actually connect to this instance before we
                    // start a local listener.
                    //
                    using (var stream = new SshRelayStream(client))
                    {
                        await stream
                            .ProbeConnectionAsync(probeTimeout) // TODO: Add Integration test -> don't pool if probe fails
                            .ConfigureAwait(false);
                    }

                    var listener = SshRelayListener.CreateLocalListener(
                        client,
                        specification.Policy,
                        specification.LocalEndpoint.Port);

                    return new Tunnel(
                        listener, 
                        specification.LocalEndpoint,
                        clientCertificate != null);
                }
            }
        }

        /// <summary>
        /// Transports are single-use, but multiple Transports might
        /// use a shared Tunnel.
        /// </summary>
        private class Transport : DisposableBase, ITransport
        {
            private readonly Tunnel tunnel;

            internal Transport(
                Tunnel relay,
                IProtocol protocol,
                TransportFlags flags)
            {
                this.tunnel = relay.ExpectNotNull(nameof(relay));
                this.Protocol = protocol.ExpectNotNull(nameof(protocol));
                this.Flags = flags;
            }

            //-----------------------------------------------------------------
            // ITransport.
            //-----------------------------------------------------------------

            public IProtocol Protocol { get; }

            public TransportFlags Flags { get; }

            public TransportStatistics Statistics => this.tunnel.Statistics;

            public IPEndPoint LocalEndpoint => this.tunnel.LocalEndpoint;

            //-----------------------------------------------------------------
            // DisposableBase.
            //-----------------------------------------------------------------

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.tunnel.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
