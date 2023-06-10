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


using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Google.Solutions.IapDesktop.Extensions.Session.Views.App
{
    internal class OpenWithAppCommand : MenuCommandBase<IProjectModelNode>
    {
        private readonly IWin32Window ownerWindow;
        private readonly IJobService jobService;
        private readonly ICredentialDialog credentialDialog;
        private readonly AppContextFactory contextFactory;

        private static string CreateName(AppProtocol protocol)
        {
            return (protocol.Client as IWindowsAppClient)?.Name ?? protocol.Name;
        }

        public OpenWithAppCommand(
            IWin32Window ownerWindow,
            IJobService jobService,
            AppContextFactory contextFactory,
            ICredentialDialog credentialDialog)
            : base($"&{CreateName(contextFactory.Protocol)}")
        {
            this.ownerWindow = ownerWindow.ExpectNotNull(nameof(ownerWindow));
            this.jobService = jobService.ExpectNotNull(nameof(jobService));
            this.contextFactory = contextFactory.ExpectNotNull(nameof(contextFactory));
            this.credentialDialog = credentialDialog.ExpectNotNull(nameof(credentialDialog));
        }

        internal async Task<AppProtocolContext> CreateContextAsync(
            IProjectModelInstanceNode instance,
            CancellationToken cancellationToken)
        {
            var requiredCredential = NetworkCredentialType.Default;
            var client = this.contextFactory.Protocol.Client;
            if (client is IWindowsAppClient windowsClient)
            {
                requiredCredential = windowsClient.RequiredCredential;
            }

            var context = (AppProtocolContext)await this.contextFactory
                .CreateContextAsync(
                    instance,
                    requiredCredential == NetworkCredentialType.Rdp
                        ? (uint)AppProtocolContextFlags.TryUseRdpNetworkCredentials
                        : (uint)AppProtocolContextFlags.None,
                    cancellationToken)
                .ConfigureAwait(true);

            //
            // Check which credential we need to use.
            //
            if (requiredCredential == NetworkCredentialType.Prompt ||
                (requiredCredential == NetworkCredentialType.Rdp && context.NetworkCredential == null))
            {
                //
                // Prompt for network credentials.
                //
                if (this.credentialDialog.PromptForWindowsCredentials(
                    this.ownerWindow,
                    this.contextFactory.Protocol.Name,
                    $"Enter credentials for {instance.DisplayName}",
                    AuthenticationPackage.Any,
                    out var credential) != DialogResult.OK)
                {
                    //
                    // Cancelled.
                    //
                    throw new TaskCanceledException();
                }

                Debug.Assert(credential != null);
                context.NetworkCredential = credential;
            }
            else if (requiredCredential == NetworkCredentialType.Default)
            {
                context.NetworkCredential = null;
            }

            return context;
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override bool IsAvailable(IProjectModelNode context)
        {
            return context is IProjectModelInstanceNode instance;
        }

        protected override bool IsEnabled(IProjectModelNode context)
        {
            return this.contextFactory
                .Protocol
                .IsAvailable((IProjectModelInstanceNode)context);
        }

        public override async Task ExecuteAsync(IProjectModelNode node)
        {
            var instance = (IProjectModelInstanceNode)node;

            var context = await CreateContextAsync(instance, CancellationToken.None)
                .ConfigureAwait(true);

            //
            // Connect a transport. This can take a bit, so do it in a job.
            //
            var transport = await this.jobService
                .RunInBackground(
                    new JobDescription(
                        $"Connecting to {instance.Instance.Name}...",
                        JobUserFeedbackType.BackgroundFeedback),
                    cancellationToken => context.ConnectTransportAsync(cancellationToken))
                .ConfigureAwait(false);

            if (context.CanLaunchClient)
            {
                var process = context.LaunchClient(transport);
                process.Resume();

                //
                // Client app launched successfully. Keep the transport
                // open until the app is closed, but don't await.
                //
                _ = process
                    .WaitAsync(TimeSpan.MaxValue)
                    .ContinueWith(_ =>
                    {
                        transport.Dispose();
                        process.Dispose();
                    });
            }
            else
            {
                throw new NotImplementedException("Client cannot be launched");
            }
        }
    }
}
