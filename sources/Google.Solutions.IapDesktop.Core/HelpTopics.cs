﻿//
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

namespace Google.Solutions.IapDesktop.Core
{
    internal static class HelpTopics
    {
        private const string GaParameters = "utm_source=iapdesktop&utm_medium=help";

        public static readonly IHelpTopic IapAccess = new HelpTopic(
            "Configuring access to Cloud IAP",
            $"https://googlecloudplatform.github.io/iap-desktop/setup-iap/?{GaParameters}");

        public static readonly IHelpTopic CreateIapFirewallRule = new HelpTopic(
            "Creating a firewall rule for Cloud IAP",
            $"https://googlecloudplatform.github.io/iap-desktop/setup-iap/?{GaParameters}");

        public static readonly IHelpTopic ProxyConfiguration = new HelpTopic(
            "Proxy Configuration",
            $"https://googlecloudplatform.github.io/iap-desktop/proxy-configuration/?{GaParameters}");
    }
}
