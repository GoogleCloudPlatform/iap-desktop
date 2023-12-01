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
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh
{
    /// <summary>
    /// Encapsulates settings and logic to create an SSH session.
    /// </summary>
    internal sealed class SshContext
        : SessionContextBase<PlatformCredential, SshParameters>
    {
        private readonly IPlatformCredentialFactory credentialFactory;
        private readonly IAsymmetricKeySigner signer;

        internal SshContext(
            IIapTransportFactory iapTransportFactory,
            IDirectTransportFactory directTransportFactory,
            IPlatformCredentialFactory credentialFactory,
            InstanceLocator instance,
            IAsymmetricKeySigner signer)
            : base(
                  iapTransportFactory,
                  directTransportFactory,
                  instance,
                  new SshParameters())
        {
            this.credentialFactory = credentialFactory.ExpectNotNull(nameof(credentialFactory));
            this.signer = signer.ExpectNotNull(nameof(signer));
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override async Task<PlatformCredential> AuthorizeCredentialAsync(
            CancellationToken cancellationToken)
        {
            //
            // Authorize the key using OS Login or metadata-based keys.
            //
            return await this.credentialFactory
                .CreateCredentialAsync(
                    this.Instance,
                    this.signer,
                    this.Parameters.PublicKeyValidity,
                    this.Parameters.PreferredUsername.NullIfEmpty(),
                    KeyAuthorizationMethods.All,
                    cancellationToken)
                .ConfigureAwait(false);
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
            this.signer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
