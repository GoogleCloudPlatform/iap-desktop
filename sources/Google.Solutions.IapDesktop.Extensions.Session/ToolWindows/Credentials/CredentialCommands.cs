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

using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Properties;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials
{
    [Service]
    public class CredentialCommands
    {
        public CredentialCommands(
            IConnectionSettingsService settingsService,
            IRdpCredentialEditorFactory rdpCredentialEditor)
        {
            this.ContextMenuNewCredentials = new NewCredentialsCommand(
                settingsService,
                rdpCredentialEditor)
            {
                CommandType = MenuCommandType.MenuCommand
            };
            this.ToolbarNewCredentials = new NewCredentialsCommand(
                settingsService,
                rdpCredentialEditor)
            {
                CommandType = MenuCommandType.MenuCommand
            };
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> ContextMenuNewCredentials { get; }
        public IContextCommand<IProjectModelNode> ToolbarNewCredentials { get; }

        private class NewCredentialsCommand : MenuCommandBase<IProjectModelNode>
        {
            private readonly IConnectionSettingsService settingsService;
            private readonly IRdpCredentialEditorFactory rdpCredentialEditor;

            public NewCredentialsCommand(
                IConnectionSettingsService settingsService,
                IRdpCredentialEditorFactory rdpCredentialEditor)
                : base("New logon &credentials...")
            {
                this.settingsService = settingsService;
                this.rdpCredentialEditor = rdpCredentialEditor;

                this.Image = Resources.AddCredentials_16;
                this.ActivityText = "Generating new Windows logon credentials";
            }

            public override string Id => "GenerateWindowsCredentials";

            protected override bool IsAvailable(IProjectModelNode node)
            {
                return node is IProjectModelInstanceNode instanceNode &&
                    instanceNode.IsWindowsInstance();
            }

            protected override bool IsEnabled(IProjectModelNode node)
            {
                return node is IProjectModelInstanceNode instanceNode &&
                   instanceNode.IsRunning;
            }

            public override async Task ExecuteAsync(IProjectModelNode node)
            {
                Debug.Assert(IsAvailable(node));
                Debug.Assert(IsEnabled(node));

                var settings = this.settingsService.GetConnectionSettings(node);

                await this.rdpCredentialEditor
                    .Edit(settings.TypedCollection)
                    .GenerateCredentialsAsync(false)
                    .ConfigureAwait(false);

                settings.Save();
            }
        }
    }
}
