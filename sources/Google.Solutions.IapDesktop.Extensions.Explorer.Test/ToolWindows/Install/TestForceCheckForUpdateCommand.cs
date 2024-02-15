//
// Copyright 2024 Google LLC
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

using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Extensions.Explorer.ToolWindows.Install;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Platform.Net;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Explorer.Test.ToolWindows.Install
{
    [TestFixture]
    public class TestForceCheckForUpdateCommand
    {
        private static IUpdatePolicy CreatePolicy(ReleaseTrack follwedTrack)
        {
            var policy = new Mock<IUpdatePolicy>();
            policy
                .SetupGet(p => p.FollowedTrack)
                .Returns(follwedTrack);
            return policy.Object;
        }

        private static IRelease CreateRelease(Version version)
        {
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(version);
            return release.Object;
        }

        //---------------------------------------------------------------------
        // Execute.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoUpdateAvailable_ThenExecuteReturns(
            [Values(ReleaseTrack.Critical, ReleaseTrack.Normal)]
            ReleaseTrack followedTrack)
        {
            var currentVersion = new Version(1, 2, 3, 4);
            var install = new Mock<IInstall>();
            install.SetupGet(i => i.CurrentVersion).Returns(currentVersion);

            var feed = new Mock<IReleaseFeed>();
            feed
                .Setup(f => f.FindLatestReleaseAsync(
                    ReleaseFeedOptions.None,
                    CancellationToken.None))
                .ReturnsAsync(CreateRelease(currentVersion));

            var dialog = new Mock<ITaskDialog>();

            var command = new ForceCheckForUpdateCommand(
                new Mock<IWin32Window>().Object,
                install.Object,
                CreatePolicy(followedTrack),
                feed.Object,
                dialog.Object,
                new Mock<IBrowser>().Object);

            command.Execute(new Mock<IInstall>().Object);

            dialog.Verify(
                d => d.ShowDialog(It.IsAny<IWin32Window>(), It.IsAny<TaskDialogParameters>()),
                Times.Never);
            feed.Verify(
                f => f.FindLatestReleaseAsync(ReleaseFeedOptions.None, CancellationToken.None),
                Times.Once);
        }

        [Test]
        public void WhenUpdateAvailable_ThenExecuteShowsDialog(
            [Values(ReleaseTrack.Critical, ReleaseTrack.Normal)]
            ReleaseTrack followedTrack)
        {
            var install = new Mock<IInstall>();
            install
                .SetupGet(i => i.CurrentVersion)
                .Returns(new Version(1, 2, 3, 0));

            var feed = new Mock<IReleaseFeed>();
            feed
                .Setup(f => f.FindLatestReleaseAsync(
                    ReleaseFeedOptions.None,
                    CancellationToken.None))
                .ReturnsAsync(CreateRelease(new Version(1, 2, 3, 4)));

            var dialog = new Mock<ITaskDialog>();

            var command = new ForceCheckForUpdateCommand(
                new Mock<IWin32Window>().Object,
                install.Object,
                CreatePolicy(followedTrack),
                feed.Object,
                dialog.Object,
                new Mock<IBrowser>().Object);

            command.Execute(new Mock<IInstall>().Object);

            dialog.Verify(
                d => d.ShowDialog(It.IsAny<IWin32Window>(), It.IsAny<TaskDialogParameters>()),
                Times.Once);
        }

        [Test]
        public void WhenCanaryUpdateAvailable_ThenExecuteShowsDialog()
        {
            var install = new Mock<IInstall>();
            install
                .SetupGet(i => i.CurrentVersion)
                .Returns(new Version(1, 2, 3, 0));

            var feed = new Mock<IReleaseFeed>();
            feed
                .Setup(f => f.FindLatestReleaseAsync(
                    ReleaseFeedOptions.IncludeCanaryReleases,
                    CancellationToken.None))
                .ReturnsAsync(CreateRelease(new Version(1, 2, 3, 4)));

            var dialog = new Mock<ITaskDialog>();

            var command = new ForceCheckForUpdateCommand(
                new Mock<IWin32Window>().Object,
                install.Object,
                CreatePolicy(ReleaseTrack.Canary),
                feed.Object,
                dialog.Object,
                new Mock<IBrowser>().Object);

            command.Execute(new Mock<IInstall>().Object);

            dialog.Verify(
                d => d.ShowDialog(It.IsAny<IWin32Window>(), It.IsAny<TaskDialogParameters>()),
                Times.Once);
        }
    }
}
