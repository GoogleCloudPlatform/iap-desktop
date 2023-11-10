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

namespace Google.Solutions.Testing.Apis.Integration
{
    public static class PredefinedRole
    {
        public const string ComputeViewer = "roles/compute.viewer";
        public const string ComputeInstanceAdminV1 = "roles/compute.instanceAdmin.v1";
        public const string LogsViewer = "roles/logging.viewer";
        public const string ServiceAccountUser = "roles/iam.serviceAccountUser";
        public const string StorageObjectViewer = "roles/storage.objectViewer";
        public const string StorageAdmin = "roles/storage.admin";
        public const string OsLogin = "roles/compute.osLogin";
        public const string ServiceUsageConsumer = "roles/serviceusage.serviceUsageConsumer";

        // NB. This role takes ~1 minute to take effect.
        public const string IapTunnelUser = "roles/iap.tunnelResourceAccessor";
    }
}
