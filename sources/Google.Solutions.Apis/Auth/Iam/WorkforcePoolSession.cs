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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0067 // The event 'WorkforcePoolSession.Terminated' is never used

namespace Google.Solutions.Apis.Auth.Iam
{
    /// <summary>
    /// A workforce identity "3PI" session.
    /// </summary>
    internal class WorkforcePoolSession : OidcSessionBase
    {
        private readonly WorkforcePoolIdentity identity;

        public WorkforcePoolSession(
            UserCredential apiCredential, 
            WorkforcePoolIdentity identity)
            : base(apiCredential)
        {
            this.identity = identity.ExpectNotNull(nameof(identity));
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string Username => this.identity.Subject;

        public override OidcOfflineCredential OfflineCredential
        {
            get
            {
                return new OidcOfflineCredential(
                    OidcIssuer.Sts,
                    this.Credential.Token.Scope,
                    this.Credential.Token.RefreshToken,
                    null);
            }
        }

        public override void Splice(IOidcSession newSession)
        {
            //
            // Workforce pool refresh tokens expire when the session
            // expires, and the basic behavior is similar to what happens
            // during a Gaia session expiry.
            //
            base.Splice(newSession);

            Debug.Assert(newSession.Username == this.Username);
        }

        public override Task RevokeGrantAsync(CancellationToken cancellationToken)
        {
            //
            // STS grants can't be revoked.
            //
            throw new NotSupportedForWorkloadIdentityException();
        }
    }
}
