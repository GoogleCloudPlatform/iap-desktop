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
using Google.Solutions.Apis.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Session
{
    internal static class HelpTopics
    {
        private const string GaParameters = "utm_source=iapdesktop&utm_medium=help";

        public static readonly IHelpTopic AppProtocols = new HelpTopic(
            "App protocols",
            $"https://googlecloudplatform.github.io/iap-desktop/client-application-configuration/?{GaParameters}");

        public static readonly IHelpTopic ManagingMetadataAuthorizedKeys = new HelpTopic(
            "Managing SSH keys in metadata",
            "https://cloud.google.com/compute/docs/instances/adding-removing-ssh-keys");

        public static readonly IHelpTopic TroubleshootingOsLogin = new HelpTopic(
            "Troubleshooting OS Login",
            "https://cloud.google.com/compute/docs/oslogin/troubleshoot-os-login");

        public static readonly IHelpTopic GrantingOsLoginRoles = new HelpTopic(
            "Granting OS Login IAM roles",
            "https://cloud.google.com/compute/docs/instances/managing-instance-access#grant-iam-roles");

        public static readonly IHelpTopic EnableOsLoginSecurityKeys = new HelpTopic(
            "Using security keys with OS Login",
            "https://cloud.google.com/compute/docs/oslogin/security-keys");

        public static readonly IHelpTopic TroubleshootingSsh = new HelpTopic(
            "SSH troubleshooting",
            $"https://googlecloudplatform.github.io/iap-desktop/troubleshooting-ssh/?{GaParameters}");

    }
}
