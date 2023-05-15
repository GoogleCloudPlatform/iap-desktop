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

using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.Auth;
using Google.Solutions.IapDesktop.Core.Net.Protocol;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Net.Transport
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
            ISshRelayPolicy policy,
            InstanceLocator targetInstance,
            ushort targetPort,
            IPEndPoint localEndpoint,
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

        private readonly IAuthorization authorization;
        private readonly IEventQueue eventQueue;
        private readonly UserAgent userAgent;
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
                    CoreTraceSources.Default.TraceInformation(
                        "Using pooled tunnel for {0}", profile);

                    tunnelTask = tunnelTask.ContinueWith(t =>
                    {
                        t.Result.AddReference();
                        return t.Result;
                    });
                }
                else
                {
                    //
                    // Initiate the creation of a new tunnel and hand out the task.
                    // If multiple callers request the same tunnel, they can await
                    // the same task.
                    //
                    CoreTraceSources.Default.TraceInformation(
                        "Creating new tunnel for {0}", profile);

                    tunnelTask = this.tunnelFactory.CreateTunnelAsync(
                        this.authorization,
                        this.userAgent,
                        profile,
                        probeTimeout,
                        cancellationToken);

                    //
                    // Track lifecycle.
                    //
                    tunnelTask = tunnelTask.ContinueWith(t =>
                    { 
                        OnTunnelCreated(t.Result);
                        t.Result.Closed += OnClosed;
                        return t.Result;
                    });

                    void OnClosed(object sender, EventArgs __)
                    {
                        var tunnel = (IapTunnel)sender;
                        OnTunnelClosed(tunnel);
                        tunnel.Closed -= OnClosed;
                    }

                    this.tunnelPool[profile] = tunnelTask;
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
            IEventQueue eventQueue,
            UserAgent userAgent,
            IapTunnel.Factory tunnelFactory)
        {
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.eventQueue = eventQueue.ExpectNotNull(nameof(eventQueue));
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));
            this.tunnelFactory = tunnelFactory.ExpectNotNull(nameof(tunnelFactory));

            this.tunnelPoolLock = new object();
            this.tunnelPool = new Dictionary<IapTunnel.Profile, Task<IapTunnel>>();
        }

        public IapTransportFactory(
            IAuthorization authorization,
            IEventQueue eventQueue,
            UserAgent userAgent)
            : this(
                  authorization,
                  eventQueue,
                  userAgent,
                  new IapTunnel.Factory())
        { }

        protected virtual void OnTunnelCreated(IapTunnel tunnel)
        {
            CoreTraceSources.Default.TraceVerbose(
                "Created tunnel for {0}", tunnel.Details);

            this.eventQueue.Publish(new TunnelEvents.TunnelCreated());
        }

        protected virtual void OnTunnelClosed(IapTunnel tunnel)
        {
            CoreTraceSources.Default.TraceVerbose(
                "Closed tunnel for {0}", tunnel.Details);

            this.eventQueue.Publish(new TunnelEvents.TunnelClosed());
        }

        //---------------------------------------------------------------------
        // IIapTransportFactory.
        //---------------------------------------------------------------------

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
            ISshRelayPolicy policy,
            InstanceLocator targetInstance,
            ushort targetPort,
            IPEndPoint localEndpoint,
            TimeSpan probeTimeout,
            CancellationToken cancellationToken)
        {
            using (CoreTraceSources.Default.TraceMethod().WithParameters(targetInstance, protocol))
            {
                if (localEndpoint == null)
                {
                    localEndpoint = new IPEndPoint(
                        IPAddress.Loopback,
                        PortFinder.FindFreeLocalPort());
                }

                var profile = new IapTunnel.Profile(
                    protocol,
                    policy,
                    targetInstance,
                    targetPort,
                    localEndpoint);

                var tunnel = await GetPooledTunnelAsync(
                        profile,
                        probeTimeout,
                        cancellationToken)
                    .ConfigureAwait(false);

                return new Transport(
                    tunnel,
                    protocol);
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
                IProtocol protocol)
            {
                this.Tunnel = relay.ExpectNotNull(nameof(relay));
                this.Protocol = protocol.ExpectNotNull(nameof(protocol));
            }

            //-----------------------------------------------------------------
            // ITransport.
            //-----------------------------------------------------------------

            public IProtocol Protocol { get; }

            public IPEndPoint Endpoint => this.Tunnel.LocalEndpoint;

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
