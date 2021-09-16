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
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.IapTunneling.Iap;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Windows
{
    internal class MainFormViewModel : ViewModelBase
    {
        private readonly DockPanelColorPalette colorPalette;
        private readonly AuthSettingsRepository authSettings;
        private readonly ApplicationSettingsRepository applicationSettings;

        // NB. This list is only access from the UI thread, so no locking required.
        private readonly LinkedList<BackgroundJob> backgroundJobs
            = new LinkedList<BackgroundJob>();

        private string windowTitle = Globals.FriendlyName;
        private bool isBackgroundJobStatusVisible = false;
        private string signInState = null;
        private string deviceState = null;
        private bool isDeviceStateVisible = false;
        private bool isReportInternalIssueVisible;

        public MainFormViewModel(
            Control view,
            DockPanelColorPalette colorPalette,
            ApplicationSettingsRepository applicationSettings,
            AuthSettingsRepository authSettings)
        {
            this.View = view;
            this.colorPalette = colorPalette;

            this.applicationSettings = applicationSettings;
            this.authSettings = authSettings;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public string WindowTitle
        {
            get => this.windowTitle;
            private set
            {
                this.windowTitle = value;
                RaisePropertyChange();
            }
        }

        public bool IsLoggingEnabled
        {
            get => Program.IsLoggingEnabled;
            set
            {
                Program.IsLoggingEnabled = value;
                RaisePropertyChange();
                RaisePropertyChange((MainFormViewModel m) => m.StatusBarBackColor);
                RaisePropertyChange((MainFormViewModel m) => m.StatusText);
            }
        }

        public Color StatusBarBackColor
        {
            get => this.IsLoggingEnabled
                ? Color.Red
                : this.colorPalette.ToolWindowCaptionActive.Background;
        }

        public string StatusText
        {
            get => this.IsLoggingEnabled
                ? $"Logging to {Program.LogFile}, performance might be degraded while logging is enabled."
                : string.Empty;
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

        public string SignInStateCaption
        {
            get => this.signInState;
            set
            {
                this.signInState = value;
                RaisePropertyChange();
            }
        }

        public string DeviceStateCaption
        {
            get => this.deviceState;
            set
            {
                this.deviceState = value;
                RaisePropertyChange();
            }
        }

        public bool IsDeviceStateVisible
        {
            get => this.isDeviceStateVisible;
            set
            {
                this.isDeviceStateVisible = value;
                RaisePropertyChange();
            }
        }

        public bool IsReportInternalIssueVisible
        {
            get => this.isReportInternalIssueVisible;
            set
            {
                this.isReportInternalIssueVisible = value;
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
        public IDeviceEnrollment DeviceEnrollment { get; private set; }

        public void Authorize()
        {
            Debug.Assert(this.Authorization == null);
            Debug.Assert(this.DeviceEnrollment == null);

            //
            // Get the user authorization, either by using stored
            // credentials or by initiating an OAuth authorization flow.
            //
            this.Authorization = AuthorizeDialog.Authorize(
                (Control)this.View,
                OAuthClient.Secrets,
                new[] { IapTunnelingEndpoint.RequiredScope },
                this.authSettings);
            if (this.Authorization == null)
            {
                // Aborted.
                return;
            }

            //
            // Determine enrollment state of this device.
            //
            this.DeviceEnrollment = SecureConnectEnrollment.GetEnrollmentAsync(
                new CertificateStoreAdapter(),
                new ChromePolicy(),
                this.applicationSettings,
                this.Authorization.UserInfo.Subject).Result;

            this.SignInStateCaption = this.Authorization.Email;
            this.DeviceStateCaption = "Endpoint Verification";
            this.IsDeviceStateVisible = this.DeviceEnrollment.State != DeviceEnrollmentState.Disabled;
            this.IsReportInternalIssueVisible = this.Authorization.UserInfo?.HostedDomain == "google.com";

            Debug.Assert(this.SignInStateCaption != null);
            Debug.Assert(this.DeviceEnrollment != null);
        }

        public async Task ReauthorizeAsync(CancellationToken token)
        {
            Debug.Assert(this.Authorization != null);
            Debug.Assert(this.DeviceEnrollment != null);

            // Reauthorize, this might cause another OAuth code flow.
            await this.Authorization.ReauthorizeAsync(token)
                .ConfigureAwait(true);

            // Refresh enrollment info as the user might have switched identities.
            await this.DeviceEnrollment
                .RefreshAsync(this.Authorization.UserInfo.Subject)
                .ConfigureAwait(true);

            this.SignInStateCaption = this.Authorization.Email;
        }

        public Task RevokeAuthorizationAsync()
        {
            Debug.Assert(this.Authorization != null);
            Debug.Assert(this.DeviceEnrollment != null);

            return this.Authorization.RevokeAsync();
        }

        public bool IsAuthorized =>
            this.Authorization != null &&
            this.DeviceEnrollment != null;

        //---------------------------------------------------------------------
        // Other actions.
        //---------------------------------------------------------------------

        public void SwitchToDocument(string title)
        {
            //
            // Update window title so that it shows the current document.
            //
            this.WindowTitle = title == null
                ? Globals.FriendlyName
                : $"{title} - {Globals.FriendlyName}";
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
