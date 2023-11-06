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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Testing.Apis.Auth;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Testing.Apis.Integration
{
    public static class TestProject
    {
        internal const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

        public static readonly string InvalidProjectId = "invalid-0000";
        public static readonly string Zone = "us-central1-a";

        public static UserAgent UserAgent { get; }

        static TestProject()
        {
            UserAgent = new UserAgent(
                "IAP-Desktop-TestSuite",
                Assembly.GetExecutingAssembly().GetName().Version);

            //
            // Enable TLS 1.2.
            //
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 |
                SecurityProtocolType.Tls11;
        }

        public static string ProjectId
        {
            get
            {
                var projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
                if (string.IsNullOrEmpty(projectId))
                {
                    throw new ApplicationException(
                        "GOOGLE_CLOUD_PROJECT not set, must contain the project ID " +
                        "to use for integration tests");
                }

                return projectId;
            }
        }

        public static string WorkforcePoolId => ProjectId;
        public static string WorkforceProviderId => "identity-platform";

        internal static GoogleCredential GetAdminCredential()
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

        public static IAuthorization SecureConnectAuthorization
        {
            get
            {
                //
                // This account must have:
                // - Cloud Identity Premium
                // - an associated device certificate on the local machine
                //
                var credentialsPath = Environment.GetEnvironmentVariable("SECURECONNECT_CREDENTIALS");
                if (string.IsNullOrEmpty(credentialsPath))
                {
                    throw new ApplicationException(
                        "SECURECONNECT_CREDENTIALS not set, needs to point to credentials " +
                        "JSON of a SecureConnect-enabled user");
                }

                var certificatePath = Environment.GetEnvironmentVariable("SECURECONNECT_CERTIFICATE");
                if (string.IsNullOrEmpty(certificatePath))
                {
                    throw new ApplicationException(
                        "SECURECONNECT_CERTIFICATE not set, needs to point to a PFX " +
                        "containing a SecureConnect device certificate");
                }

                var collection = new X509Certificate2Collection();
                collection.Import(
                    certificatePath,
                    string.Empty, // No passphrase
                    X509KeyStorageFlags.DefaultKeySet);

                var certificate = collection
                    .OfType<X509Certificate2>()
                    .First();

                var credential = GoogleCredential.FromFile(credentialsPath);

                return new TemporaryAuthorization(
                    new Enrollment(certificate),
                    new TemporaryGaiaSession(
                        ((UserCredential)credential.UnderlyingCredential).UserId,
                        credential.IsCreateScopedRequired
                            ? credential.CreateScoped(CloudPlatformScope)
                            : credential));

            }
        }

        public static IAuthorization InvalidAuthorization
        {
            get => new TemporaryAuthorization(
                new Enrollment(),
                new TemporarySession(
                    "invalid@gserviceaccount.com",
                    GoogleCredential.FromAccessToken("invalid")));
        }


        public static IAuthorization AdminAuthorization
        {
            get => new TemporaryAuthorization(
                new Enrollment(),
                new TemporaryGaiaSession(
                    "admin@gserviceaccount.com",
                    GetAdminCredential()));
        }

        public static IDeviceEnrollment DisabledEnrollment
        {
            get => new Enrollment();
        }

        public static ComputeService CreateComputeService()
        {
            return new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GetAdminCredential()
            });
        }

        internal static IamService CreateIamService()
        {
            return new IamService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GetAdminCredential()
            });
        }

        internal static IAMCredentialsService CreateIamCredentialsService()
        {
            return new IAMCredentialsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GetAdminCredential()
            });
        }

        internal static CloudResourceManagerService CreateCloudResourceManagerService()
        {
            return new CloudResourceManagerService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GetAdminCredential()
            });
        }

        internal static TService CreateService<TService>()
            where TService : BaseClientService
        {
            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = GetAdminCredential()
            };

            return (TService)Activator.CreateInstance(
                typeof(TService),
                new object[] { initializer });
        }

        internal static TemporaryWorkforcePoolSubject.IdentityPlatformService CreateIdentityPlatformService()
        {
            var apiKey = Environment.GetEnvironmentVariable("IDENTITYPLATFORM_APIKEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ApplicationException(
                    "IDENTITYPLATFORM_APIKEY not set, must contain an API key " +
                    $"for the project {ProjectId}");
            }

            return new TemporaryWorkforcePoolSubject.IdentityPlatformService(apiKey);
        }
    }
}
