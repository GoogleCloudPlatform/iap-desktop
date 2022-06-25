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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Google.Solutions.CloudIap;
using Google.Solutions.Common.Interop;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.IapDesktop.Interop;
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
        private readonly Profile profile;

        // NB. This list is only access from the UI thread, so no locking required.
        private readonly LinkedList<BackgroundJob> backgroundJobs
            = new LinkedList<BackgroundJob>();

        private string windowTitle = Globals.FriendlyName;
        private bool isBackgroundJobStatusVisible = false;
        private string profileState = null;
        private string deviceState = null;
        private bool isDeviceStateVisible = false;
        private bool isReportInternalIssueVisible;

        public MainFormViewModel(
            Control view,
            DockPanelColorPalette colorPalette,
            Profile profile,
            ApplicationSettingsRepository applicationSettings,
            AuthSettingsRepository authSettings)
        {
            this.View = view;
            this.colorPalette = colorPalette;
            
            this.profile = profile
                .ThrowIfNull(nameof(profile));
            this.applicationSettings = applicationSettings
                .ThrowIfNull(nameof(applicationSettings));
            this.authSettings = authSettings
                .ThrowIfNull(nameof(authSettings));
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

        public string ProfileStateCaption
        {
            get => this.profileState;
            set
            {
                this.profileState = value;
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

        public IEnumerable<string> AlternativeProfileNames
        {
            get => Profile
                .ListProfiles()
                .Where(name => name != this.profile.Name);
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
            Debug.Assert(this.Authorization == null);

            //
            // Determine enrollment state of this device.
            //
            var deviceEnrollment = SecureConnectEnrollment.GetEnrollmentAsync(
                new CertificateStoreAdapter(),
                new ChromePolicy(),
                this.applicationSettings).Result;

            //
            // Get the user authorization, either by using stored
            // credentials or by initiating an OAuth authorization flow.
            //
            this.Authorization = AuthorizeDialog.Authorize(
                (Control)this.View,
                OAuthClient.Secrets,
                new[] { IapTunnelingEndpoint.RequiredScope },
                deviceEnrollment,
                this.authSettings);
            if (this.Authorization == null)
            {
                // Aborted.
                return;
            }

            this.ProfileStateCaption = $"{this.profile.Name}: {this.Authorization.Email}";
            this.DeviceStateCaption = "Endpoint Verification";
            this.IsDeviceStateVisible = this.Authorization.DeviceEnrollment.State != DeviceEnrollmentState.Disabled;
            this.IsReportInternalIssueVisible = this.Authorization.UserInfo?.HostedDomain == "google.com";

            Debug.Assert(this.ProfileStateCaption != null);
            Debug.Assert(this.Authorization.DeviceEnrollment != null);

            if (!this.profile.IsDefault)
            {
                //
                // Add taskbar badge to help distinguish this profile
                // from other profiles.
                //
                using (var badge = BadgeIcon.ForTextInitial(this.profile.Name))
                using (var taskbar = ComReference.For((ITaskbarList3)new TaskbarList()))
                {
                    taskbar.Object.HrInit();
                    taskbar.Object.SetOverlayIcon(
                        this.View.Handle,
                        badge.Handle,
                        string.Empty);
                }
            }
        }

        public async Task ReauthorizeAsync(CancellationToken token)
        {
            Debug.Assert(this.Authorization != null);
            Debug.Assert(this.Authorization.DeviceEnrollment != null);

            // Reauthorize, this might cause another OAuth code flow.
            await this.Authorization.ReauthorizeAsync(token)
                .ConfigureAwait(true);

            this.ProfileStateCaption = this.Authorization.Email;
        }

        public Task RevokeAuthorizationAsync()
        {
            Debug.Assert(this.Authorization != null);
            Debug.Assert(this.Authorization.DeviceEnrollment != null);

            return this.Authorization.RevokeAsync();
        }

        public bool IsAuthorized =>
            this.Authorization != null &&
            this.Authorization.DeviceEnrollment != null;

        //---------------------------------------------------------------------
        // Other actions.
        //---------------------------------------------------------------------

        public void SwitchToDocument(string title)
        {
            //
            // Update window title so that it shows the current document.
            //
            var newTitle = title == null
                ? Globals.FriendlyName
                : $"{title} - {Globals.FriendlyName}";

            if (!this.profile.IsDefault)
            {
                newTitle += $" ({this.profile.Name})";
            }

            this.WindowTitle = newTitle;
        }

        public void LaunchInstanceWithProfile(string profileName)
        {
            //
            // Launch a new instance, passing the specified profile
            // as parameter (unless it's the default profile).
            // 
            Program.LaunchNewInstance(new CommandLineOptions()
            {
                Profile = profileName != Profile.DefaultProfileName
                    ? profileName
                    : null
            });
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
