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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Settings;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials
{
    public interface ICreateCredentialsWorkflow
    {
        Task<NetworkCredential> CreateCredentialsAsync(
            IWin32Window? owner,
            InstanceLocator instanceLocator,
            string? username,
            bool silent);

        Task CreateCredentialsAsync(
            IWin32Window? owner,
            InstanceLocator instanceRef,
            Settings.ConnectionSettings settings,
            bool silent);

        Task<bool> IsGrantedPermissionToGenerateCredentials(
            InstanceLocator instanceRef);
    }

    [Service(typeof(ICreateCredentialsWorkflow))]
    public class CreateCredentialsWorkflow : ICreateCredentialsWorkflow
    {
        private readonly IAuthorization authorization;
        private readonly IJobService jobService;
        private readonly IWindowsCredentialGenerator credentialGenerator;
        private readonly IDialogFactory<NewCredentialsView, NewCredentialsViewModel> newCredentialFactory;
        private readonly IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel> showCredentialFactory;

        internal CreateCredentialsWorkflow(
            IAuthorization authorization,
            IJobService jobService,
            IWindowsCredentialGenerator credentialGenerator,
            IDialogFactory<NewCredentialsView, NewCredentialsViewModel> newCredentialFactory,
            IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel> showCredentialFactory)
        {
            this.authorization = authorization;
            this.jobService = jobService;
            this.credentialGenerator = credentialGenerator;
            this.newCredentialFactory = newCredentialFactory;
            this.showCredentialFactory = showCredentialFactory;
        }

        public CreateCredentialsWorkflow(IServiceProvider serviceProvider)
            : this(
                serviceProvider.GetService<IAuthorization>(),
                serviceProvider.GetService<IJobService>(),
                serviceProvider.GetService<IWindowsCredentialGenerator>(),
                serviceProvider.GetViewFactory<NewCredentialsView, NewCredentialsViewModel>(),
                serviceProvider.GetViewFactory<ShowCredentialsView, ShowCredentialsViewModel>())
        {
            this.newCredentialFactory.Theme = serviceProvider.GetService<IThemeService>().DialogTheme;
            this.showCredentialFactory.Theme = serviceProvider.GetService<IThemeService>().DialogTheme;
        }

        public async Task<NetworkCredential> CreateCredentialsAsync(
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

        public async Task CreateCredentialsAsync(
            IWin32Window? owner,
            InstanceLocator instanceLocator,
            Settings.ConnectionSettings settings,
            bool silent)
        {
            var credentials = await CreateCredentialsAsync(
                owner,
                instanceLocator,
                settings.RdpUsername.Value,
                silent);

            //
            // Save credentials.
            //
            settings.RdpUsername.Value = credentials.UserName;
            settings.RdpPassword.SetClearTextValue(credentials.Password);

            //
            // NB. The computer might be joined to a domain, therefore force a local logon.
            //
            settings.RdpDomain.Value = ".";
        }

        public async Task<bool> IsGrantedPermissionToGenerateCredentials(InstanceLocator instance)
        {
            return await this.credentialGenerator
                .IsGrantedPermissionToCreateWindowsCredentialsAsync(instance)
                .ConfigureAwait(false);
        }
    }
}
