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
using Google.Solutions.IapDesktop.Application.Services.Windows.ConnectionSettings;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Util;
using System;
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
    }

    public class CredentialsService : ICredentialsService
    {
        private readonly IJobService jobService;
        private readonly IEventService eventService;
        private readonly IAuthorizationAdapter authService;
        private readonly IComputeEngineAdapter computeEngineAdapter;

        public CredentialsService(IServiceProvider serviceProvider)
        {
            this.jobService = serviceProvider.GetService<IJobService>();
            this.eventService = serviceProvider.GetService<IEventService>();
            this.authService = serviceProvider.GetService<IAuthorizationAdapter>();
            this.computeEngineAdapter = serviceProvider.GetService<IComputeEngineAdapter>();
        }

        public async Task GenerateCredentialsAsync(
            IWin32Window owner,
            InstanceLocator instanceLocator,
            ConnectionSettingsEditor settings,
            string suggestedUsername = null)
        {
            // Prompt for username to use.
            var username = new GenerateCredentialsDialog().PromptForUsername(
                owner,
                suggestedUsername ?? this.authService.Authorization.SuggestWindowsUsername());
            if (username == null)
            {
                // Aborted.
                throw new OperationCanceledException();
            }

            var credentials = await this.jobService.RunInBackground(
                new JobDescription("Generating Windows logon credentials..."),
                token => this.computeEngineAdapter.ResetWindowsUserAsync(
                    instanceLocator,
                    username,
                    token))
                .ConfigureAwait(true);

            new ShowCredentialsDialog().ShowDialog(
                owner,
                credentials.UserName,
                credentials.Password);

            // Save credentials.
            settings.Username = credentials.UserName;
            settings.CleartextPassword = credentials.Password;
            settings.Domain = null;
            settings.SaveChanges();
        }
    }
}
