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
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0067 // The event 'ResourceTaskExtensions.Authorization.Reauthorized' is never used
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace Google.Solutions.Testing.Apis.Integration
{
    public static class ResourceTaskExtensions
    {
        public static IAuthorization ToAuthorization(
            this ICredential credential)
        {
            return new Authorization(credential);
        }

        public static async Task<IAuthorization> ToAuthorization(
            this ResourceTask<ICredential> credential)
        {
            return new Authorization(await credential);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class Authorization : IAuthorization
        {
            public Authorization(ICredential credential)
            {
                this.Credential = credential;
                this.Email = "test@example.com";
                this.UserInfo = new UserInfo()
                {
                    Email = this.Email,
                    Name = "Test"
                };
                this.DeviceEnrollment = new DeviceEnrollment();
            }

            public ICredential Credential { get; }

            public string Email { get; }

            public UserInfo UserInfo { get; }

            public IDeviceEnrollment DeviceEnrollment { get; }

            public event EventHandler Reauthorized;

            public Task ReauthorizeAsync(CancellationToken token)
            {
                throw new NotImplementedException();
            }

            public Task RevokeAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class DeviceEnrollment : IDeviceEnrollment
        {
            public DeviceEnrollmentState State => DeviceEnrollmentState.Disabled;

            public X509Certificate2 Certificate => null;
        }
    }
}
