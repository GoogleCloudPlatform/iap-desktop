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

using Google.Solutions.CloudIap;
using Google.Solutions.Common.Auth;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapTunneling.Iap;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Windows
{
    internal class MainFormViewModel : ViewModelBase
    {
        internal const string FriendlyName = "IAP Desktop - Identity-Aware Proxy for Remote Desktop";

        private readonly ApplicationSettingsRepository applicationSettings;
        private readonly AuthSettingsRepository authSettings;
        private readonly AppProtocolRegistry protocolRegistry;

        // NB. This list is only access from the UI thread, so no locking required.
        private readonly LinkedList<BackgroundJob> backgroundJobs
            = new LinkedList<BackgroundJob>();

        private bool isBackgroundJobStatusVisible = false;
        private string userEmail = null;

        public MainFormViewModel(
            Control view,
            ApplicationSettingsRepository applicationSettings,
            AuthSettingsRepository authSettings,
            AppProtocolRegistry protocolRegistry)
        {
            this.View = view;
            this.applicationSettings = applicationSettings;
            this.authSettings = authSettings;
            this.protocolRegistry = protocolRegistry;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsUpdateCheckEnabled
        {
            get => this.applicationSettings.GetSettings().IsUpdateCheckEnabled.BoolValue;
            set
            {
                var settings = this.applicationSettings.GetSettings();
                settings.IsUpdateCheckEnabled.BoolValue = value;
                this.applicationSettings.SetSettings(settings);

                RaisePropertyChange();
            }
        }

        public bool IsProtocolRegistred
        {
            get => this.protocolRegistry.IsRegistered(
                IapRdpUrl.Scheme,
                GetType().Assembly.Location);
            set
            {
                if (value)
                {
                    this.protocolRegistry.Register(
                        IapRdpUrl.Scheme,
                        FriendlyName,
                        GetType().Assembly.Location);
                }
                else
                {
                    this.protocolRegistry.Unregister(IapRdpUrl.Scheme);
                }

                RaisePropertyChange();
            }
        }

        public bool IsBackgroundJobStatusVisible
        {
            get => this.isBackgroundJobStatusVisible;
            private set
            {
                this.isBackgroundJobStatusVisible = value;
                RaisePropertyChange();
                RaisePropertyChange((MainFormViewModel m) => m.BackgroundJobStatus);
            }
        }

        public string BackgroundJobStatus
        {
            get
            {
                var count = this.backgroundJobs.Count();
                if (count == 0)
                {
                    return null;
                }
                else if (count == 1)
                {
                    return this.backgroundJobs.First().Description.StatusMessage;
                }
                else
                {
                    return this.backgroundJobs.First().Description.StatusMessage +
                        $" (+{count - 1} more background jobs)";
                }
            }
        }

        public string UserEmail
        {
            get => this.userEmail;
            set
            {
                this.userEmail = value;
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // Background job actions.
        //---------------------------------------------------------------------

        public IJobUserFeedback CreateBackgroundJob(
            JobDescription jobDescription,
            CancellationTokenSource cancellationSource)
        {
            return new BackgroundJob(this, jobDescription, cancellationSource);
        }

        public void CancelBackgroundJobs()
        {
            // NB. Use ToList to create a snapshot of the list because Cancel() 
            // modifies the list while we are iterating it.
            foreach (var job in this.backgroundJobs.ToList())
            {
                job.Cancel();
            }
        }


        //---------------------------------------------------------------------
        // Authorization actions.
        //---------------------------------------------------------------------

        public IAuthorization Authorization { get; private set; }

        public void Authorize()
        {
            this.Authorization = AuthorizeDialog.Authorize(
                (Control)this.View,
                OAuthClient.Secrets,
                new[] { IapTunnelingEndpoint.RequiredScope },
                this.authSettings);

            this.UserEmail = this.Authorization?.Email;
        }

        public async Task ReauthorizeAsync(CancellationToken token)
        {
            await this.Authorization.ReauthorizeAsync(token)
                .ConfigureAwait(true);

            this.UserEmail = this.Authorization.Email;
        }

        public Task RevokeAuthorizationAsync()
        {
            return this.Authorization.RevokeAsync();
        }

        //---------------------------------------------------------------------
        // Helper classes.
        //---------------------------------------------------------------------

        private class BackgroundJob : IJobUserFeedback
        {
            private readonly MainFormViewModel viewModel;
            private readonly CancellationTokenSource cancellationSource;
            public JobDescription Description { get; }

            public bool IsShowing => true;

            public BackgroundJob(
                MainFormViewModel viewModel,
                JobDescription jobDescription,
                CancellationTokenSource cancellationSource)
            {
                this.viewModel = viewModel;
                this.Description = jobDescription;
                this.cancellationSource = cancellationSource;
            }

            public void Cancel()
            {
                this.cancellationSource.Cancel();
                Finish();
            }

            public void Finish()
            {
                this.viewModel.backgroundJobs.Remove(this);
                this.viewModel.IsBackgroundJobStatusVisible
                    = this.viewModel.backgroundJobs.Any();
            }

            public void Start()
            {
                this.viewModel.backgroundJobs.AddLast(this);
                this.viewModel.IsBackgroundJobStatusVisible = true;
            }
        }
    }
}
