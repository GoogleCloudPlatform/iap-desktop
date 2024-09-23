//
// Copyright 2023 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Transport
{
    /// <summary>
    /// Factory for IAP transports.
    /// </summary>
    public interface IIapTransportFactory
    {
        /// <summary>
        /// Returns the pool of tunnels that the factory
        /// uses to satisfy requests.
        /// </summary>
        IEnumerable<IIapTunnel> Pool { get; }

        /// <summary>
        /// Create a transport to a VM instance/port.
        /// </summary>
        Task<ITransport> CreateTransportAsync(
            IProtocol protocol,
            ITransportPolicy policy,
            InstanceLocator targetInstance,
            ushort targetPort,
            IPEndPoint? localEndpoint,
            TimeSpan probeTimeout,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Tunnel events published to event queue.
    /// </summary>
    public static class TunnelEvents
    {
        public sealed class TunnelCreated { }
        public sealed class TunnelClosed { }
    }

    //-------------------------------------------------------------------------
    // Implementation.
    //-------------------------------------------------------------------------

    /// <summary>
    /// The factory maintains a pool of tunnels (implemented using
    /// SSH relay listeners). Whenever possible, the factory tries
    /// to reuse pooled tunnels.
    /// 
    /// Callers must dispose transports when they're done. Once there
    /// is no more transport that references a specific tunnel, the
    /// tunnel is closed too.
    /// 
    ///     Transport (used by a single client)
    ///       n |
    ///         |    1
    ///         +-----> Tunnel (shared, reference-counted)
    ///                    |     1
    ///                    +------> SshRelay
    ///                
    /// </summary>
    public class IapTransportFactory : IIapTransportFactory
    {
        private readonly object tunnelPoolLock;
        private readonly IDictionary<IapTunnel.Profile, Task<IapTunnel>> tunnelPool;

        private readonly IEventQueue eventQueue;
        private readonly IapTunnel.Factory tunnelFactory;

        private Task<IapTunnel> GetPooledTunnelAsync(
            IapTunnel.Profile profile,
            TimeSpan probeTimeout,
            CancellationToken cancellationToken)
        {
            profile.Policy.ExpectNotNull(nameof(profile));
            profile.Protocol.ExpectNotNull(nameof(profile));
            profile.TargetInstance.ExpectNotNull(nameof(profile));

            lock (this.tunnelPoolLock)
            {
                if (this.tunnelPool.TryGetValue(profile, out var tunnelTask) &&
                    !tunnelTask.IsFaulted)
                {
                    //
                    // Found matching tunnel, and it looks okay.
                    //
                    CoreTraceSource.Log.TraceInformation(
                        "Using pooled tunnel for {0}", profile);

                    tunnelTask = tunnelTask.ContinueWith(t =>
                    {
                        t.Result.AddReference();
                        return t.Result;
                    });
                }
                else
                {
                    CoreTraceSource.Log.TraceInformation(
                        "Creating new tunnel for {0}", profile);

                    tunnelTask = this.tunnelFactory.CreateTunnelAsync(
                        profile,
                        probeTimeout,
                        cancellationToken);

                    //
                    // NB. We don't await tunnel creation and instead, immediately
                    // add it to the pool. That way, two threads that request the same
                    // tunnel can use the same Task<>.
                    //
                    this.tunnelPool[profile] = tunnelTask;

                    //
                    // Track lifecycle.
                    //
                    _ = tunnelTask.ContinueWith(t =>
                    {
                        //
                        // Acquire the lock. That way, we ensure that the
                        // task has been added to the pool.
                        //
                        lock (this.tunnelPoolLock)
                        {
                            Debug.Assert(this.tunnelPool.ContainsKey(profile));
                            Debug.Assert(this.Pool.Any());
                        }

                        OnTunnelCreated(t.Result);
                        t.Result.Closed += OnClosed;
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);

                    void OnClosed(object sender, EventArgs __)
                    {
                        var tunnel = (IapTunnel)sender;

                        //
                        // Remove from pool.
                        //
                        lock (this.tunnelPoolLock)
                        {
                            var removed = this.tunnelPool.Remove(tunnel.Details);
                            Debug.Assert(removed);
                        }

                        OnTunnelClosed(tunnel);
                        tunnel.Closed -= OnClosed;
                    }
                }

                Debug.Assert(tunnelTask != null);
                return tunnelTask!;
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal IapTransportFactory(
            IEventQueue eventQueue,
            IapTunnel.Factory tunnelFactory)
        {
            this.eventQueue = eventQueue.ExpectNotNull(nameof(eventQueue));
            this.tunnelFactory = tunnelFactory.ExpectNotNull(nameof(tunnelFactory));

            this.tunnelPoolLock = new object();
            this.tunnelPool = new Dictionary<IapTunnel.Profile, Task<IapTunnel>>();
        }

        public IapTransportFactory(
            IIapClient iapClient,
            IEventQueue eventQueue)
            : this(
                  eventQueue,
                  new IapTunnel.Factory(iapClient))
        { }

        protected virtual void OnTunnelCreated(IapTunnel tunnel)
        {
            CoreTraceSource.Log.TraceVerbose(
                "Created tunnel for {0}", tunnel.Details);

            this.eventQueue.Publish(new TunnelEvents.TunnelCreated());
        }

        protected virtual void OnTunnelClosed(IapTunnel tunnel)
        {
            CoreTraceSource.Log.TraceVerbose(
                "Closed tunnel for {0}", tunnel.Details);

            this.eventQueue.Publish(new TunnelEvents.TunnelClosed());
        }

        //---------------------------------------------------------------------
        // IIapTransportFactory.
        //---------------------------------------------------------------------

        [SuppressMessage("Usage",
            "VSTHRD002:Avoid problematic synchronous waits",
            Justification = "Explicit check")]
        public IEnumerable<IIapTunnel> Pool
        {
            get
            {
                lock (this.tunnelPoolLock)
                {
                    var tunnels = this.tunnelPool
                        .Values
                        .Where(t => t.IsCompleted && !t.IsFaulted)
                        .Select(t => t.Result);

                    //
                    // Return a snapshot (so that we can leave the lock).
                    //
                    return new List<IIapTunnel>(tunnels);
                }
            }
        }

        public async Task<ITransport> CreateTransportAsync(
            IProtocol protocol,
            ITransportPolicy policy,
            InstanceLocator targetInstance,
            ushort targetPort,
            IPEndPoint? localEndpoint,
            TimeSpan probeTimeout,
            CancellationToken cancellationToken)
        {
            using (CoreTraceSource.Log.TraceMethod().WithParameters(targetInstance, protocol))
            {
                var profile = new IapTunnel.Profile(
                    protocol,
                    policy,
                    targetInstance,
                    targetPort,
                    localEndpoint);

                try
                {
                    var tunnel = await GetPooledTunnelAsync(
                            profile,
                            probeTimeout,
                            cancellationToken)
                        .ConfigureAwait(false);

                    return new Transport(
                        tunnel,
                        protocol,
                        targetInstance);
                }
                catch (SshRelayDeniedException e)
                {
                    throw new TransportFailedException(
                        "You do not have sufficient access to connect to this VM instance.\n\n" +
                        "To connect to this VM, you need the 'IAP-secured Tunnel User' " +
                        "role (or an equivalent custom role).\n\n" +
                        "Note that it might take several minutes for IAM policy changes to take effect.",
                        HelpTopics.IapAccess,
                        e);
                }
                catch (NetworkStreamClosedException e)
                {
                    throw new TransportFailedException(
                        "Connecting to the instance failed. Make sure that you have " +
                        "configured your firewall rules to permit IAP-TCP access " +
                        $"to port {targetPort} of {targetInstance.Name}",
                        HelpTopics.CreateIapFirewallRule,
                        e);
                }
                catch (WebSocketConnectionDeniedException)
                {
                    throw new TransportFailedException(
                        "Establishing an IAP-TCP tunnel failed because the server " +
                        "denied access.\n\n" +
                        "If you are using a proxy server, make sure that the proxy " +
                        "server allows WebSocket connections.",
                        HelpTopics.ProxyConfiguration);
                }
            }
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        /// <summary>
        /// Transports are single-use, but multiple Transports might
        /// use a shared Tunnel.
        /// </summary>
        internal class Transport : DisposableBase, ITransport
        {
            internal IapTunnel Tunnel { get; }

            internal Transport(
                IapTunnel relay,
                IProtocol protocol,
                InstanceLocator target)
            {
                this.Tunnel = relay.ExpectNotNull(nameof(relay));
                this.Protocol = protocol.ExpectNotNull(nameof(protocol));
                this.Target = target.ExpectNotNull(nameof(target));
            }

            //-----------------------------------------------------------------
            // ITransport.
            //-----------------------------------------------------------------

            public IProtocol Protocol { get; }

            public IPEndPoint Endpoint => this.Tunnel.LocalEndpoint;

            public ComputeEngineLocator Target { get; }

            //-----------------------------------------------------------------
            // DisposableBase.
            //-----------------------------------------------------------------

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.Tunnel.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
