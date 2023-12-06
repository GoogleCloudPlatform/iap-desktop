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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Client
{
    public class GithubClient : IReleaseFeed
    {
        public const string RepositoryName = "GoogleCloudPlatform/iap-desktop";

        private readonly IExternalRestClient restAdapter;

        public GithubClient(IExternalRestClient restAdapter)
        {
            this.restAdapter = restAdapter.ExpectNotNull(nameof(restAdapter));
        }

        public async Task<IEnumerable<IRelease>> ListReleasesAsync(
            ushort maxCount,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var releases = await this.restAdapter
                    .GetAsync<List<Release>>(
                        new Uri($"https://api.github.com/repos/{RepositoryName}/releases?per_page={maxCount}"),
                        cancellationToken)
                    .ConfigureAwait(false);

                return releases.EnsureNotNull();
            }
        }

        public async Task<IRelease> FindLatestReleaseAsync(
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var latestRelease = await this.restAdapter
                    .GetAsync<Release>(
                        new Uri($"https://api.github.com/repos/{RepositoryName}/releases/latest"),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (latestRelease == null)
                {
                    return null;
                }
                else
                {
                    ApplicationTraceSource.Log.TraceVerbose(
                        "Found new release: {0}", latestRelease.TagName);

                    //
                    // New release available.
                    //
                    return latestRelease;
                }
            }
        }

        //---------------------------------------------------------------------
        // Classes for deserialization.
        //---------------------------------------------------------------------

        public class Release : IRelease
        {
            [JsonProperty("tag_name")]
            public string TagName { get; }

            [JsonProperty("html_url")]
            public string HtmlUrl { get; }

            [JsonProperty("body")]
            public string Body { get; }

            [JsonProperty("assets")]
            public List<ReleaseAsset> Assets { get; }

            [JsonConstructor]
            public Release(
                [JsonProperty("tag_name")] string tagName,
                [JsonProperty("html_url")] string htmlUrl,
                [JsonProperty("body")] string body,
                [JsonProperty("assets")] List<ReleaseAsset> assets)
            {
                this.TagName = tagName;
                this.HtmlUrl = htmlUrl;
                this.Body = body;
                this.Assets = assets;
            }

            public Version TagVersion
            {
                get
                {
                    if (Version.TryParse(this.TagName, out var version))
                    {
                        return version;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public string DownloadUrl => this
                .Assets
                .EnsureNotNull()
                .FirstOrDefault(u => u.DownloadUrl.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))?
                .DownloadUrl;

            public string DetailsUrl => this.HtmlUrl;
            public string Description => this.Body;
        }

        public class ReleaseAsset
        {
            [JsonProperty("browser_download_url")]
            public string DownloadUrl { get; }

            public ReleaseAsset(
                [JsonProperty("browser_download_url")] string downloadUrl)
            {
                this.DownloadUrl = downloadUrl;
            }
        }
    }
}
