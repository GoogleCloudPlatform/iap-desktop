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

using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.Testing.Application.Test;
using Google.Solutions.Testing.Common;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    public class TestGithubAdapter : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // FindLatestReleaseAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenRepositoryExists_ThenFindLatestReleaseReturnsRelease()
        {
            var adapter = new GithubAdapter();
            var release = await adapter
                .FindLatestReleaseAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.IsTrue(release.TagVersion.Major >= 1);

            Assert.IsNotNull(release.DownloadUrl);
            Assert.IsTrue(Uri.IsWellFormedUriString(release.DownloadUrl, UriKind.Absolute));
            StringAssert.EndsWith(".msi", release.DownloadUrl);

            Assert.IsNotNull(release.DetailsUrl);
            Assert.IsTrue(Uri.IsWellFormedUriString(release.DetailsUrl, UriKind.Absolute));
        }

        [Test]
        public void WhenRepositoryInvalid_ThenFindLatestReleaseReturnsThrowsException()
        {
            var adapter = new GithubAdapter()
            {
                RepositoryName = "GoogleCloudPlatform/iap-desktop-doesnotexist"
            };

            ExceptionAssert.ThrowsAggregateException<HttpRequestException>(
                () => adapter
                    .FindLatestReleaseAsync(CancellationToken.None)
                    .Wait());
        }

        [Ignore("Avoid dependency on other repository")]
        [Test]
        public async Task WhenRepositoryDoesNotHaveDownloads_ThenFindLatestReleaseReturnsNull()
        {
            var adapter = new GithubAdapter()
            {
                RepositoryName = "googleapis/google-api-dotnet-client"
            };

            var release = await adapter
                .FindLatestReleaseAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.IsNull(release.TagVersion);
            Assert.IsNull(release.DownloadUrl);

            Assert.IsNotNull(release.DetailsUrl);
            Assert.IsTrue(Uri.IsWellFormedUriString(release.DetailsUrl, UriKind.Absolute));
        }
    }
}
