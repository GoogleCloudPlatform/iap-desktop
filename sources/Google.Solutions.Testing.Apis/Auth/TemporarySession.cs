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
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Common.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0067 // The event 'TemporarySession.Reauthorized' is never used

namespace Google.Solutions.Testing.Apis.Auth
{
    internal abstract class TemporarySession : IOidcSession
    {
        protected TemporarySession(
            string username,
            ICredential apiCredential)
        {
            this.Username = username.ExpectNotEmpty(nameof(username));
            this.ApiCredential = apiCredential.ExpectNotNull(nameof(apiCredential));
        }

        //---------------------------------------------------------------------
        // IOidcSession.
        //---------------------------------------------------------------------

        public ICredential ApiCredential { get; }

        public OidcOfflineCredential OfflineCredential
            => throw new NotImplementedException();

        public string Username { get; }

        public event EventHandler? Reauthorized;
        public event EventHandler? Terminated;

        public Task RevokeGrantAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Splice(IOidcSession newSession)
        {
            throw new NotImplementedException();
        }

        public void Terminate()
        {
        }

        public Uri CreateDomainSpecificServiceUri(Uri target)
        {
            return target;
        }
    }

    internal class TemporaryGaiaSession : TemporarySession, IGaiaOidcSession
    {
        public TemporaryGaiaSession(string username, ICredential apiCredential)
            : base(username, apiCredential)
        {
        }

        //---------------------------------------------------------------------
        // IGaiaOidcSession.
        //---------------------------------------------------------------------

        public IJsonWebToken IdToken => throw new NotImplementedException();

        public string? HostedDomain => null;

        public string Email => this.Username;
    }

    internal class TemporaryWorkforcePoolSession : TemporarySession, IWorkforcePoolSession
    {
        public TemporaryWorkforcePoolSession(
            TemporaryWorkforcePoolSubject subject,
            ICredential apiCredential)
            : base(subject.Username, apiCredential)
        {
            this.PrincipalIdentifier = subject.PrincipalId;
        }

        //---------------------------------------------------------------------
        // IGaiaOidcSession.
        //---------------------------------------------------------------------

        public string PrincipalIdentifier { get; }
    }
}
