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
using System.Reflection;

namespace Google.Solutions.Common.Test.Integration
{
    public static class TestProject
    {
        internal const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

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
            var credential = GoogleCredential.GetApplicationDefault();
            return credential.IsCreateScopedRequired
                ? credential.CreateScoped(CloudPlatformScope)
                : credential;
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
    }
}
