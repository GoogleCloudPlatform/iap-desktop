﻿//
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

using System;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Application.Views
{
    public interface IHelpTopic
    {
        string Title { get; }
        Uri Address { get; }
    }

    public static class HelpTopics
    {
        public static IHelpTopic General = new HelpTopic(
            "Documentation",
            "https://github.com/GoogleCloudPlatform/iap-desktop/wiki");

        public static IHelpTopic BrowserIntegration = new HelpTopic(
            "Browser Integration",
            "https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Browser-Integration");

        public static IHelpTopic IapOverview = new HelpTopic(
            "Overview of Cloud IAP TCP forwarding",
            "https://cloud.google.com/iap/docs/tcp-forwarding-overview");

        public static IHelpTopic IapAccess = new HelpTopic(
            "Configuring access to Cloud IAP",
            "https://cloud.google.com/iap/docs/using-tcp-forwarding#grant-permission");

        public static IHelpTopic CreateIapFirewallRule = new HelpTopic(
            "Creating a firewall rule for Cloud IAP",
            "https://cloud.google.com/iap/docs/using-tcp-forwarding#create-firewall-rule");

        public static IHelpTopic SecureConnectDcaOverview = new HelpTopic(
            "Device certiticate authentication",
            "https://cloud.google.com/endpoint-verification/docs/overview");

        private class HelpTopic : IHelpTopic
        {
            public string Title { get; }
            public Uri Address { get; }

            public HelpTopic(string title, string address)
            {
                this.Title = title;
                this.Address = new Uri(address);
            }
        }
    }

    public class HelpService
    {
        public void OpenTopic(IHelpTopic topic)
        {
            using (Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = topic.Address.ToString()
            }))
            { };
        }
    }
}
