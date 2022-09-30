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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services
{
    public interface IUpdateService
    {
        Version InstalledVersion { get; }

        /// <summary>
        /// Check if an update check should be performed.
        /// </summary>
        bool IsUpdateCheckDue(DateTime lastCheck);

        /// <summary>
        /// Check for updates and prompt the user to update.
        /// </summary>
        void CheckForUpdates(
            IWin32Window parent, 
            out bool donotCheckForUpdatesAgain);
    }

    public class UpdateService : IUpdateService
    {
        /// <summary>
        /// Determines how often update checks are performed. 
        /// A higher number implies a slower pace of updates.
        /// </summary>
        public const int DaysBetweenUpdateChecks = 7;

        private readonly IGithubAdapter githubAdapter;
        private readonly ITaskDialog taskDialog;
        private readonly IClock clock;

        public Version InstalledVersion => typeof(UpdateService).Assembly.GetName().Version;

        private TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        public UpdateService(IGithubAdapter githubAdapter, ITaskDialog taskDialog, IClock clock)
        {
            this.githubAdapter = githubAdapter.ThrowIfNull(nameof(githubAdapter));
            this.taskDialog = taskDialog.ThrowIfNull(nameof(taskDialog));
            this.clock = clock.ThrowIfNull(nameof(clock));
        }

        public bool IsUpdateCheckDue(DateTime lastCheck)
        {
            return (this.clock.UtcNow - lastCheck).TotalDays >= DaysBetweenUpdateChecks;
        }

        public void CheckForUpdates(IWin32Window parent, out bool donotCheckForUpdatesAgain)
        {
            donotCheckForUpdatesAgain = false;
            using (var cts = new CancellationTokenSource())
            {
                //
                // Check for updates. This check must be performed synchronously,
                // otherwise this methis returns and the application exits.
                // In order not to block everything for too long in case of a network
                // problem, use a timeout.
                //
                cts.CancelAfter(this.Timeout);

                var latestRelease = this.githubAdapter.FindLatestReleaseAsync(cts.Token).Result;
                if (latestRelease == null ||
                    latestRelease.TagVersion.CompareTo(this.InstalledVersion) <= 0)
                {
                    // Installed version is up to date.
                    return;
                }

                try
                {
                    // Prompt for upgrade.
                    int selectedOption = this.taskDialog.ShowOptionsTaskDialog(
                        parent,
                        TaskDialogIcons.TD_SHIELD_ICON_INFO_BACKGROUND,
                        "Update available",
                        "An update is available for IAP Desktop",
                        "Would you like to download the update now?",
                        $"Installed version: {this.InstalledVersion}\nAvailable version: {latestRelease.TagVersion}",
                        new[]
                        {
                            "Yes, download now",     // Same as pressing 'OK'
                            "More information",
                            "No, download later"
                        },
                        "Do not check for updates again",
                        out donotCheckForUpdatesAgain);

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
