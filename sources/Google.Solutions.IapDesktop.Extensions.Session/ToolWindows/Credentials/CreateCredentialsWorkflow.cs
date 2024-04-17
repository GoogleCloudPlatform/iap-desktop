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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.Settings;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials
{
    public interface ICreateCredentialsWorkflow
    {
        Task CreateCredentialsAsync(
            IWin32Window owner,
            InstanceLocator instanceRef,
            Settings.ConnectionSettings settings,
            bool silent);

        Task<bool> IsGrantedPermissionToGenerateCredentials(
            InstanceLocator instanceRef);
    }

    [Service(typeof(ICreateCredentialsWorkflow))]
    public class CreateCredentialsWorkflow : ICreateCredentialsWorkflow
    {
        private readonly IServiceProvider serviceProvider;

        public CreateCredentialsWorkflow(IServiceProvider serviceProvider) // TODO: Inject Service<>
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task CreateCredentialsAsync(
            IWin32Window owner,
            InstanceLocator instanceLocator,
            Settings.ConnectionSettings settings,
            bool silent)
        {
            string username;
            if (settings.RdpUsername.Value != null && 
                WindowsUser.IsLocalUsername(settings.RdpUsername.Value))
            {
                username = settings.RdpUsername.Value;
            }
            else
            {
                username = WindowsUser.SuggestUsername(
                    this.serviceProvider.GetService<IAuthorization>().Session);
            }

            if (!silent)
            {
                //
                // Prompt user to customize the defaults.
                //

                var dialogResult = this.serviceProvider
                    .GetService<INewCredentialsDialog>()
                    .ShowDialog(owner, username);

                if (dialogResult.Result == DialogResult.OK)
                {
                    username = dialogResult.Username;
                }
                else
                {
                    throw new OperationCanceledException();
                }
            }

            var credentials = await this.serviceProvider.GetService<IJobService>().RunAsync(
                new JobDescription("Generating Windows logon credentials..."),
                token => this.serviceProvider
                    .GetService<IWindowsCredentialGenerator>()
                    .CreateWindowsCredentialsAsync(
                        instanceLocator,
                        username,
                        UserFlags.AddToAdministrators,
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
            settings.RdpUsername.Value = credentials.UserName;
            settings.RdpPassword.SetClearTextValue(credentials.Password);

            // NB. The computer might be joined to a domain, therefore force a local logon.
            settings.RdpDomain.Value = ".";
        }

        public async Task<bool> IsGrantedPermissionToGenerateCredentials(InstanceLocator instance)
        {
            return await this.serviceProvider
                .GetService<IWindowsCredentialGenerator>()
                .IsGrantedPermissionToCreateWindowsCredentialsAsync(instance)
                .ConfigureAwait(false);
        }
    }
}
