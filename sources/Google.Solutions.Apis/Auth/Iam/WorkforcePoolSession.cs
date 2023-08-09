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

#pragma warning disable CS0067 // The event 'WorkforcePoolSession.Terminated' is never used

namespace Google.Solutions.Apis.Auth.Iam
{
    /// <summary>
    /// A workforce identity "3PI" session.
    /// </summary>
    internal class WorkforcePoolSession : IOidcSession
    {
        private readonly UserCredential apiCredential;
        private readonly WorkforcePoolIdentity identity;

        public WorkforcePoolSession(
            UserCredential apiCredential, 
            WorkforcePoolIdentity identity)
        {
            this.apiCredential = apiCredential.ExpectNotNull(nameof(apiCredential));
            this.identity = identity.ExpectNotNull(nameof(identity));
        }


        //---------------------------------------------------------------------
        // IOidcSession.
        //---------------------------------------------------------------------

        public event EventHandler Terminated;

        public string Username => this.identity.Subject;

        public ICredential ApiCredential => this.apiCredential;

        public OidcOfflineCredential OfflineCredential
        {
            get
            {
                return new OidcOfflineCredential(
                    OidcIssuer.Sts,
                    this.apiCredential.Token.Scope,
                    this.apiCredential.Token.RefreshToken,
                    null);
            }
        }

        public void Splice(IOidcSession newSession)
        {
            throw new NotSupportedForWorkloadIdentityException();
        }

        public Task RevokeGrantAsync(CancellationToken cancellationToken)
        {
            //
            // STS grants can't be revoked.
            //
            throw new NotSupportedForWorkloadIdentityException();
        }

        public void Terminate()
        {
            //
            // STS tokens can't be revoked.
            //
            throw new NotSupportedForWorkloadIdentityException();
        }
    }
}
