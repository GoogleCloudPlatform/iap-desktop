//
// Copyright 2024 Google LLC
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
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;

namespace Google.Solutions.IapDesktop.Extensions.Explorer.ToolWindows.Install
{
    /// <summary>
    /// Base class for Help menu commands.
    /// </summary>
    public abstract class OpenHelpCommand : MenuCommandBase<IInstall>
    {
        private readonly HelpClient helpClient;
        private readonly IHelpTopic topic;

        protected OpenHelpCommand(
            string text,
            IHelpTopic topic,
            HelpClient helpClient)
            : base(text)
        {
            this.topic = topic;
            this.helpClient = helpClient;
            this.Image = Resources.Documentation_16;
        }

        protected override bool IsAvailable(IInstall _)
        {
            return true;
        }

        protected override bool IsEnabled(IInstall _)
        {
            return true;
        }

        public override void Execute(IInstall context)
        {
            this.helpClient.OpenTopic(this.topic);
        }

        [MenuCommand(typeof(HelpMenu), Rank = 0x100)]
        [Service]
        public class General : OpenHelpCommand
        {
            public General(HelpClient helpClient)
                : base(
                      "&Documentation", 
                      HelpTopics.General, 
                      helpClient)
            {
            }
        }

        [MenuCommand(typeof(HelpMenu), Rank = 0x101)]
        [Service]
        public class Shortcuts : OpenHelpCommand
        {
            public Shortcuts(HelpClient helpClient)
                : base(
                      "Keyboard &shortcuts", 
                      HelpTopics.Shortcuts, 
                      helpClient)
            {
            }
        }

        [MenuCommand(typeof(HelpMenu), Rank = 0x200)]
        [Service]
        public class IapOverview : OpenHelpCommand
        {
            public IapOverview(HelpClient helpClient)
                : base(
                      "&IAP TCP forwarding overview", 
                      HelpTopics.IapOverview, 
                      helpClient)
            {
            }
        }

        [MenuCommand(typeof(HelpMenu), Rank = 0x201)]
        [Service]
        public class CertificateBasedAccessOverview : OpenHelpCommand
        {
            public CertificateBasedAccessOverview(HelpClient helpClient)
                : base(
                      "Using &certificate-based access",
                      HelpTopics.CertificateBasedAccessOverview,
                      helpClient)
            {
            }
        }

        [MenuCommand(typeof(HelpMenu), Rank = 0x300)]
        [Service]
        public class CreateIapFirewallRule : OpenHelpCommand
        {
            public CreateIapFirewallRule(HelpClient helpClient)
                : base(
                      "How to create a &firewall rule for IAP",
                      HelpTopics.CreateIapFirewallRule,
                      helpClient)
            {
            }
        }

        [MenuCommand(typeof(HelpMenu), Rank = 0x301)]
        [Service]
        public class IapAccess : OpenHelpCommand
        {
            public IapAccess(HelpClient helpClient)
                : base(
                      "How to &grant permissions to use IAP",
                      HelpTopics.IapAccess,
                      helpClient)
            {
            }
        }
    }
}
