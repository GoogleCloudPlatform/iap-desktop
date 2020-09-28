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
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.ConnectionSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Views.Credentials
{
    public interface ICredentialPrompt
    {
        Task ShowCredentialsPromptAsync(
           IWin32Window owner,
           InstanceLocator instanceLocator,
           ConnectionSettingsBase settings,
           bool allowJumpToSettings);
    }

    [Service(typeof(ICredentialPrompt))]
    public class CredentialPrompt : ICredentialPrompt
    {
        private readonly IServiceProvider serviceProvider;

        public CredentialPrompt(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task ShowCredentialsPromptAsync(
            IWin32Window owner,
            InstanceLocator instanceLocator,
            ConnectionSettingsBase settings,
            bool allowJumpToSettings)
        {
            var credentialsService = this.serviceProvider.GetService<ICredentialsService>();

            //
            // Determine which options to show in prompt.
            //
            var credentialsExist =
                !string.IsNullOrEmpty(settings.Username.StringValue) &&
                !string.IsNullOrEmpty(settings.Password.ClearTextValue);

            if (settings.CredentialGenerationBehavior.EnumValue == RdpCredentialGenerationBehavior.Force
                && await credentialsService
                        .IsGrantedPermissionToGenerateCredentials(instanceLocator)
                        .ConfigureAwait(true))
            {
                // Generate new credentials right away and skip the prompt.
                await credentialsService.GenerateCredentialsAsync(
                        owner,
                        instanceLocator,
                        settings,
                        true)
                    .ConfigureAwait(true);
                return;
            }

            var options = new List<CredentialOption>();
            if ((!credentialsExist
                    && settings.CredentialGenerationBehavior.EnumValue == RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound
                    && await credentialsService
                        .IsGrantedPermissionToGenerateCredentials(instanceLocator)
                        .ConfigureAwait(true))
                || settings.CredentialGenerationBehavior.EnumValue == RdpCredentialGenerationBehavior.Allow)
            {
                options.Add(
                    new CredentialOption()
                    {
                        Title = "Generate new credentials",
                        Apply = () => credentialsService
                            .GenerateCredentialsAsync(
                                owner,
                                instanceLocator,
                                settings,
                                false)
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
                            this.serviceProvider
                                .GetService<IConnectionSettingsWindow>()
                                .ShowWindow();

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
                var optionIndex = this.serviceProvider.GetService<ITaskDialog>().ShowOptionsTaskDialog(
                    owner,
                    TaskDialogIcons.TD_INFORMATION_ICON,
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
