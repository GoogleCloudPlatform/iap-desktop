﻿//
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
using Google.Solutions.Common.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1056 // Uri properties should not be strings
#pragma warning disable CA2227 // Collection properties should be read only

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public class GithubAdapter
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/GoogleCloudPlatform/iap-desktop/releases/latest";
        public const string BaseUrl = "https://github.com/GoogleCloudPlatform/iap-desktop";

        private void OpenUrl(string url)
        {
            using (Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = url
            }))
            { };
        }

        public void ReportIssue()
        {
            var version = typeof(GithubAdapter).Assembly.GetName().Version;
            var body = "Expected behavior:\n" +
                       "* Step 1\n" +
                       "* Step 2\n" +
                       "* ...\n" +
                       "\n" +
                       "Observed behavior:\n" +
                       "* Step 1\n" +
                       "* Step 2\n" +
                       "* ...\n" +
                       "\n" +
                       $"Installed version: {version}\n" +
                       $".NET Version: {ClrVersion.Version}\n" +
                       $"OS Version: {Environment.OSVersion}";
            OpenUrl($"{BaseUrl}/issues/new?body={WebUtility.UrlEncode(body)}");
        }

        public async Task<Release> FindLatestReleaseAsync(CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                var assemblyName = typeof(ComputeEngineAdapter).Assembly.GetName();
                var client = new RestClient()
                {
                    UserAgent = Globals.UserAgent
                };

                var latestRelease = await client.GetAsync<Release>(
                    LatestReleaseUrl,
                    cancellationToken).ConfigureAwait(false);
                if (latestRelease == null)
                {
                    return null;
                }
                else
                {
                    ApplicationTraceSources.Default.TraceVerbose("Found new release: {0}", latestRelease.TagName);

                    // New release available.
                    return latestRelease;
                }
            }
        }

        public class Release
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; }

            public Version TagVersion => Version.Parse(this.TagName);

            [JsonProperty("html_url")]
            public string HtmlUrl { get; set; }

            [JsonProperty("assets")]
            public List<ReleaseAsset> Assets { get; set; }
        }

        public class ReleaseAsset
        {
            [JsonProperty("browser_download_url")]
            public string DownloadUrl { get; set; }
        }
    }
}
