//
// Copyright 2023 Google LLC
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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Platform.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.ToolWindows.Update
{
    public class CheckForUpdateCommand<TContext> : MenuCommandBase<TContext>
    {
        private readonly IWin32Window parentWindow;
        private readonly IInstall install;
        private readonly IUpdatePolicy updatePolicy;
        private readonly IReleaseFeed feed;
        private readonly ITaskDialog taskDialog;
        private readonly IBrowser browser;

        /// <summary>
        /// Get or set whether to show survey notifications. Updated
        /// to reflect the user's opt-out decision after the command
        /// has been executed.
        /// </summary>
        public bool EnableSurveys { get; set; } = false;

        /// <summary>
        /// Last release version for which the user has taken a survey.
        /// </summary>
        public Version LastSurveyVersion { get; set; }

        protected ReleaseFeedOptions FeedOptions
        {
            get => this.updatePolicy.FollowedTrack == ReleaseTrack.Canary
                ? ReleaseFeedOptions.IncludeCanaryReleases
                : ReleaseFeedOptions.None;
        }

        protected Version InstalledVersion
        {
            get => this.install.CurrentVersion;
        }

        protected virtual bool IsUpdateAdvised(IRelease release)
        {
            return this.updatePolicy.IsUpdateAdvised(release);
        }

        public CheckForUpdateCommand(
            IWin32Window parentWindow,
            IInstall install,
            IUpdatePolicyFactory updatePolicyFactory,
            IReleaseFeed feed,
            ITaskDialog taskDialog,
            IBrowser browser)
            : base("Check for &updates")
        {
            this.parentWindow = parentWindow.ExpectNotNull(nameof(parentWindow));
            this.install = install.ExpectNotNull(nameof(install));
            this.feed = feed.ExpectNotNull(nameof(feed));
            this.taskDialog = taskDialog.ExpectNotNull(nameof(taskDialog));
            this.browser = browser.ExpectNotNull(nameof(browser));

            this.updatePolicy = updatePolicyFactory
                .ExpectNotNull(nameof(updatePolicyFactory))
                .GetPolicy();
        }

        public bool IsAutomatedCheckDue(DateTime lastCheck)
        {
            return this.updatePolicy.IsUpdateCheckDue(lastCheck);
        }

        internal void PromptForAction(IRelease latestRelease)
        {
            if (latestRelease == null)
            {
                return;
            }
            else if (IsUpdateAdvised(latestRelease))
            {
                var nameOfUpdate = this.updatePolicy.GetReleaseTrack(latestRelease) switch
                { 
                    ReleaseTrack.Canary => "An optional update",
                    ReleaseTrack.Critical => "A critical security update",
                    _ => "An update",
                };

                //
                // Prompt for upgrade.
                //
                var dialogParameters = new TaskDialogParameters()
                {
                    Icon = TaskDialogIcon.ShieldGreenBackground,
                    Caption = "Update available",
                    Heading = $"{nameOfUpdate} is available for IAP Desktop.\n\n" +
                        $"Installed version: {this.install.CurrentVersion}\n" +
                        $"Available version: {latestRelease.TagVersion}",
                    Text = "Would you like to download the update now?"
                };
                dialogParameters.Buttons.Add(TaskDialogStandardButton.Cancel);

                //
                // Download release.
                //
                var downloadButton = new TaskDialogCommandLinkButton(
                    "Yes, download now",
                    DialogResult.OK);
                downloadButton.Click += (_, __) => this.browser.Navigate(
                    latestRelease.DownloadUrl ??
                    latestRelease.DetailsUrl);
                dialogParameters.Buttons.Add(downloadButton);

                //
                // Show release notes.
                //
                var showReleaseNotes = new TaskDialogCommandLinkButton(
                    "Show release notes",
                    DialogResult.OK);
                showReleaseNotes.Click += (_, __) => this.browser.Navigate(
                    latestRelease.DetailsUrl);
                dialogParameters.Buttons.Add(showReleaseNotes);

                //
                // No, later.
                //
                var laterButton = new TaskDialogCommandLinkButton(
                    "No, download later",
                    DialogResult.Cancel);
                dialogParameters.Buttons.Add(laterButton);

                this.taskDialog.ShowDialog(
                    this.parentWindow,
                    dialogParameters);
            }
            else if (
                latestRelease.Survey != null && 
                this.EnableSurveys &&
                (this.LastSurveyVersion == null || this.LastSurveyVersion < latestRelease.TagVersion))
            {
                var dialogParameters = new TaskDialogParameters()
                {
                    Caption = "Tell us what you think",
                    Heading = latestRelease.Survey.Title,
                    Text = latestRelease.Survey.Description,
                };
                dialogParameters.Buttons.Add(TaskDialogStandardButton.Cancel);

                //
                // Open survey.
                //
                var openButton = new TaskDialogCommandLinkButton(
                    "Start survey",
                    DialogResult.OK);
                openButton.Click += (_, __) =>
                {
                    this.browser.Navigate(latestRelease.Survey.Url);
                    this.LastSurveyVersion = latestRelease.TagVersion;
                };
                dialogParameters.Buttons.Add(openButton);

                //
                // No, later.
                //
                var laterButton = new TaskDialogCommandLinkButton(
                    "Maybe later",
                    DialogResult.Cancel);
                dialogParameters.Buttons.Add(laterButton);

                //
                // Opt-out.
                //
                dialogParameters.VerificationCheckBox = new TaskDialogVerificationCheckBox(
                    "Don't show this message again")
                {
                    Checked = false
                };

                this.taskDialog.ShowDialog(
                    this.parentWindow,
                    dialogParameters);

                this.EnableSurveys = !dialogParameters.VerificationCheckBox.Checked;
            }
        }

        public void Execute(TContext context, CancellationToken cancellationToken)
        {
            var latestRelease = this.feed
                .FindLatestReleaseAsync(
                    this.FeedOptions,
                    cancellationToken)

#pragma warning disable VSTHRD002
                .Result;
#pragma warning restore VSTHRD002

            PromptForAction(latestRelease);
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override bool IsAvailable(TContext context)
        {
            return true;
        }

        protected override bool IsEnabled(TContext context)
        {
            return true;
        }

        public override void Execute(TContext context)
        {
            Execute(context, CancellationToken.None);
        }

        public override async Task ExecuteAsync(TContext context)
        {
            var latestRelease = await this.feed
               .FindLatestReleaseAsync(
                    this.FeedOptions, 
                    CancellationToken.None)
               .ConfigureAwait(true);

            PromptForAction(latestRelease);
        }
    }
}
