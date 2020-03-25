using Google.Solutions.Compute;
using Google.Solutions.Compute.Iap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services
{
    public class Tunnel
    {
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly SshRelayListener listener;
        private readonly IapTunnelingEndpoint endpoint;

        public TunnelDestination Destination => new TunnelDestination(
            this.Endpoint.VmInstance, this.Endpoint.Port);

        public int LocalPort => listener.LocalPort;
        public int? ProcessId => null;

        public IapTunnelingEndpoint Endpoint => endpoint;

        public Tunnel(
            IapTunnelingEndpoint endpoint,
            SshRelayListener listener,
            CancellationTokenSource cancellationTokenSource)
        {
            this.endpoint = endpoint;
            this.listener = listener;
            this.cancellationTokenSource = cancellationTokenSource;
        }

        public void Close()
        {
            this.cancellationTokenSource.Cancel();
        }

        public async Task Probe(TimeSpan timeout)
        {
            // Probe connection to fail fast if there is an 'access denied'
            // issue.
            using (var stream = new SshRelayStream(this.Endpoint))
            {
                await stream.TestConnectionAsync(timeout).ConfigureAwait(false);
            }
        }
    }

    public class TunnelDestination : IEquatable<TunnelDestination>
    {
        public VmInstanceReference Instance { get; private set; }

        public ushort RemotePort { get; private set; }

        public TunnelDestination(VmInstanceReference instance, ushort remotePort)
        {
            this.Instance = instance;
            this.RemotePort = remotePort;
        }

        public bool Equals(TunnelDestination other)
        {
            return
                other != null &&
                other.Instance.Equals(this.Instance) &&
                other.RemotePort == this.RemotePort;
        }

        public override bool Equals(object obj)
        {
            return obj is TunnelDestination && Equals((TunnelDestination)obj);
        }

        public override int GetHashCode()
        {
            return this.Instance.GetHashCode() ^
                (int)this.RemotePort;
        }

        public override string ToString()
        {
            return $"{this.Instance.InstanceName}:{this.RemotePort}";
        }
    }
}
