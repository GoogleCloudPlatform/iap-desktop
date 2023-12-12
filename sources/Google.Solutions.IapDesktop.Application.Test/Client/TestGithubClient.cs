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

using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Client
{
    [TestFixture]
    public class TestGithubClient : ApplicationFixtureBase
    {
        private const string SampleRepository = "google/sample";

        //---------------------------------------------------------------------
        // FindLatestReleaseAsync.
        //---------------------------------------------------------------------

        [Test]
        public void WhenServerReturnsError_ThenFindLatestReleaseThrowsException()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("mock"));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            ExceptionAssert.ThrowsAggregateException<HttpRequestException>(
                () => adapter
                    .FindLatestReleaseAsync(false, CancellationToken.None)
                    .Wait());
        }

        [Test]
        public async Task WhenServerReturnsEmptyResult_ThenFindLatestReleaseReturnsNull()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GithubClient.Release)null);

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            Assert.IsNull(await adapter
                .FindLatestReleaseAsync(false, CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task WhenIncludeCanaryReleasesIsTrue_ThenFindLatestReleaseIncludesPrereleases()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<List<GithubClient.Release>>(
                    new Uri(
                        $"https://api.github.com/repos/{SampleRepository}/releases?" +
                        $"per_page={GithubClient.PageSize}&page=1"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GithubClient.Release>
                {
                    new GithubClient.Release("3.0.1", null, null, true, null),
                    new GithubClient.Release("1.0.0", null, null, null, null),
                    new GithubClient.Release("1.0.1", null, null, false, null),
                    new GithubClient.Release("2.0.1", null, null, true, null),
                    new GithubClient.Release("4.0.1", null, null, true, null),
                });

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            var latest = await adapter
                .FindLatestReleaseAsync(true, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("4.0.1", latest.TagVersion.ToString());
        }

        [Test]
        public async Task WhenIncludeCanaryReleasesIsFalse_ThenFindLatestReleaseReturnsLatest()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    new Uri(
                        $"https://api.github.com/repos/{SampleRepository}/releases/latest"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release("3.0.1", null, null, false, null));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            var latest = await adapter
                .FindLatestReleaseAsync(false, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("3.0.1", latest.TagVersion.ToString());
        }

        //---------------------------------------------------------------------
        // ListReleases.
        //---------------------------------------------------------------------

        [Test]
        public void WhenServerReturnsError_ThenListReleasesThrowsException()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<List<GithubClient.Release>>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("mock"));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            ExceptionAssert.ThrowsAggregateException<HttpRequestException>(
                () => adapter
                    .ListReleasesAsync(false, CancellationToken.None)
                    .Wait());
        }

        [Test]
        public async Task WhenServerReturnsEmptyResult_ThenListReleasesReturnsEmptyList()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<List<GithubClient.Release>>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<GithubClient.Release>)null);

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            CollectionAssert.IsEmpty(await adapter
                .ListReleasesAsync(false, CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task WhenServerReturnsMultiplePages_ThenListReleasesReturnsOrderedList()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<List<GithubClient.Release>>(
                    new Uri(
                        $"https://api.github.com/repos/{SampleRepository}/releases?" +
                        $"per_page={GithubClient.PageSize}&page=1"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GithubClient.Release>
                {
                    new GithubClient.Release("1.0.0", null, null, false, null),
                    new GithubClient.Release("3.1.1", null, null, false, null),
                });
            restAdapter
                .Setup(a => a.GetAsync<List<GithubClient.Release>>(
                    new Uri(
                        $"https://api.github.com/repos/{SampleRepository}/releases?" +
                        $"per_page={GithubClient.PageSize}&page=2"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GithubClient.Release>
                {
                    new GithubClient.Release("2.0.0", null, null, false, null),
                    new GithubClient.Release(null,    null, null, false, null),
                    new GithubClient.Release("2.0.1", null, null, false, null),
                });
            restAdapter
                .Setup(a => a.GetAsync<List<GithubClient.Release>>(
                    new Uri(
                        $"https://api.github.com/repos/{SampleRepository}/releases?" +
                        $"per_page={GithubClient.PageSize}&page=3"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GithubClient.Release>());

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);
            
            var releases = await adapter
                .ListReleasesAsync(false, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(4, releases.Count());
            CollectionAssert.AreEqual(
                new[]
                {
                    "3.1.1",
                    "2.0.1",
                    "2.0.0",
                    "1.0.0",
                },
                releases.Select(r => r.TagVersion.ToString()));
        }

        [Test]
        public async Task WhenIncludeCanaryReleasesIsTrue_ThenListReleasesIncludesPrereleases()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<List<GithubClient.Release>>(
                    new Uri(
                        $"https://api.github.com/repos/{SampleRepository}/releases?" +
                        $"per_page={GithubClient.PageSize}&page=1"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GithubClient.Release>
                {
                    new GithubClient.Release("3.0.1", null, null, true, null),
                    new GithubClient.Release("1.0.0", null, null, null, null),
                    new GithubClient.Release("1.0.1", null, null, false, null),
                    new GithubClient.Release("2.0.1", null, null, true, null),
                });

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);
            
            var releases = await adapter
                .ListReleasesAsync(true, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(4, releases.Count());
            CollectionAssert.AreEqual(
                new[]
                {
                    "3.0.1",
                    "2.0.1",
                    "1.0.1",
                    "1.0.0",
                },
                releases.Select(r => r.TagVersion.ToString()));
        }

        [Test]
        public async Task WhenIncludeCanaryReleasesIsFalse_ThenListReleasesIgnoresPrereleases()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<List<GithubClient.Release>>(
                    new Uri(
                        $"https://api.github.com/repos/{SampleRepository}/releases?" +
                        $"per_page={GithubClient.PageSize}&page=1"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GithubClient.Release>
                {
                    new GithubClient.Release("3.0.1", null, null, true, null),
                    new GithubClient.Release("1.0.0", null, null, null, null),
                    new GithubClient.Release("1.0.1", null, null, false, null),
                    new GithubClient.Release("2.0.1", null, null, true, null),
                });

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);
            
            var releases = await adapter
                .ListReleasesAsync(false, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(2, releases.Count());
            CollectionAssert.AreEqual(
                new[]
                {
                    "1.0.1",
                    "1.0.0",
                },
                releases.Select(r => r.TagVersion.ToString()));
        }

        //---------------------------------------------------------------------
        // TagVersion.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenTagIsVersion_ThenTagVersionIsNotNull()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release("1.2.3.4", null, null, false, null));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);
            var release = await adapter
                .FindLatestReleaseAsync(false, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.AreEqual(new Version(1, 2, 3, 4), release.TagVersion);
        }

        [Test]
        public async Task WhenTagMalformed_ThenTagVersionIsNull()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release("not a version", null, null, false, null));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);
            var release = await adapter
                .FindLatestReleaseAsync(false, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.IsNull(release.TagVersion);
        }

        //---------------------------------------------------------------------
        // DownloadUrl.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenReleaseHasNoMsiDownload_ThenDownloadUrlIsNull()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release(
                    "1.2.3.4",
                    null,
                    null,
                    null,
                    new List<GithubClient.ReleaseAsset>()
                    {
                        new GithubClient.ReleaseAsset("http://example.com/test.txt")
                    }));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);
            var release = await adapter
                .FindLatestReleaseAsync(false, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.IsNull(release.DownloadUrl);
        }

        [Test]
        public async Task WhenReleaseHasMsiDownload_ThenDownloadUrlIsNull()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release(
                    "1.2.3.4",
                    null,
                    null,
                    null,
                    new List<GithubClient.ReleaseAsset>()
                    {
                        new GithubClient.ReleaseAsset("http://example.com/test.txt"),
                        new GithubClient.ReleaseAsset("http://example.com/download.msi")
                    }));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);
            var release = await adapter
                .FindLatestReleaseAsync(false, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.AreEqual("http://example.com/download.msi", release.DownloadUrl);
        }

        //---------------------------------------------------------------------
        // Test with real backend.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenRepositoryExists_ThenFindLatestReleaseReturnsRelease()
        {
            var adapter = new GithubClient(
                new ExternalRestClient(),
                "GoogleCloudPlatform/iap-desktop");
            var release = await adapter
                .FindLatestReleaseAsync(false, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.IsTrue(release.TagVersion.Major >= 1);

            Assert.IsNotNull(release.DownloadUrl);
            Assert.IsTrue(Uri.IsWellFormedUriString(release.DownloadUrl, UriKind.Absolute));
            StringAssert.EndsWith(".msi", release.DownloadUrl);

            Assert.IsNotNull(release.DetailsUrl);
            Assert.IsTrue(Uri.IsWellFormedUriString(release.DetailsUrl, UriKind.Absolute));
        }
    }
}
