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

using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.Testing.Application.Test;
using Google.Solutions.Testing.Common;
using Moq;
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
        public void WhenServerReturnsError_ThenFindLatestReleaseThrowsException()
        {
            var restAdapter = new Mock<IExternalRestAdapter>();
            restAdapter
                .Setup(a => a.GetAsync<GithubAdapter.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("mock"));

            var adapter = new GithubAdapter(restAdapter.Object);

            ExceptionAssert.ThrowsAggregateException<HttpRequestException>(
                () => adapter
                    .FindLatestReleaseAsync(CancellationToken.None)
                    .Wait());
        }

        [Test]
        public async Task WhenServerReturnsEmptyResult_ThenFindLatestReleaseReturnsNull()
        {
            var restAdapter = new Mock<IExternalRestAdapter>();
            restAdapter
                .Setup(a => a.GetAsync<GithubAdapter.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GithubAdapter.Release)null);

            var adapter = new GithubAdapter(restAdapter.Object);

            Assert.IsNull(await adapter
                .FindLatestReleaseAsync(CancellationToken.None)
                .ConfigureAwait(false));
        }

        //---------------------------------------------------------------------
        // TagVersion.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenTagIsVersion_ThenTagVersionIsNotNull()
        {
            var restAdapter = new Mock<IExternalRestAdapter>();
            restAdapter
                .Setup(a => a.GetAsync<GithubAdapter.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubAdapter.Release("1.2.3.4", null, null));

            var adapter = new GithubAdapter(restAdapter.Object);
            var release = await adapter
                .FindLatestReleaseAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.AreEqual(new Version(1, 2, 3, 4), release.TagVersion);
        }

        [Test]
        public async Task WhenTagMalformed_ThenTagVersionIsNull()
        {
            var restAdapter = new Mock<IExternalRestAdapter>();
            restAdapter
                .Setup(a => a.GetAsync<GithubAdapter.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubAdapter.Release("not a version", null, null));

            var adapter = new GithubAdapter(restAdapter.Object);
            var release = await adapter
                .FindLatestReleaseAsync(CancellationToken.None)
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
            var restAdapter = new Mock<IExternalRestAdapter>();
            restAdapter
                .Setup(a => a.GetAsync<GithubAdapter.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubAdapter.Release(
                    "1.2.3.4",
                    null,
                    new System.Collections.Generic.List<GithubAdapter.ReleaseAsset>()
                    {
                        new GithubAdapter.ReleaseAsset("http://example.com/test.txt")
                    }));

            var adapter = new GithubAdapter(restAdapter.Object);
            var release = await adapter
                .FindLatestReleaseAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(release);
            Assert.IsNull(release.DownloadUrl);
        }

        [Test]
        public async Task WhenReleaseHasMsiDownload_ThenDownloadUrlIsNull()
        {
            var restAdapter = new Mock<IExternalRestAdapter>();
            restAdapter
                .Setup(a => a.GetAsync<GithubAdapter.Release>(
                    It.IsNotNull<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GithubAdapter.Release(
                    "1.2.3.4",
                    null,
                    new System.Collections.Generic.List<GithubAdapter.ReleaseAsset>()
                    {
                        new GithubAdapter.ReleaseAsset("http://example.com/test.txt"),
                        new GithubAdapter.ReleaseAsset("http://example.com/download.msi")
                    }));

            var adapter = new GithubAdapter(restAdapter.Object);
            var release = await adapter
                .FindLatestReleaseAsync(CancellationToken.None)
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
            var adapter = new GithubAdapter(new ExternalRestAdapter());
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
    }
}
