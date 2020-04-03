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

using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services
{
    public interface IUpdateService
    {
        Version InstalledVersion { get; }
        void CheckForUpdates(IWin32Window parent, TimeSpan timeout, out bool donotCheckForUpdatesAgain);
    }

    public class UpdateService : IUpdateService
    {
        private readonly GithubAdapter githubAdapter;

        public Version InstalledVersion => typeof(UpdateService).Assembly.GetName().Version;

        public UpdateService(IServiceProvider serviceProvider)
        {
            this.githubAdapter = serviceProvider.GetService<GithubAdapter>();
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
                if (latestRelease == null)
                {
                    // No releases available, nevermind.
                    return;
                }

                if (latestRelease == null ||
                    latestRelease.TagVersion.CompareTo(this.InstalledVersion) <= 0)
                {
                    // Installed version is up to date.
                    return;
                }

                // Prompt for upgrade.
                int selectedOption = UnsafeNativeMethods.ShowOptionsTaskDialog(
                    parent,
                    "Update available",
                    "An update is available for the Cloud IAP plugin",
                    "Would you like to download the update now?",
                    $"Installed version: {this.InstalledVersion}\nAvailable version: {latestRelease.TagVersion}",
                    new[]
                    {
                        "Yes, download now",
                        "More information",     // Same as pressing 'OK'
                        "No, download later"    // Same as pressing 'Cancel'
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
        }
    }
}
