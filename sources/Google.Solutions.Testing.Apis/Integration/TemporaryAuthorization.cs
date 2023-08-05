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

using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using System.Security.Cryptography.X509Certificates;

#pragma warning disable CS0067 // The event is never used

namespace Google.Solutions.Testing.Apis.Integration
{
    public class TemporaryAuthorization : IAuthorization, IOidcSession
    {
        private TemporaryAuthorization(
            string email,
            ICredential credential,
            IDeviceEnrollment deviceEnrollment)
        {
            this.ApiCredential = credential.ExpectNotNull(nameof(credential));
            this.DeviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            this.Username = email;
        }

        internal TemporaryAuthorization(
            string email,
            ICredential credential,
            X509Certificate2 certificate)
            : this(email, credential, new Enrollment(certificate))
        {
        }

        public TemporaryAuthorization(
            string email,
            ICredential credential)
            : this(email, credential, new Enrollment())
        {
        }

        internal TemporaryAuthorization(
            string email,
            string accessToken)
            : this(email, new Credential(accessToken), new Enrollment())
        {
        }

        public static TemporaryAuthorization ForSecureConnectUser()
        {
            return new TemporaryAuthorization(
                "secure-connect@gserviceaccount.com",
                TestProject.GetSecureConnectCredential(),
                TestProject.GetDeviceCertificate());
        }

        public static TemporaryAuthorization ForInvalidCredential()
        {
            return new TemporaryAuthorization(
                "invalid@gserviceaccount.com",
                GoogleCredential.FromAccessToken("invalid"));
        }

        public static TemporaryAuthorization ForAdmin()
        {
            return new TemporaryAuthorization(
                "admin@gserviceaccount.com",
                TestProject.GetAdminCredential());
        }

        //---------------------------------------------------------------------
        // IAuthorization.
        //---------------------------------------------------------------------

        public IOidcSession Session => this;

        public IDeviceEnrollment DeviceEnrollment { get; }

        public string Email => this.Username;

        public string Username { get; }

        public Task ReauthorizeAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        //---------------------------------------------------------------------
        // IOidcSession.
        //---------------------------------------------------------------------

        public ICredential ApiCredential { get; }

        public OidcOfflineCredential OfflineCredential => throw new NotImplementedException();

        public event EventHandler Reauthorized;
        public event EventHandler Terminated;

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

        private class Enrollment : IDeviceEnrollment
        {
            public Enrollment()
            {
                this.State = DeviceEnrollmentState.NotEnrolled;
            }

            public Enrollment(X509Certificate2 certificate)
            {
                this.Certificate = certificate;
                this.State = DeviceEnrollmentState.Enrolled;
            }

            public DeviceEnrollmentState State { get; }

            public X509Certificate2 Certificate { get; }
        }

        private class Credential : ServiceCredential
        {
            public string AccessToken { get; }

            internal Credential(
                string accessToken)
                : base(new ServiceCredential.Initializer(GoogleAuthConsts.TokenUrl))
            {
                this.AccessToken = accessToken;
            }

            public override Task<bool> RequestAccessTokenAsync(
                CancellationToken taskCancellationToken)
            {
                this.Token = new TokenResponse()
                {
                    AccessToken = this.AccessToken,
                    ExpiresInSeconds = 3600
                };

                return Task.FromResult(true);
            }
        }
    }
}
