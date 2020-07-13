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
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ConnectionSettings;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Workflows
{
    public interface ICredentialsService
    {
        Task GenerateCredentialsAsync(
            IWin32Window owner,
            InstanceLocator instanceRef,
            ConnectionSettingsEditor settings,
            string suggestedUsername = null);

        Task ShowCredentialsPromptAsync(
           IWin32Window owner,
           InstanceLocator instanceLocator,
           ConnectionSettingsEditor settings,
           bool allowJumpToSettings);
    }

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
            ConnectionSettingsEditor settings,
            string suggestedUsername = null)
        {
            // Prompt for username to use.
            var username = this.serviceProvider
                .GetService<IGenerateCredentialsDialog>()
                .PromptForUsername(
                    owner,
                    suggestedUsername ?? this.serviceProvider
                        .GetService<IAuthorizationAdapter>()
                        .Authorization
                        .SuggestWindowsUsername());
            if (username == null)
            {
                // Aborted.
                throw new OperationCanceledException();
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

            this.serviceProvider.GetService<IShowCredentialsDialog>().ShowDialog(
                owner,
                credentials.UserName,
                credentials.Password);

            // Save credentials.
            settings.Username = credentials.UserName;
            settings.CleartextPassword = credentials.Password;
            settings.Domain = null;
            settings.SaveChanges();
        }

        public async Task ShowCredentialsPromptAsync(
            IWin32Window owner,
            InstanceLocator instanceLocator,
            ConnectionSettingsEditor settings,
            bool allowJumpToSettings)
        {
            //
            // Determine which options to show in prompt.
            //
            var credentialsExist =
                !string.IsNullOrEmpty(settings.Username) &&
                settings.Password != null &&
                settings.Password.Length != 0;

            var options = new List<CredentialOption>();
            if ((!credentialsExist && settings.CredentialGenerationBehavior == RdpCredentialGenerationBehavior.Prompt) ||
                settings.CredentialGenerationBehavior == RdpCredentialGenerationBehavior.Always)
            {
                // TODO: check for permission

                options.Add(
                    new CredentialOption()
                    {
                        Title = "Generate new credentials",
                        Apply = () => GenerateCredentialsAsync(
                            owner,
                            instanceLocator,
                            settings)
                    });
            }

            if (!credentialsExist && allowJumpToSettings)
            {
                options.Add(
                    new CredentialOption()
                    {
                        Title = "Configure credentials",
                        Apply = () =>
                        {
                            // Configure credentials -> jump to settings.
                            this.serviceProvider.GetService<IConnectionSettingsWindow>().ShowWindow();
                            return Task.FromException(new OperationCanceledException());
                        }
                    });
            }

            options.Add(
                new CredentialOption()
                {
                    Title = "Connect without configuring credentials",
                    Apply = () => Task.CompletedTask
                });

            //
            // Prompt.
            //
            CredentialOption selectedOption;
            if (options.Count > 1)
            {
                // NB. The sequence of options determines the behavior of 
                // Enter/ESC and OK/Cancel:
                //
                //  Enter/OK   -> first option.
                //  ESC/Cancel -> last option.
                var optionIndex = this.serviceProvider.GetService<ITaskDialog>().ShowOptionsTaskDialog(
                    owner,
                    UnsafeNativeMethods.TD_INFORMATION_ICON,
                    "Credentials",
                    $"You do not have any saved credentials for {instanceLocator.Name}",
                    "How do you want to proceed?",
                    null,
                    options.Select(o => o.Title).ToList(),
                    null,   //"Do not show this prompt again",
                    out bool donotAskAgain);
                selectedOption = options[optionIndex];
            }
            else
            {
                // Do not prompt if there is only one option.
                selectedOption = options[0];
            }

            // 
            // Apply.
            //
            await selectedOption
                .Apply()
                .ConfigureAwait(true);
        }


        private struct CredentialOption
        {
            public string Title;
            public Func<Task> Apply;
        }
    }
}
