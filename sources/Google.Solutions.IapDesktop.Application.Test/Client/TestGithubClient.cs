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
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.Platform;
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
        private const string SampleDetailsUrl = "https://example.com/";

        //---------------------------------------------------------------------
        // FindLatestRelease.
        //---------------------------------------------------------------------

        [Test]
        public void FindLatestRelease_WhenServerReturnsError_ThenFindLatestReleaseThrowsException()
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
                    .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                    .Wait());
        }

        [Test]
        public async Task FindLatestRelease_WhenServerReturnsEmptyResult_ThenFindLatestReleaseReturnsNull()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GithubClient.Release?)null);

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            Assert.IsNull(await adapter
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task FindLatestRelease_WhenIncludeCanaryReleasesIsOn_ThenFindLatestReleaseIncludesPrereleases()
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
                    new GithubClient.Release("3.0.1", SampleDetailsUrl, null, true, null),
                    new GithubClient.Release("1.0.0", SampleDetailsUrl, null, null, null),
                    new GithubClient.Release("1.0.1", SampleDetailsUrl, null, false, null),
                    new GithubClient.Release("2.0.1", SampleDetailsUrl, null, true, null),
                    new GithubClient.Release("4.0.1", SampleDetailsUrl, null, true, null),
                });

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            var latest = await adapter
                .FindLatestReleaseAsync(ReleaseFeedOptions.IncludeCanaryReleases, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(latest);
            Assert.That(latest!.TagVersion!.ToString(), Is.EqualTo("4.0.1"));
        }

        [Test]
        public async Task FindLatestRelease_WhenIncludeCanaryReleasesIsOff_ThenFindLatestReleaseReturnsLatest()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    new Uri(
                        $"https://api.github.com/repos/{SampleRepository}/releases/latest"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release("3.0.1", SampleDetailsUrl, null, false, null));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            var latest = await adapter
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(latest?.TagVersion?.ToString(), Is.EqualTo("3.0.1"));
        }

        //---------------------------------------------------------------------
        // FindLatestRelease - survey.
        //---------------------------------------------------------------------

        [Test]
        public async Task FindLatestRelease_WhenReleaseDoesNotIncludeSurvey_ThenFindLatestReleaseReturnsReleaseWithoutSurvey()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    new Uri($"https://api.github.com/repos/{SampleRepository}/releases/latest"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release("3.0.1", SampleDetailsUrl, null, false, null));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            var latest = await adapter
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(latest);
            Assert.IsNull(latest!.Survey);
        }

        [Test]
        public async Task FindLatestRelease_WhenSurveyDownloadFails_ThenFindLatestReleaseReturnsReleaseWithoutSurvey()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    new Uri($"https://api.github.com/repos/{SampleRepository}/releases/latest"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release(
                    "3.0.1",
                    SampleDetailsUrl,
                    null,
                    false,
                    new List<GithubClient.ReleaseAsset>
                    {
                        new GithubClient.ReleaseAsset("https://github/Survey.dat")
                    }));
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.ReleaseSurvey>(
                    new Uri("https://github/Survey.dat"),
                    It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException("mock"));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            var latest = await adapter
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(latest);
            Assert.IsNull(latest!.Survey);
        }

        [Test]
        public async Task FindLatestRelease_WhenReleaseIncludesSurvey_ThenFindLatestReleaseReturnsReleaseWithSurvey()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    new Uri($"https://api.github.com/repos/{SampleRepository}/releases/latest"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release(
                    "3.0.1",
                    SampleDetailsUrl,
                    null,
                    false,
                    new List<GithubClient.ReleaseAsset>
                    {
                        new GithubClient.ReleaseAsset("https://github/Survey.dat")
                    }));
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.ReleaseSurvey>(
                    new Uri("https://github/Survey.dat"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.ReleaseSurvey(
                    "title",
                    "description",
                    "http://survey.example.com/"));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            var latest = await adapter
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(latest);
            Assert.IsNotNull(latest!.Survey);
            Assert.That(latest!.Survey!.Title, Is.EqualTo("title"));
            Assert.That(latest.Survey.Description, Is.EqualTo("description"));
            Assert.That(latest.Survey.Url, Is.EqualTo("http://survey.example.com/"));
        }

        //---------------------------------------------------------------------
        // ListReleases.
        //---------------------------------------------------------------------

        [Test]
        public void ListReleases_WhenServerReturnsError_ThenListReleasesThrowsException()
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
                    .ListReleasesAsync(ReleaseFeedOptions.None, CancellationToken.None)
                    .Wait());
        }

        [Test]
        public async Task ListReleases_WhenServerReturnsEmptyResult_ThenListReleasesReturnsEmptyList()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<List<GithubClient.Release>>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<GithubClient.Release>?)null);

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            Assert.That(await adapter
                .ListReleasesAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false), Is.Empty);
        }

        [Test]
        public async Task ListReleases_WhenServerReturnsMultiplePages_ThenListReleasesReturnsOrderedList()
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
                    new GithubClient.Release("1.0.0", SampleDetailsUrl, null, false, null),
                    new GithubClient.Release("3.1.1", SampleDetailsUrl, null, false, null),
                });
            restAdapter
                .Setup(a => a.GetAsync<List<GithubClient.Release>>(
                    new Uri(
                        $"https://api.github.com/repos/{SampleRepository}/releases?" +
                        $"per_page={GithubClient.PageSize}&page=2"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GithubClient.Release>
                {
                    new GithubClient.Release("2.0.0", SampleDetailsUrl, null, false, null),
                    new GithubClient.Release(null,    SampleDetailsUrl, null, false, null),
                    new GithubClient.Release("2.0.1", SampleDetailsUrl, null, false, null),
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
                .ListReleasesAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(releases.Count(), Is.EqualTo(4));
            Assert.That(
                releases.Select(r => r.TagVersion!.ToString()), Is.EqualTo(new[]
                {
                    "3.1.1",
                    "2.0.1",
                    "2.0.0",
                    "1.0.0",
                }).AsCollection);
        }

        [Test]
        public async Task ListReleases_WhenIncludeCanaryReleasesIsOn_ThenListReleasesIncludesPrereleases()
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
                    new GithubClient.Release("3.0.1", SampleDetailsUrl, null, true, null),
                    new GithubClient.Release("1.0.0", SampleDetailsUrl, null, null, null),
                    new GithubClient.Release("1.0.1", SampleDetailsUrl, null, false, null),
                    new GithubClient.Release("2.0.1", SampleDetailsUrl, null, true, null),
                });

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            var releases = await adapter
                .ListReleasesAsync(ReleaseFeedOptions.IncludeCanaryReleases, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(releases.Count(), Is.EqualTo(4));
            Assert.That(
                releases.Select(r => r.TagVersion!.ToString()), Is.EqualTo(new[]
                {
                    "3.0.1",
                    "2.0.1",
                    "1.0.1",
                    "1.0.0",
                }).AsCollection);
        }

        [Test]
        public async Task ListReleases_WhenIncludeCanaryReleasesIsOff_ThenListReleasesIgnoresPrereleases()
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
                    new GithubClient.Release("3.0.1", SampleDetailsUrl, null, true, null),
                    new GithubClient.Release("1.0.0", SampleDetailsUrl, null, null, null),
                    new GithubClient.Release("1.0.1", SampleDetailsUrl, null, false, null),
                    new GithubClient.Release("2.0.1", SampleDetailsUrl, null, true, null),
                });

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);

            var releases = await adapter
                .ListReleasesAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(releases.Count(), Is.EqualTo(2));
            Assert.That(
                releases.Select(r => r.TagVersion!.ToString()), Is.EqualTo(new[]
                {
                    "1.0.1",
                    "1.0.0",
                }).AsCollection);
        }

        //---------------------------------------------------------------------
        // TagVersion.
        //---------------------------------------------------------------------

        [Test]
        public async Task TagVersion_WhenTagIsVersion_ThenTagVersionIsNotNull()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release("1.2.3.4", SampleDetailsUrl, null, false, null));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);
            var release = await adapter
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.That(release!.TagVersion, Is.EqualTo(new Version(1, 2, 3, 4)));
        }

        [Test]
        public async Task TagVersion_WhenTagMalformed_ThenTagVersionIsNull()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release("not a version", SampleDetailsUrl, null, false, null));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);
            var release = await adapter
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.IsNull(release!.TagVersion);
        }

        //---------------------------------------------------------------------
        // DownloadUrl.
        //---------------------------------------------------------------------

        [Test]
        public async Task DownloadUrl_WhenReleaseHasNoMsiDownload()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release(
                    "1.2.3.4",
                    SampleDetailsUrl,
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
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.IsFalse(release!.TryGetDownloadUrl(
                Architecture.X86,
                out var downloadUrl));
            Assert.IsNull(downloadUrl);
        }

        [Test]
        public async Task DownloadUrl_WhenReleaseHasPlatformSpecificMsiDownload()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release(
                    "1.2.3.4",
                    SampleDetailsUrl,
                    null,
                    null,
                    new List<GithubClient.ReleaseAsset>()
                    {
                        new GithubClient.ReleaseAsset("http://example.com/x86.x64.txt"),
                        new GithubClient.ReleaseAsset("http://example.com/download.x64.msi"),
                        new GithubClient.ReleaseAsset("http://example.com/download.x86.MSI"),
                        new GithubClient.ReleaseAsset("http://example.com/download.arm64.msi"),
                        new GithubClient.ReleaseAsset("http://example.com/download.MSI")
                    }));

            var adapter = new GithubClient(
                restAdapter.Object,
                SampleRepository);
            var release = await adapter
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);

            Assert.IsTrue(release!.TryGetDownloadUrl(
                Architecture.X86,
                out var downloadUrlX86));
            Assert.That(downloadUrlX86, Is.EqualTo("http://example.com/download.x86.MSI"));

            Assert.IsTrue(release.TryGetDownloadUrl(
                Architecture.X64,
                out var downloadUrlX64));
            Assert.That(downloadUrlX64, Is.EqualTo("http://example.com/download.x64.msi"));

            Assert.IsTrue(release.TryGetDownloadUrl(
                Architecture.Arm64,
                out var downloadUrlArm64));
            Assert.That(downloadUrlArm64, Is.EqualTo("http://example.com/download.arm64.msi"));
        }

        [Test]
        public async Task DownloadUrl_WhenReleaseHasGenericMsiDownload()
        {
            var restAdapter = new Mock<IExternalRestClient>();
            restAdapter
                .Setup(a => a.GetAsync<GithubClient.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubClient.Release(
                    "1.2.3.4",
                    SampleDetailsUrl,
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
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.IsTrue(release!.TryGetDownloadUrl(
                Architecture.X86,
                out var downloadUrl));
            Assert.That(downloadUrl, Is.EqualTo("http://example.com/download.msi"));
        }

        //---------------------------------------------------------------------
        // Test with real backend.
        //---------------------------------------------------------------------

        [Test]
        public async Task FindLatestRelease_WhenRepositoryExists()
        {
            var adapter = new GithubClient(
                new ExternalRestClient(),
                "GoogleCloudPlatform/iap-desktop");
            var release = await adapter
                .FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.IsNotNull(release!.TagVersion);
            Assert.IsTrue(release.TagVersion!.Major >= 1);

            Assert.IsTrue(release.TryGetDownloadUrl(
                Architecture.X86,
                out var downloadUrl));

            Assert.IsNotNull(downloadUrl);
            Assert.IsTrue(Uri.IsWellFormedUriString(downloadUrl, UriKind.Absolute));
            Assert.That(downloadUrl, Does.EndWith(".msi"));

            Assert.IsNotNull(release.DetailsUrl);
            Assert.IsTrue(Uri.IsWellFormedUriString(release.DetailsUrl, UriKind.Absolute));
        }
    }
}
