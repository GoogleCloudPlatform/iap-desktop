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
using Google.Solutions.Common.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// Base class for OIDC sessions.
    /// </summary>
    public abstract class OidcSessionBase : IOidcSession
    {
        protected UserCredential Credential { get; }

        protected OidcSessionBase(UserCredential credential)
        {
            this.Credential = credential.ExpectNotNull(nameof(credential));
        }

        //---------------------------------------------------------------------
        // IOidcSession.
        //---------------------------------------------------------------------

        public event EventHandler Terminated;

        public ICredential ApiCredential => this.Credential;

        public abstract string Username { get; }

        public abstract OidcOfflineCredential OfflineCredential { get; }

        public abstract Task RevokeGrantAsync(CancellationToken cancellationToken);

        public virtual void Splice(IOidcSession newSession)
        {
            //
            // Replace the current tokens (which we can assume to be invalid
            // by now) with the new session's tokens.
            //
            // NB. By retaining the UserCredential object, we ensure that
            // any existing API client (which has the UserCredential installed
            // as interceptor) continues to work, and immediately starts
            // using the new tokens.
            //
            // A more obviuos approach to achiveve the same would be to
            // implement an ICredential facade, and swap out its backing
            // credential with the new session's credential. But UserCredential
            // hands out `this` pointers in multiple places, and that makes the
            // facade approach brittle in practice.
            //
            if (newSession is OidcSessionBase session && session != null)
            {
                this.Credential.Token = session.Credential.Token;

                //
                // NB. Leave IdToken and other properties as is as their
                // values shouldn't have changed.
                //
            }
            else
            {
                throw new ArgumentException(nameof(newSession));
            }
        }

        public virtual void Terminate()
        {
            this.Terminated?.Invoke(this, EventArgs.Empty);

            //
            // NB. The client handles the actual termination.
            //
        }

        public abstract Uri CreateDomainSpecificServiceUri(Uri target);
    }
}
