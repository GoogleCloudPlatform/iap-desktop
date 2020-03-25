using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Solutions.IapDesktop.Application.ObjectModel;

namespace Google.Solutions.IapDesktop.Application.Services
{
    public class TunnelBrokerService
    {
        private readonly TunnelService tunnelService;
        private readonly object tunnelsLock = new object();
        private readonly IDictionary<TunnelDestination, Task<Tunnel>> tunnels =
            new Dictionary<TunnelDestination, Task<Tunnel>>();

        public TunnelBrokerService(IServiceProvider serviceProvider)
        {
            this.tunnelService = serviceProvider.GetService<TunnelService>();
        }

        public IEnumerable<Tunnel> OpenTunnels =>
            this.tunnels.Values
                .Where(t => t.IsCompleted && !t.IsFaulted)
                .Select(t => t.Result);

        private Task<Tunnel> ConnectAndCacheAsync(TunnelDestination endpoint)
        {
            var tunnel = this.tunnelService.CreateTunnelAsync(endpoint);
            this.tunnels[endpoint] = tunnel;
            return tunnel;
        }

        public bool IsConnected(TunnelDestination endpoint)
        {
            lock (this.tunnelsLock)
            {
                if (this.tunnels.TryGetValue(endpoint, out Task<Tunnel> tunnel))
                {
                    return !tunnel.IsFaulted;
                }
                else
                {
                    return false;
                }
            }
        }

        private Task<Tunnel> ConnectIfNecessaryAsync(TunnelDestination endpoint)
        {
            lock (this.tunnelsLock)
            {
                if (!this.tunnels.TryGetValue(endpoint, out Task<Tunnel> tunnel))
                {
                    return ConnectAndCacheAsync(endpoint);
                }
                else if (tunnel.IsFaulted)
                {
                    // There is no point in handing out a faulty attempt
                    // to create a tunnel. So start anew.
                    return ConnectAndCacheAsync(endpoint);
                }
                else
                {
                    // This tunnel is good or still in the process
                    // of connecting.
                    return tunnel;
                }
            }
        }

        public async Task<Tunnel> ConnectAsync(TunnelDestination endpoint, TimeSpan timeout)
        {
            var tunnel = await ConnectIfNecessaryAsync(endpoint);

            // Whether it is a new or existing tunnel, probe it first before 
            // handing it out. It might be broken after all (because of reauth
            // or for other reasons).
            await tunnel.Probe(timeout);

            return tunnel;
        }

        public void CloseTunnel(TunnelDestination endpoint)
        {
            lock (this.tunnelsLock)
            {
                if (!this.tunnels.TryGetValue(endpoint, out var tunnel))
                {
                    throw new KeyNotFoundException($"No active tunnel to {endpoint}");
                }

                tunnel.Result.Close();
                this.tunnels.Remove(endpoint);
            }
        }

        public void CloseTunnels()
        {
            lock (this.tunnelsLock)
            {
                var copyOfEndpoints = new List<TunnelDestination>(this.tunnels.Keys);

                var exceptions = new List<Exception>();
                foreach (var endpoint in copyOfEndpoints)
                {
                    try
                    {
                        CloseTunnel(endpoint);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }

                if (exceptions.Any())
                {
                    throw new AggregateException(exceptions);
                }
            }
        }
    }
}
