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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Properties;
using Google.Solutions.Platform.Net;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Profile.Auth
{
    internal class Authorization : IAuthorization
    {
        private readonly IOidcClient client;
        private IOidcSession session = null;

        private void SetOrSpliceSession(IOidcSession newSession)
        {
            newSession.ExpectNotNull(nameof(newSession));   

            if (this.session != null)
            {
                //
                // Once we have a session, we must never replace it,
                // all we can do it is splice it to extend its lifetime.
                //
                this.session.Splice(newSession);

                this.Reauthorized?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                this.session = newSession;
            }
        }

        public Authorization(
            IOidcClient client,
            IDeviceEnrollment deviceEnrollment)
        {
            this.client = client.ExpectNotNull(nameof(client));
            this.DeviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
        }

        //---------------------------------------------------------------------
        // IAuthorization.
        //---------------------------------------------------------------------

        public event EventHandler Reauthorized;

        public IOidcSession Session
        {
            get => this.session ?? throw new InvalidOperationException("Not authorized yet");
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        /// <summary>
        /// Raised before reauthorizations. Subscribers can use this event
        /// to show a UI and/or modify the browser preference.
        /// </summary>
        public event EventHandler<ReauthorizeEventArgs> BeforeReauthorize;

        /// <summary>
        /// Try to authorize using an existing refresh token.
        /// </summary>
        public async Task<bool> TryAuthorizeSilentlyAsync(
            CancellationToken cancellationToken)
        {
            Debug.Assert(
                this.session == null,
                "Silent authorize should only be performed initially");

            var newSession = await this.client
                .TryAuthorizeSilentlyAsync(cancellationToken)
                .ConfigureAwait(false);

            if (newSession != null)
            {
                SetOrSpliceSession(newSession);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Authorize or re-authorize using a browser-based OIDC flow.
        /// </summary>
        public async Task AuthorizeAsync(
            BrowserPreference browserPreference,
            CancellationToken cancellationToken)
        {
            var newSession = await this.client
                .AuthorizeAsync(
                    new BrowserCodeReceiver(browserPreference),
                    cancellationToken)
                .ConfigureAwait(false);
            SetOrSpliceSession(newSession);
        }

        public Task ReauthorizeAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(this.session != null);

            //
            // Give the UI a chance to react and decide which browser to use.
            //
            var args = new ReauthorizeEventArgs();
            this.BeforeReauthorize?.Invoke(this, args);

            return AuthorizeAsync(args.BrowserPreference, cancellationToken);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public class ReauthorizeEventArgs : EventArgs
        {
            public BrowserPreference BrowserPreference { get; set; } 
                = BrowserPreference.Default;
        }

        private class BrowserCodeReceiver : LocalServerCodeReceiver 
        {
            private readonly BrowserPreference browserPreference;

            public BrowserCodeReceiver(BrowserPreference browserPreference)
                : base(Resources.AuthorizationSuccessful)
            {
                this.browserPreference = browserPreference;
            }

            protected override bool OpenBrowser(string url)
            {
                Browser
                    .Get(this.browserPreference)
                    .Navigate(url);
                return true;
            }
        }

        //---------------------------------------------------------------------
        // TODO: Obsolete methods below?

        public ICredential Credential => this.Session.ApiCredential;

        public string Email => this.Session.Username;

        public IDeviceEnrollment DeviceEnrollment { get; }

        public Task RevokeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
