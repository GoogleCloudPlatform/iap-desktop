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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.Platform.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.ToolWindows.Update
{
    internal class CheckForUpdateCommand<TContext> : MenuCommandBase<TContext>
    {
        private readonly IWin32Window parentWindow;
        private readonly IInstall install;
        private readonly IUpdatePolicy updatePolicy;
        private readonly IReleaseFeed feed;
        private readonly ITaskDialog taskDialog;
        private readonly IBrowser browser;

        public CheckForUpdateCommand(
            IWin32Window parentWindow,
            IInstall install,
            IUpdatePolicyFactory updatePolicyFactory,
            IReleaseFeed feed,
            ITaskDialog taskDialog,
            IBrowser browser,
            ReleaseTrack releaseTrack)
            : base("Check for &updates")
        {
            this.parentWindow = parentWindow.ExpectNotNull(nameof(parentWindow));
            this.install = install.ExpectNotNull(nameof(install));
            this.feed = feed.ExpectNotNull(nameof(feed));
            this.taskDialog = taskDialog.ExpectNotNull(nameof(taskDialog));
            this.browser = browser.ExpectNotNull(nameof(browser));

            this.updatePolicy = updatePolicyFactory
                .ExpectNotNull(nameof(updatePolicyFactory))
                .GetPolicy(releaseTrack);
        }

        public bool IsAutomatedUpdateCheckDue(DateTime lastCheck)
        {
            return this.updatePolicy.IsUpdateCheckDue(lastCheck);
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

        public override async Task ExecuteAsync(TContext context) // TODO: Can throw OperationCanceledException!
        {
            var latestRelease = await this.feed
                .FindLatestReleaseAsync(CancellationToken.None)
                .ConfigureAwait(false);
            if (latestRelease != null && 
                this.updatePolicy.IsUpdateAdvised(latestRelease))
            {
                //
                // Prompt for upgrade.
                //
                var selectedOption = this.taskDialog.ShowOptionsTaskDialog(
                    this.parentWindow,
                    TaskDialogIcons.TD_SHIELD_ICON_GREEN_BACKGROUND,
                    "Update available",
                    "An update is available for IAP Desktop.\n\n" +
                        $"Installed version: {this.install.CurrentVersion}\n" +
                        $"Available version: {latestRelease.TagVersion}",   // TODO: Indicate track
                    "Would you like to download the update now?",
                    null,
                    new[]
                    {
                        "Yes, download now",
                        "Show release notes",
                        "No, download later"
                    },
                    null,
                    out var _);

                switch (selectedOption)
                {
                    case 0: // Download now.
                        this.browser.Navigate(latestRelease.DownloadUrl ?? latestRelease.DetailsUrl);
                        break;

                    case 1: // Show release notes
                        this.browser.Navigate(latestRelease.DetailsUrl);
                        break;

                    default: // Later/cancel.
                        break;
                }
            }
        }
    }
}
