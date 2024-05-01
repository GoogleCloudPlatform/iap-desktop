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

using Google.Solutions.Apis;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials
{
    public interface ISelectCredentialsDialog // TODO: remove
    {
        Task SelectCredentialsAsync(
           IWin32Window? owner,
           InstanceLocator instanceLocator,
           Settings.ConnectionSettings settings,
           RdpCredentialGenerationBehavior allowedBehavior,
           bool allowJumpToSettings);
    }

    [Service(typeof(ISelectCredentialsDialog))]
    public class SelectCredentialsDialog : ISelectCredentialsDialog
    {
        private readonly IServiceProvider serviceProvider;

        public SelectCredentialsDialog(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        private async Task<bool> IsGrantedPermissionToGenerateCredentials(
            ICreateCredentialsWorkflow credentialsService,
            InstanceLocator instanceLocator)
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
                return await credentialsService
                    .IsGrantedPermissionToGenerateCredentials(instanceLocator)
                    .ConfigureAwait(true);
            }
            catch (Exception e) when (e.IsReauthError())
            {
                return true;
            }
        }

        public async Task SelectCredentialsAsync(
            IWin32Window? owner,
            InstanceLocator instanceLocator,
            Settings.ConnectionSettings settings,
            RdpCredentialGenerationBehavior allowedBehavior,
            bool allowJumpToSettings)
        {
            var credentialsService = this.serviceProvider.GetService<ICreateCredentialsWorkflow>();

            //
            // Determine which options to show in prompt.
            //
            var credentialsExist =
                !string.IsNullOrEmpty(settings.RdpUsername.Value) &&
                !string.IsNullOrEmpty(settings.RdpPassword.GetClearTextValue());

            if (allowedBehavior == RdpCredentialGenerationBehavior.Force
                && await IsGrantedPermissionToGenerateCredentials(
                            credentialsService,
                            instanceLocator)
                        .ConfigureAwait(true))
            {
                // Generate new credentials right away and skip the prompt.
                await credentialsService.CreateCredentialsAsync(
                        owner,
                        instanceLocator,
                        settings,
                        true)
                    .ConfigureAwait(true);
                return;
            }

            var options = new List<CredentialOption>();
            if ((!credentialsExist
                    && allowedBehavior == RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound
                    && await IsGrantedPermissionToGenerateCredentials(
                                credentialsService,
                                instanceLocator)
                            .ConfigureAwait(true))
                || allowedBehavior == RdpCredentialGenerationBehavior.Allow)
            {
                options.Add(
                    new CredentialOption()
                    {
                        Title = "Generate new credentials",
                        Apply = () => credentialsService
                            .CreateCredentialsAsync(
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
                                .GetService<IConfigureCredentialsWorkflow>()
                                .ShowCredentialsDialog();

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
                var optionIndex = this.serviceProvider.GetService<ILegacyTaskDialog>().ShowOptionsTaskDialog(
                    owner,
                    TaskDialogIcons.TD_INFORMATION_ICON,
                    "Credentials",
                    $"You do not have any saved credentials for {instanceLocator.Name}",
                    "How do you want to proceed?",
                    null,
                    options.Select(o => o.Title).ToList(),
                    null,   //"Do not show this prompt again",
                    out var donotAskAgain);
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
