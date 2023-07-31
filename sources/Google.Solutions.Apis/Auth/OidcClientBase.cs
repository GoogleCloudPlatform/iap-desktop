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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// Client for performing a OIDC-based authorization.
    /// </summary>
    public interface IOidcClient : IClient
    {
        /// <summary>
        /// Try to authorize using an existing refresh token.
        /// </summary>
        /// <returns>Null if silent authorization failed</returns>
        Task<IOidcSession> TryAuthorizeSilentlyAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Authorize using a browser-based OIDC flow.
        /// </summary>
        Task<IOidcSession> AuthorizeAsync(
            ICodeReceiver codeReceiver,
            CancellationToken cancellationToken);
    }

    public abstract class OidcClientBase : IOidcClient
    {
        private readonly IOidcOfflineCredentialStore store;

        protected OidcClientBase(IOidcOfflineCredentialStore store)
        {
            this.store = store.ExpectNotNull(nameof(store));
        }

        internal void ClearOfflineCredentialStore()
        {
            this.store.Clear();
        }

        //---------------------------------------------------------------------
        // IOidcClient.
        //---------------------------------------------------------------------

        public abstract IServiceEndpoint Endpoint { get; }

        public async Task<IOidcSession> TryAuthorizeSilentlyAsync(
            CancellationToken cancellationToken)
        {
            if (this.store.TryRead(out var offlineCredential))
            {
                ApiTraceSources.Default.TraceVerbose(
                    "Attempting authorization using offline credential...");

                Debug.Assert(offlineCredential.RefreshToken != null);
                try
                {
                    var session = await
                        ActivateOfflineCredentialAsync(offlineCredential, cancellationToken)
                        .ConfigureAwait(false);
                    Debug.Assert(session != null);

                    //
                    // Update the offline credential as the refresh
                    // token and/or ID token might have changed.
                    //
                    this.store.Write(session.OfflineCredential);

                    return session;
                }
                catch (Exception e)
                {
                    //
                    // The offline credentials didn't work.
                    //

                    ApiTraceSources.Default.TraceWarning(
                        "Activating offline credential failed: {0}", 
                        e.FullMessage());

                    this.store.Clear();
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public async Task<IOidcSession> AuthorizeAsync(
            ICodeReceiver codeReceiver,
            CancellationToken cancellationToken)
        {
            codeReceiver.ExpectNotNull(nameof(codeReceiver));

            this.store.TryRead(out var offlineCredential);

            var authorization = await 
                AuthorizeWithBrowserAsync(
                    offlineCredential, 
                    codeReceiver, 
                    cancellationToken)
                .ConfigureAwait(false);

            //
            // Store the refresh token so that we can do a silent
            // activation next time.
            //
            this.store.Write(authorization.OfflineCredential);
            return authorization;
        }

        protected abstract Task<IOidcSession> AuthorizeWithBrowserAsync(
            OidcOfflineCredential offlineCredential,
            ICodeReceiver codeReceiver,
            CancellationToken cancellationToken);

        protected abstract Task<IOidcSession> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken);
    }
}
