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
using Google.Solutions.Common;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
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
        /// Registration used by this client.
        /// </summary>
        OidcClientRegistration Registration { get; }

        /// <summary>
        /// Try to authorize using an existing refresh token.
        /// </summary>
        /// <returns>Null if silent authorization failed</returns>
        Task<IOidcSession?> TryAuthorizeSilentlyAsync(
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

        protected OidcClientBase(
            IOidcOfflineCredentialStore store,
            OidcClientRegistration registration)
        {
            this.store = store.ExpectNotNull(nameof(store));
            this.Registration = registration.ExpectNotNull(nameof(registration));
        }

        internal void ClearOfflineCredentialStore()
        {
            this.store.Clear();
        }

        //---------------------------------------------------------------------
        // IOidcClient.
        //---------------------------------------------------------------------

        public OidcClientRegistration Registration { get; }

        public abstract IServiceEndpoint Endpoint { get; }

        public async Task<IOidcSession?> TryAuthorizeSilentlyAsync(
            CancellationToken cancellationToken)
        {
            if (this.store.TryRead(out var offlineCredential))
            {
                ApiTraceSource.Log.TraceVerbose(
                    "Attempting authorization using offline credential...");

                Debug.Assert(offlineCredential!.RefreshToken != null);

                if (offlineCredential.Issuer != this.Registration.Issuer)
                {
                    ApiTraceSource.Log.TraceWarning(
                        "Found offline credential from wrong issuer: {0}",
                        offlineCredential.Issuer);
                    return null;
                }

                try
                {
                    var session = await
                        ActivateOfflineCredentialAsync(offlineCredential, cancellationToken)
                        .ConfigureAwait(false);

                    Debug.Assert(session != null);
                    Debug.Assert(session!.OfflineCredential != null);
                    Debug.Assert(session.OfflineCredential!.Issuer == this.Registration.Issuer);

                    //
                    // Update the offline credential as the refresh
                    // token and/or ID token might have changed.
                    //
                    this.store.Write(session.OfflineCredential);

                    TelemetryLog.Current.Write(
                        "app_auth_offline",
                        new Dictionary<string, object>
                        {
                            { "issuer", this.Registration.Issuer.ToString()}
                        });

                    ApiTraceSource.Log.TraceVerbose(
                        "Activating offline credential succeeded.");

                    return session;
                }
                catch (Exception e)
                {
                    Debug.Assert(!(e is ArgumentException));
                    Debug.Assert(!(e is NullReferenceException));

                    //
                    // The offline credentials didn't work, but they might still
                    // be useful to streamline a browser-based sign-in. Therefore,
                    // we don't clear the store.
                    //

                    TelemetryLog.Current.Write(
                        "app_auth_offline_failed",
                        new Dictionary<string, object>
                        {
                            { "issuer", this.Registration.Issuer.ToString()},
                            { "error", e.FullMessage() }
                        });

                    ApiTraceSource.Log.TraceWarning(
                        "Activating offline credential failed: {0}",
                        e.FullMessage());

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

            if (offlineCredential != null &&
                offlineCredential.Issuer != this.Registration.Issuer)
            {
                //
                // User switched issuers, we can't use this credential
                // anymore.
                //
                offlineCredential = null;
            }

            try
            {
                var session = await
                    AuthorizeWithBrowserAsync(
                        offlineCredential,
                        codeReceiver,
                        cancellationToken)
                    .ConfigureAwait(false);

                Debug.Assert(session != null);
                Debug.Assert(session!.OfflineCredential.Issuer == this.Registration.Issuer);

                //
                // Store the refresh token so that we can do a silent
                // activation next time.
                //
                this.store.Write(session.OfflineCredential);

                TelemetryLog.Current.Write(
                    "app_auth",
                    new Dictionary<string, object>
                    {
                        { "issuer", this.Registration.Issuer.ToString()}
                    });

                ApiTraceSource.Log.TraceVerbose(
                    "Browser-based authorization succeeded.");

                return session;
            }
            catch (PlatformNotSupportedException)
            {
                //
                // Convert this into an exception with more actionable information.
                //
                throw new AuthorizationFailedException(
                    "Authorization failed because the HTTP Server API is not enabled " +
                    "on your computer. This API is required to complete the OAuth authorization flow.\n\n" +
                    "To enable the API, open an elevated command prompt and run " +
                    "'sc config http start= auto'.");
            }
        }

        protected abstract Task<IOidcSession> AuthorizeWithBrowserAsync(
            OidcOfflineCredential? offlineCredential,
            ICodeReceiver codeReceiver,
            CancellationToken cancellationToken);

        protected abstract Task<IOidcSession> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken);
    }


    public class AuthorizationFailedException : Exception
    {
        public AuthorizationFailedException(string message) : base(message)
        {
        }
    }

    public class OAuthScopeNotGrantedException : AuthorizationFailedException
    {
        public OAuthScopeNotGrantedException(string message) : base(message)
        {
        }
    }
}
