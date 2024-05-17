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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Apis;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Solutions.Mvvm.Controls;
using System.Linq;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session
{
    /// <summary>
    /// Lets users edit or amend RDP credentials used in 
    /// connection settings.
    /// </summary>
    public interface IRdpCredentialEditor
    {
        /// <summary>
        /// Check if changes are allowed to be saved.
        /// </summary>
        public bool AllowSave { get; set; }

        /// <summary>
        /// Prompt user to enter password.
        /// </summary>
        /// <exception cref="OperationCanceledException">when cancelled by user</exception>
        void PromptForCredentials();

        /// <summary>
        /// Generate new credentials and update connection settings.
        /// </summary>
        /// <exception cref="OperationCanceledException">when cancelled by user</exception>
        Task GenerateCredentialsAsync(bool silent);

        /// <summary>
        /// Amend existing credentials if they are incomplete.
        /// </summary>
        /// <exception cref="OperationCanceledException">when cancelled by user</exception>
        Task AmendCredentialsAsync(
            RdpCredentialGenerationBehavior generationBehavior);
    }

    internal class RdpCredentialEditor : IRdpCredentialEditor
    {
        private readonly IWin32Window? owner;
        private readonly IAuthorization authorization;
        private readonly IJobService jobService;
        private readonly IWindowsCredentialGenerator credentialGenerator;
        private readonly ITaskDialog taskDialog;
        private readonly ICredentialDialog credentialDialog;
        private readonly IWindowFactory<NewCredentialsView, NewCredentialsViewModel> newCredentialFactory;
        private readonly IWindowFactory<ShowCredentialsView, ShowCredentialsViewModel> showCredentialFactory;

        internal RdpCredentialEditor(
            IWin32Window? owner,
            ConnectionSettings settings,
            IAuthorization authorization,
            IJobService jobService,
            IWindowsCredentialGenerator credentialGenerator,
            ITaskDialog taskDialog,
            ICredentialDialog credentialDialog,
            IWindowFactory<NewCredentialsView, NewCredentialsViewModel> newCredentialFactory,
            IWindowFactory<ShowCredentialsView, ShowCredentialsViewModel> showCredentialFactory)
        {
            this.owner = owner;
            this.Settings = settings;

            this.authorization = authorization;
            this.jobService = jobService;
            this.credentialGenerator = credentialGenerator;
            this.taskDialog = taskDialog;
            this.credentialDialog = credentialDialog;
            this.newCredentialFactory = newCredentialFactory;
            this.showCredentialFactory = showCredentialFactory;

            Debug.Assert(settings.Resource is InstanceLocator);
        }

        /// <summary>
        /// Settings that are being editied.
        /// </summary>
        public ConnectionSettings Settings { get; }

        /// <summary>
        /// Instance for which settings are being edited.
        /// </summary>
        public InstanceLocator Instance
        {
            get => (InstanceLocator)this.Settings.Resource;
        }

        internal bool AreCredentialsComplete
        {
            get => 
                !string.IsNullOrEmpty(this.Settings.RdpUsername.Value) &&
                !string.IsNullOrEmpty(this.Settings.RdpPassword.GetClearTextValue());
        }

        internal async Task<bool> IsGrantedPermissionToCreateWindowsCredentialsAsync()
        {
            //
            // The call can fail if the session expired, but it's not worth/
            // reasonable to move the call into a job. Therefore, catch
            // the reauth error and fail open so that the re-auth error
            // can be handled during the connection or password reset
            // attempt.
            //
            try
            {
                return await this.credentialGenerator
                    .IsGrantedPermissionToCreateWindowsCredentialsAsync(this.Instance)
                    .ConfigureAwait(false);
            }
            catch (Exception e) when (e.IsReauthError())
            {
                return true;
            }
        }

        internal async Task<NetworkCredential> CreateCredentialsAsync(
            IWin32Window? owner,
            InstanceLocator instanceLocator,
            string? username,
            bool silent)
        {
            if (username == null ||
                string.IsNullOrEmpty(username) ||
                !WindowsUser.IsLocalUsername(username))
            {
                username = WindowsUser.SuggestUsername(this.authorization.Session);
            }

            if (!silent)
            {
                //
                // Prompt user to customize the defaults.
                //
                using (var dialog = this.newCredentialFactory.CreateDialog())
                {
                    dialog.ViewModel.Username = username;
                    if (dialog.ShowDialog(owner) == DialogResult.OK)
                    {
                        username = dialog.ViewModel.Username;
                    }
                    else
                    {
                        throw new OperationCanceledException();
                    }
                }
            }

            var credentials = await this.jobService.RunAsync(
                new JobDescription("Generating Windows logon credentials..."),
                token => this.credentialGenerator
                    .CreateWindowsCredentialsAsync(
                        instanceLocator,
                        username,
                        UserFlags.AddToAdministrators,
                        token))
                    .ConfigureAwait(true);

            if (!silent)
            {
                using (var dialog = this.showCredentialFactory.CreateDialog(
                    new ShowCredentialsViewModel(
                        credentials.UserName,
                        credentials.Password)))
                {
                    dialog.ShowDialog(owner);
                }
            }

            return credentials;
        }

        //---------------------------------------------------------------------
        // IRdpCredentialEditor.
        //---------------------------------------------------------------------

        public bool AllowSave { get; set; } = true;

        public void PromptForCredentials()
        {
            var parameters = new CredentialDialogParameters()
            {
                Caption = $"Enter your credentials for {this.Instance.Name}",
                Message = "These credentials will be used to connect to the VM",
                ShowSaveCheckbox = this.AllowSave,
                InputCredential = string.IsNullOrEmpty(this.Settings.RdpUsername.Value)
                    ? null
                    : new NetworkCredential(
                        this.Settings.RdpUsername.Value,
                        (string?)null,
                        this.Settings.RdpDomain.Value)
            };

            if (this.credentialDialog.PromptForWindowsCredentials(
                this.owner,
                parameters,
                out var save,
                out var credential) == DialogResult.Cancel || credential == null)
            {
                throw new OperationCanceledException();
            }

            this.AllowSave = save;
            this.Settings.RdpUsername.Value = credential.UserName;
            this.Settings.RdpPassword.SetClearTextValue(credential.Password);
            this.Settings.RdpDomain.Value = credential.Domain;
        }

        public async Task GenerateCredentialsAsync(bool silent)
        {
            var credentials = await CreateCredentialsAsync(
                owner,
                this.Instance,
                this.Settings.RdpUsername.Value,
                silent);

            //
            // Save credentials.
            //
            this.Settings.RdpUsername.Value = credentials.UserName;
            this.Settings.RdpPassword.SetClearTextValue(credentials.Password);

            //
            // NB. The computer might be joined to a domain, therefore force a local logon.
            //
            this.Settings.RdpDomain.Value = ".";
        }

        public async Task AmendCredentialsAsync(
            RdpCredentialGenerationBehavior allowedBehavior)
        { 
            if (this.Settings.RdpNetworkLevelAuthentication.Value 
                == RdpNetworkLevelAuthentication.Disabled)
            {
                //
                // When NLA is disabled, RDP credentials don't matter.
                //
                return;
            }
            else if (allowedBehavior == RdpCredentialGenerationBehavior.Force && 
                await IsGrantedPermissionToCreateWindowsCredentialsAsync()
                    .ConfigureAwait(true))
            {
                //
                // Silently generate new credentials right away and
                // skip any further prompts.
                //
                await GenerateCredentialsAsync(true)
                    .ConfigureAwait(false);
                return;
            }

            //
            // Prepare to show a dialog.
            //

            const DialogResult GenerateNewCredentialsResult = (DialogResult)0x1000;
            const DialogResult EnterCredentialsResult = (DialogResult)0x1001;

            var dialogParameters = new TaskDialogParameters(
                "Credentials",
                $"You do not have any saved credentials for {this.Instance.Name}",
                "How do you want to proceed?");
            dialogParameters.Buttons.Add(TaskDialogStandardButton.Cancel);

            if ((allowedBehavior == RdpCredentialGenerationBehavior.Allow ||
                (allowedBehavior == RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound 
                    && !this.AreCredentialsComplete)) &&
               await IsGrantedPermissionToCreateWindowsCredentialsAsync().ConfigureAwait(true))
            {
                dialogParameters.Buttons.Add(new TaskDialogCommandLinkButton(
                    "Generate new credentials",
                    GenerateNewCredentialsResult));
            }
            else if (this.AreCredentialsComplete)
            {
                //
                // We have credentials, so just go ahead and connect.
                // 
                return;
            }

            dialogParameters.Buttons.Add(new TaskDialogCommandLinkButton(
                "Enter credentials manually",
                EnterCredentialsResult));

            DialogResult result;
            if (dialogParameters.Buttons.OfType<TaskDialogCommandLinkButton>().Count() > 1)
            {
                result = this.taskDialog.ShowDialog(this.owner, dialogParameters);
            }
            else
            {
                //
                // There's no point in showing a dialog when there's only
                // a single option to choose from.
                //
                result = EnterCredentialsResult;
            }

            switch (result)
            {
                case GenerateNewCredentialsResult:
                    await GenerateCredentialsAsync(false);
                    break;

                case EnterCredentialsResult:
                    PromptForCredentials();
                    break;

                case DialogResult.Cancel:
                    throw new OperationCanceledException();

                default:
                    break;
            }
        }
    }
}
