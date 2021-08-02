//
// Copyright 2019 Google LLC
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
using Google.Apis.CloudResourceManager.v1;
using Google.Apis.Compute.v1;
using Google.Apis.Iam.v1;
using Google.Apis.IAMCredentials.v1;
using Google.Apis.Services;
using Google.Solutions.Common.Net;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Common.Test.Integration
{
    public static class TestProject
    {
        internal const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

        public static readonly string InvalidProjectId = "invalid-0000";
        public static readonly string ProjectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
        public static readonly string Zone = "us-central1-a";

        public static UserAgent UserAgent { get; }

        static TestProject()
        {
            UserAgent = new UserAgent(
                "IAP-Desktop-TestSuite",
                Assembly.GetExecutingAssembly().GetName().Version);
        }

        public static GoogleCredential GetAdminCredential()
        {
            // This account must have the following roles:
            // - Compute Admin
            // - Service Account Admin (to create service accounts)
            // - Service Account User (to access Compute Engine Service account)
            // - IAP-secured Tunnel User
            // - Logs Viewer
            // - Private Logs Viewer (for data access log)
            // - Project IAM Admin
            // - Service Account Token Creator

            var credential = GoogleCredential.GetApplicationDefault();
            return credential.IsCreateScopedRequired
                ? credential.CreateScoped(CloudPlatformScope)
                : credential;
        }

        public static GoogleCredential GetSecureConnectCredential()
        {
            // This account must have:
            // - Cloud Identity Premium
            // - an associated device certiticate on the local machine

            var credentialsPath = Environment.GetEnvironmentVariable("SECURECONNECT_CREDENTIALS");
            if (string.IsNullOrEmpty(credentialsPath))
            {
                throw new ApplicationException(
                    "SECURECONNECT_CREDENTIALS not set, needs to point to credentials " +
                    "JSON of a SecureConnect-enabled user");
            }

            var credential = GoogleCredential.FromFile(credentialsPath);
            return credential.IsCreateScopedRequired
                ? credential.CreateScoped(CloudPlatformScope)
                : credential;
        }

        public static X509Certificate2 GetDeviceCertificate()
        {
            var credentialsPath = Environment.GetEnvironmentVariable("SECURECONNECT_CERTIFICATE");
            if (string.IsNullOrEmpty(credentialsPath))
            {
                throw new ApplicationException(
                    "SECURECONNECT_CERTIFICATE not set, needs to point to a PFX " +
                    "containing a SecureConnect device certificate");
            }

            var collection = new X509Certificate2Collection();
            collection.Import(
                credentialsPath,
                string.Empty, // No passphrase
                X509KeyStorageFlags.DefaultKeySet);

            return collection
                .OfType<X509Certificate2>()
                .First();
        }

        public static ComputeService CreateComputeService()
        {
            return new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = TestProject.GetAdminCredential()
            });
        }

        public static IamService CreateIamService()
        {
            return new IamService(new BaseClientService.Initializer
            {
                HttpClientInitializer = TestProject.GetAdminCredential()
            });
        }

        public static IAMCredentialsService CreateIamCredentialsService()
        {
            return new IAMCredentialsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = TestProject.GetAdminCredential()
            });
        }

        public static CloudResourceManagerService CreateCloudResourceManagerService()
        {
            return new CloudResourceManagerService(new BaseClientService.Initializer
            {
                HttpClientInitializer = TestProject.GetAdminCredential()
            });
        }

        public static TService CreateService<TService>()
            where TService : BaseClientService
        {
            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = TestProject.GetAdminCredential()
            };

            return (TService)Activator.CreateInstance(
                typeof(TService),
                new object[] { initializer });
        }
    }
}
