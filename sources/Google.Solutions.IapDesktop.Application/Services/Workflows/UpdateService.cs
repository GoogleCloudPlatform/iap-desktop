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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Workflows
{
    public interface IUpdateService
    {
        Version InstalledVersion { get; }
        void CheckForUpdates(IWin32Window parent, TimeSpan timeout, out bool donotCheckForUpdatesAgain);
    }

    public class UpdateService : IUpdateService
    {
        private readonly GithubAdapter githubAdapter;
        private readonly ITaskDialog taskDialog;

        public Version InstalledVersion => typeof(UpdateService).Assembly.GetName().Version;

        public UpdateService(IServiceProvider serviceProvider)
        {
            this.githubAdapter = serviceProvider.GetService<GithubAdapter>();
            this.taskDialog = serviceProvider.GetService<ITaskDialog>();
        }

        /// <summary>
        /// Check for updates and prompt the user to update.
        /// </summary>
        public void CheckForUpdates(IWin32Window parent, TimeSpan timeout, out bool donotCheckForUpdatesAgain)
        {
            donotCheckForUpdatesAgain = false;
            using (var cts = new CancellationTokenSource())
            {
                // Check for updates. This check must be performed synchronously,
                // otherwise this methis returns and the application exits.
                // In order not to block everything for too long in case of a network
                // problem, use a timeout.
                cts.CancelAfter(timeout);

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
                        UnsafeNativeMethods.TD_SHIELD_ICON_INFO_BACKGROUND,
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
                        if (selectedOption == 0 && latestRelease.Assets.Any())
                        {
                            launchBrowser.StartInfo.FileName = latestRelease.Assets.First().DownloadUrl;
                        }
                        else
                        {
                            launchBrowser.StartInfo.FileName = latestRelease.HtmlUrl;
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
