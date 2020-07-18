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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Rdp.Properties;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Credentials;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class Extension
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IWin32Window window;

        private static CommandState GetGenerateCredentialToolbarCommandState(IProjectExplorerNode node)
        {
            return node is IProjectExplorerVmInstanceNode vmNode && vmNode.IsRunning
                ? CommandState.Enabled
                : CommandState.Disabled;
        }

        private static CommandState GetGenerateCredentialContextMenuCommandState(IProjectExplorerNode node)
        {
            if (node is IProjectExplorerVmInstanceNode vmNode)
            {
                return vmNode.IsRunning
                    ? CommandState.Enabled
                    : CommandState.Disabled;
            }
            else
            {
                return CommandState.Unavailable;
            }
        }

        private async void GenerateCredentials(IProjectExplorerNode node)
        {
            try
            {
                if (node is IProjectExplorerVmInstanceNode vmNode)
                {
                    var credentialService = this.serviceProvider.GetService<ICredentialsService>();
                    await credentialService.GenerateCredentialsAsync(
                            this.window,
                            vmNode.Reference,
                            vmNode.SettingsEditor)
                        .ConfigureAwait(true);
                }
            }
            catch (Exception e) when (e.IsCancellation())
            {
                // Ignore.
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this.window, "Generating credentials failed", e);
            }
        }

        //---------------------------------------------------------------------

        public Extension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.window = serviceProvider.GetService<IMainForm>().Window;

            //
            // Add commands to project explorer.
            //
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "&Generate Windows logon credentials...",
                    GetGenerateCredentialContextMenuCommandState,
                    GenerateCredentials)
                {
                    Image = Resources.Password_16
                },
                1);

            projectExplorer.ToolbarCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Generate Windows logon credentials",
                    GetGenerateCredentialToolbarCommandState,
                    GenerateCredentials)
                {
                    Image = Resources.Password_16
                });
        }
    }
}
