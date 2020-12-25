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

using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using Google.Apis.Services;
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Ssh.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Ssh.Views.Terminal;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class SshExtension
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IWin32Window window;

        private static CommandState GetContextMenuCommandStateWhenRunningInstanceRequired(IProjectExplorerNode node)
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

        //---------------------------------------------------------------------
        // Commands.
        //---------------------------------------------------------------------

        private async Task __AddPublicKeyAsync(
            InstanceLocator instance,
            string username,
            ISshKey key)
        {
            var authz = serviceProvider.GetService<IAuthorizationAdapter>().Authorization;
            var service = new ComputeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = authz.Credential,
            });

            var rsaPublicKey = Convert.ToBase64String(key.PublicKey);
            await service.Instances.AddMetadataAsync(
                    instance,
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = "ssh-keys",
                                Value = $"{username}:ssh-rsa {rsaPublicKey} {username}"
                            }
                        }
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        private async void ConnectToPublicEndpoint(IProjectExplorerNode node)
        {
            try
            {
                if (node is IProjectExplorerVmInstanceNode vmNode)
                {
                    // TODO: Use real instance/endpoint
                    var instanceLocator = new InstanceLocator(
                        "iap-windows-rdc-plugin-tests",
                        "us-central1-a",
                        "u5399e2efe48901");
                    var endpoint = new IPEndPoint(
                        IPAddress.Parse("35.226.214.252"),
                        22);

                    using (var gceAdapter = this.serviceProvider.GetService<IComputeEngineAdapter>())
                    using (var keysAdapter = this.serviceProvider.GetService<IComputeEngineKeysAdapter>())
                    {
                        // TODO: Use proper key.
                        var key = RsaSshKey.NewEphemeralKey();

                        await keysAdapter.PushPublicKeyAsync(
                                instanceLocator,
                                "test",
                                key,
                                CancellationToken.None)
                            .ConfigureAwait(true);

                        // TODO: Use ActivateOrConnectInstanceAsync
                        await this.serviceProvider
                            .GetService<ISshTerminalConnectionBroker>()
                            .ConnectAsync(
                                vmNode.Reference,
                                "test",
                                endpoint,
                                key)
                            .ConfigureAwait(true);
                    }
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
                    .Show(this.window, "Connecting to VM instance failed", e);
            }
        }

        //---------------------------------------------------------------------
        // Setup
        //---------------------------------------------------------------------

        public SshExtension(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            this.window = serviceProvider.GetService<IMainForm>().Window;

            //
            // Connect.
            //
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "DEBUG: Connect to SSH",
                    GetContextMenuCommandStateWhenRunningInstanceRequired,
                    ConnectToPublicEndpoint)
                {
                },
                1);
        }
    }
}
