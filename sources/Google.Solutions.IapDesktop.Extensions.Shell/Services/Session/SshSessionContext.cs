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

using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Text;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Session
{
    /// <summary>
    /// Encapsulates settings and logic to create an SSH session.
    /// </summary>
    internal sealed class SshSessionContext
        : SessionContextBase<SshCredential, SshSessionParameters>
    {
        private readonly IKeyAuthorizationService keyAuthorizationService;
        private readonly ISshKeyPair localKeyPair;

        internal SshSessionContext(
            IIapTransportFactory iapTransportFactory,
            IDirectTransportFactory directTransportFactory,
            IKeyAuthorizationService keyAuthService,
            IAddressResolver addressResolver,
            InstanceLocator instance,
            ISshKeyPair localKeyPair)
            : base(
                  iapTransportFactory,
                  directTransportFactory,
                  addressResolver,
                  instance,
                  new SshSessionParameters())
        {
            this.keyAuthorizationService = keyAuthService.ExpectNotNull(nameof(keyAuthService));
            this.localKeyPair = localKeyPair.ExpectNotNull(nameof(localKeyPair));
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override async Task<SshCredential> AuthorizeCredentialAsync(
            CancellationToken cancellationToken)
        {
            //
            // Authorize the key using OS Login or metadata-based keys.
            //
            var authorizedKey = await this.keyAuthorizationService
                .AuthorizeKeyAsync(
                    this.Instance,
                    this.localKeyPair,
                    this.Parameters.PublicKeyValidity,
                    this.Parameters.PreferredUsername.NullIfEmpty(),
                    KeyAuthorizationMethods.All,
                    cancellationToken)
                .ConfigureAwait(false);

            return new SshCredential(authorizedKey);
        }

        public override Task<ITransport> ConnectTransportAsync(
            CancellationToken cancellationToken)
        {
            return ConnectTransportAsync(
                SshProtocol.Protocol,
                this.Parameters.TransportType,
                this.Parameters.Port,
                this.Parameters.ConnectionTimeout,
                cancellationToken);
        }

        public override void Dispose()
        {
            this.localKeyPair.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
