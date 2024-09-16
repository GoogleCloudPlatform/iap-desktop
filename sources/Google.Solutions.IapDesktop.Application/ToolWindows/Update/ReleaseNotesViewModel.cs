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

using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.ToolWindows.Update
{
    public class ReleaseNotesViewModel : ViewModelBase
    {
        private const ushort MaxReleases = 10;

        private readonly IInstall install;
        private readonly IReleaseFeed feed;

        private async Task LoadAsync()
        {
            //
            // Create a summary of all releases.
            //
            var summary = new StringBuilder();

            try
            {
                var releases = await this.feed
                    .ListReleasesAsync(ReleaseFeedOptions.None, CancellationToken.None)
                    .ConfigureAwait(true);

                foreach (var release in releases
                    .EnsureNotNull()
                    .Where(r => this.install.CurrentVersion.Major == 1 || // Dev builds
                                r.TagVersion <= this.install.CurrentVersion)
                    .Where(r => this.ShowAllReleases ||
                                this.install.PreviousVersion == null ||
                                this.install.PreviousVersion < r.TagVersion)
                    .OrderByDescending(r => r.TagVersion)
                    .Take(MaxReleases))
                {
                    summary.AppendLine();
                    summary.AppendFormat("## Release {0}", release.TagVersion);
                    summary.AppendLine();
                    summary.AppendLine();
                    summary.Append(release.Description);
                    summary.AppendLine();
                    summary.AppendFormat("[Details]({0})", release.DetailsUrl);
                    summary.AppendLine();
                    summary.AppendLine();
                }

                summary.AppendLine(new string('_', 80));
                summary.AppendLine();
                summary.AppendLine();
                summary.AppendLine("If you're missing a feature, [let us know](https://github.com/GoogleCloudPlatform/iap-desktop/issues/new). ");
                summary.AppendLine("And if you like IAP Desktop, consider [giving it a star on GitHub](https://github.com/GoogleCloudPlatform/iap-desktop)!");
            }
            catch (Exception e)
            {
                summary.AppendLine("Loading release notes failed: " + e.Message);
            }

            this.Summary.Value = summary.ToString();
        }

        public ReleaseNotesViewModel(
            IInstall install,
            IReleaseFeed feed)
        {
            this.install = install.ExpectNotNull(nameof(install));
            this.feed = feed.ExpectNotNull(nameof(feed));

            this.Summary = ObservableProperty.Build("Loading...");
            this.RefreshCommand = ObservableCommand.Build(
                "Refresh",
                LoadAsync);
        }

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// When set to false, only releases newer than the previously installed
        /// versions are shown.
        /// </summary>
        public bool ShowAllReleases { get; set; } = true;

        //---------------------------------------------------------------------
        // "Output" properties.
        //---------------------------------------------------------------------

        public ObservableProperty<string> Summary { get; }

        //---------------------------------------------------------------------
        // Observable commands.
        //---------------------------------------------------------------------

        public ObservableCommand RefreshCommand { get; }
    }
}
