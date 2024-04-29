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
using Google.Solutions.Mvvm.Shell;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App
{
    /// <summary>
    /// Connect an AppProtocol and open the client.
    /// </summary>
    internal class ConnectAppProtocolWithClientCommand : ConnectAppProtocolCommandBase
    {
        private readonly IWin32Window ownerWindow;
        private readonly ICredentialDialog credentialDialog;
        private readonly AppProtocolContextFactory contextFactory;
        private readonly bool forceCredentialPrompt;

        private static string CreateName(
            AppProtocol protocol,
            bool forceCredentialPrompt)
        {
            var name = protocol.Name;
            if (forceCredentialPrompt)
            {
                name += " as user...";
            }

            return name;
        }

        private static Image? CreateIcon(AppProtocol protocol)
        {
            if (protocol.Client != null &&
                protocol.Client.IsAvailable &&
                protocol.Client.Executable is string executable &&
                File.Exists(executable))
            {
                //
                // Try to extract the icon from the EXE file.
                //
                try
                {
                    return FileType
                        .Lookup(executable, FileAttributes.Normal, FileType.IconFlags.None)
                        .FileIcon;
                }
                catch
                { }
            }

            return null;
        }

        public ConnectAppProtocolWithClientCommand(
            IWin32Window ownerWindow,
            IJobService jobService,
            AppProtocolContextFactory contextFactory,
            ICredentialDialog credentialDialog,
            INotifyDialog notifyDialog,
            bool forceCredentialPrompt = false)
            : base(
                  $"&{CreateName(contextFactory.Protocol, forceCredentialPrompt)}",
                  jobService,
                  notifyDialog)
        {
            Debug.Assert(contextFactory.Protocol.Client != null);

            this.ownerWindow = ownerWindow.ExpectNotNull(nameof(ownerWindow));
            this.contextFactory = contextFactory.ExpectNotNull(nameof(contextFactory));
            this.credentialDialog = credentialDialog.ExpectNotNull(nameof(credentialDialog));
            this.forceCredentialPrompt = forceCredentialPrompt;

            this.Image = CreateIcon(contextFactory.Protocol);
        }

        protected internal override async Task<AppProtocolContext> CreateContextAsync(
            IProjectModelInstanceNode instance,
            CancellationToken cancellationToken)
        {
            var context = (AppProtocolContext)await this.contextFactory
                .CreateContextAsync(
                    instance,
                    (uint)AppProtocolContextFlags.TryUseRdpNetworkCredentials,
                    cancellationToken)
                .ConfigureAwait(true);

            var client = this.contextFactory.Protocol
                .Client
                .ExpectNotNull("Client is null");

            if (!client.IsNetworkLevelAuthenticationSupported ||
                context.Parameters.NetworkLevelAuthentication == AppNetworkLevelAuthenticationState.Disabled)
            {
                //
                // Reset network credentials, we're not supposed to
                // use these.
                //
                context.NetworkCredential = null;

                if (client.IsUsernameRequired &&
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
                        new CredentialDialogParameters()
                        {
                            Caption = this.contextFactory.Protocol.Name,
                            Message = $"Enter credentials for {instance.DisplayName}",
                            Package = AuthenticationPackage.Any
                        },
                        out var _,
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

        public override string Id
        {
            get => GetId(this.contextFactory.Protocol);
        }

        protected override bool IsEnabled(IProjectModelNode context)
        {
            return this.contextFactory
                .Protocol
                .IsAvailable((IProjectModelInstanceNode)context);
        }
    }
}
