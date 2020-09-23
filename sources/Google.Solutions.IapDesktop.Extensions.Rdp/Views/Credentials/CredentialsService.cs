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
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Settings;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Views.Credentials
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
            // Prompt for username to use.
            string username;
            if (silent)
            {
                username = string.IsNullOrEmpty(settings.Username.StringValue)
                    ? this.serviceProvider
                        .GetService<IAuthorizationAdapter>()
                        .Authorization
                        .SuggestWindowsUsername()
                    : settings.Username.StringValue;
            }
            else
            {
                username = this.serviceProvider
                    .GetService<IGenerateCredentialsDialog>()
                    .PromptForUsername(
                        owner,
                        string.IsNullOrEmpty(settings.Username.StringValue)
                            ? this.serviceProvider
                                .GetService<IAuthorizationAdapter>()
                                .Authorization
                                .SuggestWindowsUsername()
                            : settings.Username.StringValue);
                if (username == null)
                {
                    // Aborted.
                    throw new OperationCanceledException();
                }
            }

            var credentials = await this.serviceProvider.GetService<IJobService>().RunInBackground(
                new JobDescription("Generating Windows logon credentials..."),
                token => this.serviceProvider
                    .GetService<IComputeEngineAdapter>()
                    .ResetWindowsUserAsync(
                        instanceLocator,
                        username,
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
            settings.Username.StringValue = credentials.UserName;
            settings.Password.ClearTextValue = credentials.Password;
            settings.Domain.StringValue = null;
            
            // TODO: validate!!  settings.SaveChanges();
        }

        public Task<bool> IsGrantedPermissionToGenerateCredentials(InstanceLocator instance)
            => this.serviceProvider
                .GetService<IComputeEngineAdapter>()
                .IsGrantedPermissionToResetWindowsUser(instance);
    }
}
