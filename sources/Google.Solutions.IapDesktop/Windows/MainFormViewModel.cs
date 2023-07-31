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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Windows
{
    internal class MainFormViewModel : ViewModelBase
    {
        private readonly IThemeService themeService;
        private readonly IInstall install;
        private readonly UserProfile profile;
        private readonly IAuthorization authorization;

        // NB. This list is only access from the UI thread, so no locking required.
        private readonly LinkedList<BackgroundJob> backgroundJobs
            = new LinkedList<BackgroundJob>();

        private string windowTitle = Install.FriendlyName;
        private bool isBackgroundJobStatusVisible = false;
        private string profileState = null;
        private string deviceState = null;
        private bool isDeviceStateVisible = false;
        private bool isReportInternalIssueVisible;

        public MainFormViewModel(
            Control view,
            IInstall install,
            UserProfile profile,
            IAuthorization authorization,
            IThemeService themeService)
        {
            this.View = view.ExpectNotNull(nameof(view));
            this.install = install.ExpectNotNull(nameof(install));
            this.profile = profile.ExpectNotNull(nameof(profile));
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.themeService = themeService.ExpectNotNull(nameof(themeService));

            this.ProfileStateCaption = $"{this.profile.Name}: {this.authorization.Email}";
            this.DeviceStateCaption = "Endpoint Verification";
            this.IsDeviceStateVisible = this.authorization.DeviceEnrollment.State != DeviceEnrollmentState.Disabled;
            this.IsReportInternalIssueVisible = this.authorization.UserInfo?.HostedDomain == "google.com";

            this.authorization.Reauthorized += (_, __) =>
            {
                //
                // The email address might have changed after reauth.
                //
                // NB. The event might be fired on a worker thread.
                //
                view.BeginInvoke((Action)(() => this.ProfileStateCaption = authorization.Email));
            };
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
                RaisePropertyChange((MainFormViewModel m) => m.StatusText);
            }
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
                var count = this.backgroundJobs.Count;
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
            get => UserProfile
                .ListProfiles(this.install)
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

        public Task RevokeAuthorizationAsync() // TODO: Rename to Terminate
        {
            Debug.Assert(this.authorization != null);
            Debug.Assert(this.authorization.DeviceEnrollment != null);

            return this.authorization.RevokeAsync();
        }

        //---------------------------------------------------------------------
        // Other actions.
        //---------------------------------------------------------------------

        public void SwitchToDocument(string title)
        {
            //
            // Update window title so that it shows the current document.
            //
            var newTitle = title == null
                ? Install.FriendlyName
                : $"{title} - {Install.FriendlyName}";

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
                Profile = profileName != UserProfile.DefaultProfileName
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
