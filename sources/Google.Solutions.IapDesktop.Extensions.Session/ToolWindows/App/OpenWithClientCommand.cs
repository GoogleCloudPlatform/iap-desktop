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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App
{
    internal class OpenWithClientCommand : MenuCommandBase<IProjectModelNode>
    {
        private readonly IWin32Window ownerWindow;
        private readonly IJobService jobService;
        private readonly ICredentialDialog credentialDialog;
        private readonly AppContextFactory contextFactory;
        private readonly bool forceCredentialPrompt;

        private static string CreateName(
            AppProtocol protocol,
            bool forceCredentialPrompt)
        {
            var name = (protocol.Client as IWindowsProtocolClient)?.Name ?? protocol.Name;
            if (forceCredentialPrompt)
            {
                name += " as user...";
            }

            return name;
        }

        public OpenWithClientCommand(
            IWin32Window ownerWindow,
            IJobService jobService,
            AppContextFactory contextFactory,
            ICredentialDialog credentialDialog,
            bool forceCredentialPrompt = false)
            : base($"&{CreateName(contextFactory.Protocol, forceCredentialPrompt)}")
        {
            this.ownerWindow = ownerWindow.ExpectNotNull(nameof(ownerWindow));
            this.jobService = jobService.ExpectNotNull(nameof(jobService));
            this.contextFactory = contextFactory.ExpectNotNull(nameof(contextFactory));
            this.credentialDialog = credentialDialog.ExpectNotNull(nameof(credentialDialog));
            this.forceCredentialPrompt = forceCredentialPrompt;

            if (contextFactory.Protocol.Client is IWindowsProtocolClient appClient &&
                appClient.Icon != null)
            {
                this.Image = appClient.Icon;
            }
        }

        internal async Task<AppProtocolContext> CreateContextAsync(
            IProjectModelInstanceNode instance,
            CancellationToken cancellationToken)
        {
            var context = (AppProtocolContext)await this.contextFactory
                .CreateContextAsync(
                    instance,
                    (uint)AppProtocolContextFlags.TryUseRdpNetworkCredentials,
                    cancellationToken)
                .ConfigureAwait(true);

            var windowsClient = this.contextFactory.Protocol.Client as IWindowsProtocolClient;
            if (windowsClient == null)
            {
                //
                // Reset network credentials, we're not supposed to
                // use these.
                //
                context.NetworkCredential = null;
                return context;
            }


            if (!windowsClient.IsNetworkLevelAuthenticationSupported ||
                context.Parameters.NetworkLevelAuthentication == AppNetworkLevelAuthenticationState.Disabled)
            {
                //
                // Reset network credentials, we're not supposed to
                // use these.
                //
                context.NetworkCredential = null;

                if (windowsClient.IsUsernameRequired &&
                    (this.forceCredentialPrompt || string.IsNullOrEmpty(context.Parameters.PreferredUsername)))
                {
                    //
                    // Prompt for a username.
                    //
                    if (this.credentialDialog.PromptForUsername(
                        this.ownerWindow,
                        this.contextFactory.Protocol.Name,
                        $"Enter username for {instance.DisplayName}",
                        out var username) != DialogResult.OK)
                    {
                        //
                        // Cancelled.
                        //
                        throw new TaskCanceledException();
                    }

                    Debug.Assert(username != null);
                    context.Parameters.PreferredUsername = username;
                }
            }
            else
            {
                //
                // NLA enabled, make sure we actually have credentials.
                //
                if (this.forceCredentialPrompt || context.NetworkCredential == null)
                {
                    //
                    // Prompt for Windows credentials.
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

                Debug.Assert(context.NetworkCredential != null);
            }

            return context;
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override bool IsAvailable(IProjectModelNode context)
        {
            return context is IProjectModelInstanceNode;
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
                .RunAsync(
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
                    .WaitAsync(TimeSpan.MaxValue, CancellationToken.None)
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
