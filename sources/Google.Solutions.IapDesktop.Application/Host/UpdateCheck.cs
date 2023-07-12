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

using Google.Apis.Util;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Host
{
    public interface IUpdateCheck
    {
        Version InstalledVersion { get; }

        /// <summary>
        /// Check if an update check should be performed.
        /// </summary>
        bool IsUpdateCheckDue(DateTime lastCheck);

        /// <summary>
        /// Check for updates and prompt the user to update.
        /// </summary>
        void CheckForUpdates(IWin32Window parent);
    }

    public class UpdateCheck : IUpdateCheck
    {
        /// <summary>
        /// Determines how often update checks are performed. 
        /// A higher number implies a slower pace of updates.
        /// </summary>
        public const int DaysBetweenUpdateChecks = 10;

        private readonly IInstall install;
        private readonly IReleaseFeed feed;
        private readonly ILegacyTaskDialog taskDialog;
        private readonly IClock clock;

        private TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        public Version InstalledVersion => this.install.CurrentVersion;

        public UpdateCheck(
            IInstall install,
            IReleaseFeed feed,
            ILegacyTaskDialog taskDialog,
            IClock clock)
        {
            this.install = install.ExpectNotNull(nameof(install));
            this.feed = feed.ExpectNotNull(nameof(feed));
            this.taskDialog = taskDialog.ExpectNotNull(nameof(taskDialog));
            this.clock = clock.ExpectNotNull(nameof(clock));
        }

        public bool IsUpdateCheckDue(DateTime lastCheck)
        {
            return (this.clock.UtcNow - lastCheck).TotalDays >= DaysBetweenUpdateChecks;
        }

        public void CheckForUpdates(IWin32Window parent)
        {
            using (var cts = new CancellationTokenSource())
            {
                //
                // Check for updates. This check must be performed synchronously,
                // otherwise this method returns and the application exits.
                // In order not to block everything for too long in case of a network
                // problem, use a timeout.
                //
                cts.CancelAfter(this.Timeout);

                var latestRelease = this.feed.FindLatestReleaseAsync(cts.Token).Result;
                if (latestRelease == null ||
                    latestRelease.TagVersion.CompareTo(this.install.CurrentVersion) <= 0)
                {
                    // Installed version is up to date.
                    return;
                }

                try
                {
                    // Prompt for upgrade.
                    var selectedOption = this.taskDialog.ShowOptionsTaskDialog(
                        parent,
                        TaskDialogIcons.TD_SHIELD_ICON_GREEN_BACKGROUND,
                        "Update available",
                        "An update is available for IAP Desktop.\n\n" +
                            $"Installed version: {this.InstalledVersion}\nAvailable version: {latestRelease.TagVersion}",
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

                    if (selectedOption == 2)
                    {
                        // Cancel.
                        return;
                    }

                    using (var launchBrowser = new Process())
                    {
                        if (selectedOption == 0 && latestRelease.DownloadUrl != null)
                        {
                            launchBrowser.StartInfo.FileName = latestRelease.DownloadUrl;
                        }
                        else
                        {
                            launchBrowser.StartInfo.FileName = latestRelease.DetailsUrl;
                        }

                        launchBrowser.StartInfo.UseShellExecute = true;
                        launchBrowser.Start();
                    }
                }
                catch (OperationCanceledException)
                {
                    // User cancelled
                }
            }
        }
    }
}
