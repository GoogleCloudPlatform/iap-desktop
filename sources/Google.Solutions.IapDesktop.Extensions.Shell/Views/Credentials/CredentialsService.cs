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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials
{
    public interface ICredentialsService
    {
        Task GenerateCredentialsAsync(
            IWin32Window owner,
            InstanceLocator instanceRef,
            ConnectionSettingsBase settings,
            bool silent);

        Task<bool> IsGrantedPermissionToGenerateCredentials(
            InstanceLocator instanceRef);
    }

    [Service(typeof(ICredentialsService))]
    public class CredentialsService : ICredentialsService
    {
        private readonly IServiceProvider serviceProvider;

        public CredentialsService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task GenerateCredentialsAsync(
            IWin32Window owner,
            InstanceLocator instanceLocator,
            ConnectionSettingsBase settings,
            bool silent)
        {
            var username = string.IsNullOrEmpty(settings.RdpUsername.StringValue)
                ? this.serviceProvider
                    .GetService<IAuthorizationAdapter>()
                    .Authorization
                    .SuggestWindowsUsername()
                : settings.RdpUsername.StringValue;

            if (!silent)
            {
                //
                // Prompt user to customize the defaults.
                //

                var dialogResult = this.serviceProvider
                    .GetService<IGenerateCredentialsDialog>()
                    .ShowDialog(owner,
                    username);

                if (dialogResult.Result == DialogResult.OK)
                {
                    username = dialogResult.Username;
                }
                else
                {
                    throw new OperationCanceledException();
                }
            }

            using (var windowsCredentialAdapter = this.serviceProvider.GetService<IWindowsCredentialAdapter>())
            {
                var credentials = await this.serviceProvider.GetService<IJobService>().RunInBackground(
                    new JobDescription("Generating Windows logon credentials..."),
                    token => windowsCredentialAdapter
                        .CreateWindowsCredentialsAsync(
                            instanceLocator,
                            username,
                            flags,
                            token))
                    .ConfigureAwait(true);

                if (!silent)
                {
                    this.serviceProvider.GetService<IShowCredentialsDialog>().ShowDialog(
                        owner,
                        credentials.UserName,
                        credentials.Password);
                }

                // Save credentials.
                settings.RdpUsername.StringValue = credentials.UserName;
                settings.RdpPassword.ClearTextValue = credentials.Password;

                // NB. The computer might be joined to a domain, therefore force a local logon.
                settings.RdpDomain.StringValue = ".";
            }
        }

        public async Task<bool> IsGrantedPermissionToGenerateCredentials(InstanceLocator instance)
        {
            using (var windowsCredentialAdapter = this.serviceProvider.GetService<IWindowsCredentialAdapter>())
            {
                return await windowsCredentialAdapter
                    .IsGrantedPermissionToCreateWindowsCredentialsAsync(instance)
                    .ConfigureAwait(false);
            }
        }
    }
}
