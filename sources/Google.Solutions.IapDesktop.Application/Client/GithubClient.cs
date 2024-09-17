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
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Client
{
    public class GithubClient : IReleaseFeed
    {
#if DEBUG
        internal const ushort PageSize = 2;
#else
        internal const ushort PageSize = 100;
#endif
        private readonly IExternalRestClient restAdapter;

        public GithubClient(
            IExternalRestClient restAdapter,
            string repositoryName)
        {
            this.restAdapter = restAdapter.ExpectNotNull(nameof(restAdapter));
            this.Repository = repositoryName.ExpectNotEmpty(nameof(repositoryName));

            Debug.Assert(repositoryName.Contains('/'));
        }

        /// <summary>
        /// Repository to source updates from, formatted as org-name/repo-name.
        /// </summary>
        public string Repository { get; }

        /// <summary>
        /// List recent releases, ordered by version number in descending order.
        /// </summary>
        public async Task<IEnumerable<IRelease>> ListReleasesAsync(
            ReleaseFeedOptions options,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                //
                // NB. The releases API seems to order results by published_at, 
                // but we shouldn't rely on that, see
                // https://github.com/orgs/community/discussions/21901.
                //
                // The API also doesn't let us specify a custom order, so we have
                // to do the ordering by ourselves.
                //

                var allReleases = new List<IRelease>();
                for (var pageNumber = 1; ; pageNumber++)
                {
                    var page = await this.restAdapter
                        .GetAsync<List<Release>>(
                            new Uri(
                                $"https://api.github.com/repos/{this.Repository}/releases?" +
                                $"per_page={PageSize}&page={pageNumber}"),
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (page == null || page.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        allReleases.AddRange(page);
                    }
                }

                return allReleases
                    .EnsureNotNull()
                    .Where(r => r.TagVersion != null)
                    .Where(r => options.HasFlag(ReleaseFeedOptions.IncludeCanaryReleases)
                                || !r.IsCanaryRelease)
                    .OrderByDescending(r => r.TagVersion);
            }
        }

        /// <summary>
        /// Find the latest available update.
        /// </summary>
        public async Task<IRelease?> FindLatestReleaseAsync(
            ReleaseFeedOptions options,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                Release? latestRelease;
                if (!options.HasFlag(ReleaseFeedOptions.IncludeCanaryReleases))
                {
                    //
                    // Use the whacky /latest API to avoid having
                    // to page over dozens of releases.
                    //
                    // The /latest API ignores prereleases
                    //
                    latestRelease = await this.restAdapter
                        .GetAsync<Release>(
                            new Uri($"https://api.github.com/repos/{this.Repository}/releases/latest"),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    //
                    // Use the latest release (by version), regardless of
                    // whether it's a prerelease or not.
                    //
                    var releases = await
                        ListReleasesAsync(
                            options,
                            cancellationToken)
                        .ConfigureAwait(false);
                    latestRelease = (Release)releases.FirstOrDefault();
                }

                if (latestRelease == null)
                {
                    return null;
                }
                else
                {
                    ApplicationTraceSource.Log.TraceVerbose(
                        "Found new release: {0}", latestRelease.TagVersion);

                    var surveyAssetUrl = latestRelease
                        .Assets
                        .EnsureNotNull()
                        .FirstOrDefault(u => u.DownloadUrl.EndsWith(
                            "survey.dat",
                            StringComparison.OrdinalIgnoreCase))?
                        .DownloadUrl;
                    if (surveyAssetUrl != null)
                    {
                        ApplicationTraceSource.Log.TraceVerbose(
                            "Found survey: {0}", surveyAssetUrl);

                        //
                        // Try to load survey details.
                        //
                        try
                        {
                            var survey = await this.restAdapter
                               .GetAsync<ReleaseSurvey>(
                                   new Uri(surveyAssetUrl),
                                   cancellationToken)
                               .ConfigureAwait(false);
                            if (survey != null &&
                                !string.IsNullOrEmpty(survey.Title) &&
                                !string.IsNullOrEmpty(survey.Description) &&
                                !string.IsNullOrEmpty(survey.Url))
                            {
                                latestRelease.Survey = survey;
                            }
                        }
                        catch (Exception e)
                        {
                            // Ignore in Release builds.
                            if (!Install.IsExecutingTests)
                            {
                                Debug.Fail(e.FullMessage());
                            }
                        }
                    }

                    //
                    // New release available.
                    //
                    return latestRelease;
                }
            }
        }

        public Task<IRelease?> FindLatestReleaseAsync(
            CancellationToken cancellationToken)
        {
            return FindLatestReleaseAsync(ReleaseFeedOptions.None, cancellationToken);
        }

        //---------------------------------------------------------------------
        // Classes for deserialization.
        //---------------------------------------------------------------------

        public class Release : IRelease
        {
            [JsonProperty("tag_name")]
            public string? TagName { get; }

            [JsonProperty("html_url")]
            public string HtmlUrl { get; }

            [JsonProperty("body")]
            public string? Body { get; }

            [JsonProperty("assets")]
            public List<ReleaseAsset>? Assets { get; }

            [JsonProperty("prerelease")]
            public bool IsCanaryRelease { get; }

            [JsonConstructor]
            public Release(
                [JsonProperty("tag_name")] string? tagName,
                [JsonProperty("html_url")] string htmlUrl,
                [JsonProperty("body")] string? body,
                [JsonProperty("prerelease")] bool? prerelease,
                [JsonProperty("assets")] List<ReleaseAsset>? assets)
            {
                this.TagName = tagName;
                this.HtmlUrl = htmlUrl;
                this.Body = body;
                this.IsCanaryRelease = prerelease == true;
                this.Assets = assets;
            }

            public Version? TagVersion
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

            public IReleaseSurvey? Survey { get; internal set; }

            public string DetailsUrl => this.HtmlUrl;

            public string? Description => this.Body;

            public bool TryGetDownloadUrl(Architecture architecture, out string? downloadUrl)
            {
                var msiFiles = this
                    .Assets
                    .EnsureNotNull()
                    .Where(u => u.DownloadUrl.EndsWith(
                        ".msi",
                        StringComparison.OrdinalIgnoreCase))
                    .EnsureNotNull();

                var platformSpecificMsiFiles = msiFiles
                    .Where(u => u.DownloadUrl.IndexOf(
                        architecture.ToString(),
                        StringComparison.OrdinalIgnoreCase) > 0);

                if (platformSpecificMsiFiles.Any())
                {
                    downloadUrl = platformSpecificMsiFiles.First().DownloadUrl;
                }
                else if (msiFiles.Any())
                {
                    downloadUrl = msiFiles.First().DownloadUrl;
                }
                else
                {
                    downloadUrl = null;
                }

                return downloadUrl != null;
            }
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

        public class ReleaseSurvey : IReleaseSurvey
        {
            [JsonProperty("title")]
            public string Title { get; }

            [JsonProperty("description")]
            public string Description { get; }

            [JsonProperty("url")]
            public string Url { get; }

            public ReleaseSurvey(
                [JsonProperty("title")] string title,
                [JsonProperty("description")] string description,
                [JsonProperty("url")] string url)
            {
                this.Title = title;
                this.Description = description;
                this.Url = url;
            }
        }
    }
}
