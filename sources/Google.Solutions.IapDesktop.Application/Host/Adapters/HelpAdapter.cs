//
// Copyright 2020 Google LLC
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
using Google.Solutions.Platform.Net;

#pragma warning disable CA1822 // Mark members as static

namespace Google.Solutions.IapDesktop.Application.Host.Adapters
{
    public static class HelpTopics
    {
        private const string GaParameters = "utm_source=iapdesktop&utm_medium=help";

        public static readonly IHelpTopic General = new HelpTopic(
            "Documentation",
            $"https://googlecloudplatform.github.io/iap-desktop/?{GaParameters}");

        public static readonly IHelpTopic Shortcuts = new HelpTopic(
            "Keyboard shortcuts",
            $"https://googlecloudplatform.github.io/iap-desktop/keyboard-shortcuts/?{GaParameters}");

        public static readonly IHelpTopic BrowserIntegration = new HelpTopic(
            "Browser Integration",
            $"https://googlecloudplatform.github.io/iap-desktop/connect-by-url/?{GaParameters}");

        public static readonly IHelpTopic IapOverview = new HelpTopic(
            "Overview of Cloud IAP TCP forwarding",
            "https://cloud.google.com/iap/docs/tcp-forwarding-overview");

        public static readonly IHelpTopic IapAccess = new HelpTopic(
            "Grant access to VMs",
            $"https://googlecloudplatform.github.io/iap-desktop/setup-iap/?{GaParameters}#grant-access");

        public static readonly IHelpTopic CreateIapFirewallRule = new HelpTopic(
            "Create a firewall rule for IAP",
            $"https://googlecloudplatform.github.io/iap-desktop/setup-iap/?{GaParameters}#create-a-firewall-rule");

        public static readonly IHelpTopic SecureConnectDcaOverview = new HelpTopic(
            "Device certificate authentication",
            $"https://googlecloudplatform.github.io/iap-desktop/setup-caa-with-a-beyondcorp-certificate-access-policy/?{GaParameters}");

        public static readonly IHelpTopic PrivateServiceConnectOverview = new HelpTopic(
            "Access Google APIs through Private Service Connect",
            $"https://googlecloudplatform.github.io/iap-desktop/connect-to-google-cloud/?{GaParameters}");

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

        public static readonly IHelpTopic NodeUsageReporting = new HelpTopic(
           "Track VM and sole-tenant node usage for license reporting",
           "https://cloud.google.com/compute/docs/nodes/determining-server-usage");

        public static readonly IHelpTopic AppProtocols = new HelpTopic(
            "App protocols",
            $"https://googlecloudplatform.github.io/iap-desktop/client-application-configuration/?{GaParameters}");
        
        public static readonly IHelpTopic Privacy = new HelpTopic(
            "Privacy",
            $"https://googlecloudplatform.github.io/iap-desktop/security/?{GaParameters}");

        //TODO: Split, move to Session

        public static readonly IHelpTopic SshKeyStorage = new HelpTopic(
            "SSH key storage",
            $"https://googlecloudplatform.github.io/iap-desktop/security/?{GaParameters}"); //TODO: Add real link
    }

    public class HelpAdapter
    {
        public void OpenTopic(IHelpTopic topic)
        {
            Browser.Default.Navigate(topic.Address.ToString());
        }
    }
}
