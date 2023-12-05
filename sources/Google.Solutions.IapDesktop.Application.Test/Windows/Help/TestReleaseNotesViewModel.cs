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

using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Windows.Help;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Help
{
    [TestFixture]
    public class TestReleaseNotesViewModel
    {
        private static Mock<IGitHubRelease> CreateRelease(Version version, string descripion)
        {
            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.TagVersion).Returns(version);
            release.SetupGet(r => r.Description).Returns(descripion);
            return release;
        }

        //---------------------------------------------------------------------
        // Summary.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotLoadedYet_ThenSummaryContainsDefaultText()
        {
            var viewModel = new ReleaseNotesViewModel(
                new Mock<IInstall>().Object,
                new Mock<IGithubClient>().Object);

            Assert.AreEqual("Loading...", viewModel.Summary.Value);
        }

        [Test]
        public async Task WhenLoadingFailed_ThenSummaryContainsError()
        {
            var adapter = new Mock<IGithubClient>();
            adapter
                .Setup(a => a.ListReleasesAsync(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("mock"));

            var viewModel = new ReleaseNotesViewModel(
                new Mock<IInstall>().Object,
                adapter.Object);

            await viewModel.RefreshCommand
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            StringAssert.Contains("mock", viewModel.Summary.Value);
        }

        [Test]
        public async Task WhenLatestVersionIsNewerThanCurrentVersion_ThenSummaryIgnoresLatestVersion()
        {
            var currentVersion = new Version(2, 1, 0, 0);

            var install = new Mock<IInstall>();
            install.SetupGet(i => i.CurrentVersion).Returns(currentVersion);

            var latestRelease = CreateRelease(new Version(2, 2, 0, 0), "latest release");
            var currentRelease = CreateRelease(currentVersion, "current release");
            var oldRelease = CreateRelease(new Version(2, 0, 0, 0), "old release");

            var adapter = new Mock<IGithubClient>();
            adapter
                .Setup(a => a.ListReleasesAsync(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {
                    oldRelease.Object,
                    currentRelease.Object,
                    latestRelease.Object
                });

            var viewModel = new ReleaseNotesViewModel(
                install.Object,
                adapter.Object);

            await viewModel.RefreshCommand
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            StringAssert.DoesNotContain("latest release", viewModel.Summary.Value);
            StringAssert.Contains("current release", viewModel.Summary.Value);
            StringAssert.Contains("old release", viewModel.Summary.Value);
        }

        //---------------------------------------------------------------------
        // ShowAllReleases.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenShowAllReleasesisFalse_ThenSummaryExcludesOlderReleases()
        {
            var currentVersion = new Version(2, 1, 0, 0);
            var previousVersion = new Version(2, 0, 0, 0);

            var install = new Mock<IInstall>();
            install.SetupGet(i => i.CurrentVersion).Returns(currentVersion);
            install.SetupGet(i => i.PreviousVersion).Returns(previousVersion);

            var currentRelease = CreateRelease(currentVersion, "current release");
            var skippedRelease = CreateRelease(new Version(2, 0, 1, 0), "skipped release");
            var previousRelease = CreateRelease(previousVersion, "previous release");
            var oldRelease = CreateRelease(new Version(1, 0, 0, 0), "old release");

            var adapter = new Mock<IGithubClient>();
            adapter
                .Setup(a => a.ListReleasesAsync(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {
                     oldRelease.Object,
                     currentRelease.Object,
                     previousRelease.Object,
                     skippedRelease.Object,
                });

            var viewModel = new ReleaseNotesViewModel(
                install.Object,
                adapter.Object)
            {
                ShowAllReleases = false
            };

            await viewModel.RefreshCommand
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            StringAssert.Contains("current release", viewModel.Summary.Value);
            StringAssert.Contains("skipped release", viewModel.Summary.Value);
            StringAssert.DoesNotContain("previous release", viewModel.Summary.Value);
            StringAssert.DoesNotContain("old release", viewModel.Summary.Value);
        }
    }
}
