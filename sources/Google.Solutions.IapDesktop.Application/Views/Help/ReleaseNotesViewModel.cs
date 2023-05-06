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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Views.Help
{
    public class ReleaseNotesViewModel : ViewModelBase
    {
        private const ushort MaxReleases = 10;
        private readonly IGithubAdapter githubAdapter;

        private async Task LoadAsync()
        {
            //
            // Create a summary of all releases.
            //
            var summary = new StringBuilder();

            try
            {
                var releases = await githubAdapter
                    .ListReleases(MaxReleases, CancellationToken.None)
                    .ConfigureAwait(true);

                foreach (var release in releases
                    .EnsureNotNull()
                    .Where(r => this.PreviousVersion == null || this.PreviousVersion < r.TagVersion)
                    .OrderByDescending(r => r.TagVersion))
                {
                    summary.AppendLine();
                    summary.AppendFormat("## Release {0}", release.TagVersion);
                    summary.AppendLine();
                    summary.AppendLine();
                    summary.Append(release.Description);
                    summary.AppendLine();
                    summary.AppendFormat("[Details]({0})", release.DetailsUrl);
                    summary.AppendLine();
                }

            }
            catch (Exception e)
            {
                summary.AppendLine("Loading release notes failed: " + e.Message);
            }

            this.Summary.Value = summary.ToString();
        }

        public ReleaseNotesViewModel(IGithubAdapter githubAdapter)
        {
            this.githubAdapter = githubAdapter.ExpectNotNull(nameof(githubAdapter));

            this.Summary = ObservableProperty.Build("Loading...");
            this.RefreshCommand = ObservableCommand.Build(
                "Refresh",
                LoadAsync);
        }

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Last version known/installed. Only newer versions that this
        /// will be shown.
        /// </summary>
        public Version PreviousVersion { get; set; }

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
