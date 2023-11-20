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
using Google.Apis.Json;
using Google.Apis.Logging.v2.Data;
using Google.Apis.Services;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Testing.Apis.Auth;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;

namespace Google.Solutions.Testing.Apis.Integration
{
    public static class TestProject
    {
        internal const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

        public static readonly string InvalidProjectId = "invalid-0000";

        private static GoogleCredential adminCredential;

        /// <summary>
        /// Test configuration, loaded from file.
        /// </summary>
        internal static ConfigurationSection Configuration { get; }

        /// <summary>
        /// User agent to use for tests.
        /// </summary>
        public static UserAgent UserAgent { get; }

        /// <summary>
        /// Project to run tests in.
        /// </summary>
        public static string ProjectId => Configuration.ProjectId;

        /// <summary>
        /// Zone to run tests in.
        /// </summary>
        public static string Zone => Configuration.Zone;

        public static ApiKey ApiKey => new ApiKey(Configuration.ApiKey);

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


            var configFile = Environment.GetEnvironmentVariable("IAPDESKTOP_CONFIGURATION");
            if (string.IsNullOrEmpty(configFile))
            {
                throw new ApplicationException(
                    "IAPDESKTOP_CONFIGURATION not set, must contain the " +
                    "path to a configuration file for integration tests.");
            }

            //
            // Load configuration file.
            //
            Configuration = NewtonsoftJsonSerializer
                .Instance
                .Deserialize<ConfigurationSection>(File.ReadAllText(configFile));
            if (string.IsNullOrEmpty(Configuration.ProjectId) )
            {
                throw new ApplicationException(
                    $"The configuration file {configFile} is incomplete.");
            }

            //
            // Load admin credential.
            //
            // This account must have the following roles:
            //
            // - Compute Admin
            // - Service Account Admin (to create service accounts)
            // - Service Account User (to access Compute Engine Service account)
            // - IAP-secured Tunnel User
            // - Logs Viewer
            // - Private Logs Viewer (for data access log)
            // - Project IAM Admin
            // - Service Account Token Creator
            //
            var credential = GoogleCredential.GetApplicationDefault();
            adminCredential = credential.IsCreateScopedRequired
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
                new TemporaryGaiaSession(
                    "invalid@gserviceaccount.com",
                    GoogleCredential.FromAccessToken("invalid")));
        }

        public static IAuthorization AdminAuthorization
        {
            get => new TemporaryAuthorization(
                new Enrollment(),
                new TemporaryGaiaSession(
                    "admin@gserviceaccount.com",
                    adminCredential));
        }

        public static IDeviceEnrollment DisabledEnrollment
        {
            get => new Enrollment();
        }

        public static ComputeService CreateComputeService()
        {
            return new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = adminCredential
            });
        }

        internal static IamService CreateIamService()
        {
            return new IamService(new BaseClientService.Initializer
            {
                HttpClientInitializer = adminCredential
            });
        }

        internal static IAMCredentialsService CreateIamCredentialsService()
        {
            return new IAMCredentialsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = adminCredential
            });
        }

        internal static CloudResourceManagerService CreateCloudResourceManagerService()
        {
            return new CloudResourceManagerService(new BaseClientService.Initializer
            {
                HttpClientInitializer = adminCredential
            });
        }

        internal static TService CreateService<TService>()
            where TService : BaseClientService
        {
            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = adminCredential
            };

            return (TService)Activator.CreateInstance(
                typeof(TService),
                new object[] { initializer });
        }

        //---------------------------------------------------------------------
        // Configuration.
        //---------------------------------------------------------------------

        public class ConfigurationSection
        {
            /// <summary>
            /// Project to run tests in.
            /// </summary>
            [JsonProperty("projectId")]
            public string ProjectId { get; internal set; }

            /// <summary>
            /// Zone to run tests in.
            /// </summary>
            [JsonProperty("zone")]
            public string Zone { get; internal set; }

            /// <summary>
            /// API key (for OS Login/workforce identity).
            /// </summary>
            [JsonProperty("apiKey")]
            public string ApiKey { get; internal set; }

            /// <summary>
            /// Workforce pool for test principals.
            /// 
            /// Create the pool as follows:
            /// 
            ///   gcloud iam workforce-pools create PROJECT_ID \
            ///     --location global \
            ///     --organization ORG_ID
            ///     
            /// </summary>
            [JsonProperty("workforcePoolId")]
            public string WorkforcePoolId { get; internal set; }

            /// <summary>
            /// Workforce pool provider for test principals.
            /// 
            /// Create the pool as follows:
            /// 
            ///   gcloud iam workforce-pools providers update-oidc identity-platform \
            ///     --workforce-pool PROJECT_ID \
            ///     --location global \
            ///     --attribute-mapping 'google.subject=assertion.sub, google.posix_username=assertion.sub' \
            ///     --client-id "PROJECT_ID" \
            ///     --issuer-uri https://securetoken.google.com/PROJECT_ID/ \
            ///     --web-sso-response-type "id-token" \
            ///     --web-sso-assertion-claims-behavior "only-id-token-claims"
            /// 
            /// </summary>
            [JsonProperty("workforceProviderId")]
            public string WorkforceProviderId { get; internal set; }

            /// <summary>
            /// API key for Identity Platform (in same project).
            /// </summary>
            [JsonProperty("identityPlatformApiKey")]
            public string IdentityPlatformApiKey { get; internal set; }
        }
    }
}
