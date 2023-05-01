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
using Google.Solutions.Common.Text;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.Ssh.Auth;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Session
{
    public interface ISshSessionParameters
    {

        /// <summary>
        /// Timeout to use for SSH connections.
        /// </summary>
        TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Terminal locale.
        /// </summary>
        CultureInfo Language { get; set; }

        /// <summary>
        /// Port to connect to (default: 22).
        /// </summary>
        ushort Port { get; set; }

        /// <summary>
        /// POSIX username to log in with, only applicable when
        /// using metadata-based keys.
        /// </summary>
        string PreferredUsername { get; set; }

        /// <summary>
        /// Validity to apply when authorizing the public key.
        /// </summary>
        TimeSpan PublicKeyValidity { get; set; }
    }

    internal sealed class SshSessionContext 
        : ISshSessionParameters, ISessionContext<SshCredential, ISshSessionParameters>
    {
        internal const ushort DefaultPort = 22;
        internal static readonly TimeSpan DefaultPublicKeyValidity = TimeSpan.FromDays(30);
        internal static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(30);

        private readonly ITunnelBrokerService tunnelBroker;
        private readonly IKeyAuthorizationService keyAuthorizationService;
        private readonly ISshKeyPair localKeyPair;

        internal SshSessionContext(
            ITunnelBrokerService tunnelBroker,
            IKeyAuthorizationService keyAuthService,
            InstanceLocator instance,
            ISshKeyPair localKeyPair)
        {
            this.tunnelBroker = tunnelBroker.ExpectNotNull(nameof(tunnelBroker));
            this.keyAuthorizationService = keyAuthService.ExpectNotNull(nameof(keyAuthService));

            this.localKeyPair = localKeyPair.ExpectNotNull(nameof(localKeyPair));
        }

        //---------------------------------------------------------------------
        // Parameters.
        //---------------------------------------------------------------------

        public CultureInfo Language { get; set; } = null;
        public TimeSpan ConnectionTimeout { get; set; } = DefaultConnectionTimeout;
        public ushort Port { get; set; } = DefaultPort;
        public string PreferredUsername { get; set; } = null;
        public TimeSpan PublicKeyValidity { get; set; } = DefaultPublicKeyValidity;

        //---------------------------------------------------------------------
        // ISessionContext.
        //---------------------------------------------------------------------

        public InstanceLocator Instance { get; }

        public ISshSessionParameters Parameters => this;

        public async Task<SshCredential> AuthorizeCredentialAsync(
            CancellationToken cancellationToken)
        {
            //
            // Authorize the key using OS Login or metadata-based keys.
            //
            var authorizedKey = await this.keyAuthorizationService
                .AuthorizeKeyAsync(
                    this.Instance,
                    this.localKeyPair,
                    this.PublicKeyValidity,
                    this.PreferredUsername.NullIfEmpty(),
                    KeyAuthorizationMethods.All,
                    cancellationToken)
                .ConfigureAwait(false);

            return new SshCredential(authorizedKey);
        }

        public async Task<ITransport> ConnectTransportAsync(
            CancellationToken cancellationToken)
        {
            return await Transport
                .CreateIapTransportAsync(
                    this.tunnelBroker,
                    this.Instance,
                    this.Port,
                    this.ConnectionTimeout)
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
            this.localKeyPair.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
