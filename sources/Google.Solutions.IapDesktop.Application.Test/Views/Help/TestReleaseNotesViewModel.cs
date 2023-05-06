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

using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views.Help;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Help
{
    [TestFixture]
    public class TestReleaseNotesViewModel
    {
        //---------------------------------------------------------------------
        // Summary.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotLoadedYet_ThenSummaryContainsDefaultText()
        {
            var viewModel = new ReleaseNotesViewModel(
                new Mock<IGithubAdapter>().Object);

            Assert.AreEqual("Loading...", viewModel.Summary.Value);
        }

        [Test]
        public async Task WhenLoadingFailed_ThenSummaryContainsError()
        {
            var adapter = new Mock<IGithubAdapter>();
            adapter
                .Setup(a => a.ListReleases(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("mock"));

            var viewModel = new ReleaseNotesViewModel(adapter.Object);

            await viewModel.RefreshCommand
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            StringAssert.Contains("mock", viewModel.Summary.Value);
        }

        [Test]
        public async Task WhenLoaded_ThenSummaryContainsReleaseDetails()
        {
            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version(2, 0, 0, 0));
            release.SetupGet(r => r.Description).Returns("description");

            var adapter = new Mock<IGithubAdapter>();
            adapter
                .Setup(a => a.ListReleases(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { release.Object });

            var viewModel = new ReleaseNotesViewModel(adapter.Object);

            await viewModel.RefreshCommand
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            StringAssert.Contains("description", viewModel.Summary.Value);
        }

        //---------------------------------------------------------------------
        // PreviousVersion.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenLastestReleaseIsLastKnown_ThenSummaryIsEmpty()
        {
            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version(2, 0, 0, 0));
            release.SetupGet(r => r.Description).Returns("description");

            var adapter = new Mock<IGithubAdapter>();
            adapter
                .Setup(a => a.ListReleases(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { release.Object });

            var viewModel = new ReleaseNotesViewModel(adapter.Object)
            {
                PreviousVersion = new Version(2, 0, 0, 0)
            };

            await viewModel.RefreshCommand
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            StringAssert.DoesNotContain("description", viewModel.Summary.Value);
        }
    }
}
