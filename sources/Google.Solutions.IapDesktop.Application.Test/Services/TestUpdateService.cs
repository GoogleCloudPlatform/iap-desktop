//
// Copyright 2022 Google LLC
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

using Google.Apis.Util;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Services
{
    [TestFixture]
    public class TestUpdateService : ApplicationFixtureBase
    {
        private static IClock CreateClock(DateTime dateTime)
        {
            var clock = new Mock<IClock>();
            clock.SetupGet(c => c.UtcNow).Returns(dateTime);
            return clock.Object;
        }

        private static Install CreateInstall()
        {
            return new Install(Install.DefaultBaseKeyPath);
        }

        //---------------------------------------------------------------------
        // IsUpdateCheckDue.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotEnoughDaysElapsed_ThenIsUpdateCheckDueIsFalse()
        {
            var now = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var updateService = new UpdateService(
                CreateInstall(),
                new Mock<IGithubAdapter>().Object,
                new Mock<ITaskDialog>().Object,
                CreateClock(now));

            Assert.IsFalse(updateService.IsUpdateCheckDue(now));
            Assert.IsFalse(updateService.IsUpdateCheckDue(now.AddYears(1)));
            Assert.IsFalse(updateService.IsUpdateCheckDue(now.AddDays(-UpdateService.DaysBetweenUpdateChecks).AddMinutes(1)));
            Assert.IsFalse(updateService.IsUpdateCheckDue(now.AddDays(-UpdateService.DaysBetweenUpdateChecks + 1)));
        }

        [Test]
        public void WhenTooManyDaysElapsed_ThenIsUpdateCheckDueIsTrue()
        {
            var now = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var updateService = new UpdateService(
                CreateInstall(),
                new Mock<IGithubAdapter>().Object,
                new Mock<ITaskDialog>().Object,
                CreateClock(now));

            Assert.IsTrue(updateService.IsUpdateCheckDue(now.AddDays(-UpdateService.DaysBetweenUpdateChecks)));
            Assert.IsTrue(updateService.IsUpdateCheckDue(now.AddDays(-UpdateService.DaysBetweenUpdateChecks - 1)));
        }

        //---------------------------------------------------------------------
        // CheckForUpdates: None available.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoReleaseAvailable_ThenCheckForUpdatesReturns()
        {
            var githubAdapter = new Mock<IGithubAdapter>();
            githubAdapter
                .Setup(a => a.FindLatestReleaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IGitHubRelease)null);

            var taskDialog = new Mock<ITaskDialog>();

            var updateService = new UpdateService(
                CreateInstall(),
                new Mock<IGithubAdapter>().Object,
                taskDialog.Object,
                new Mock<IClock>().Object);

            updateService.CheckForUpdates(null, out var _);

            bool f = false;
            taskDialog.Verify(d => d.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out f), Times.Never);
        }

        [Test]
        public void WhenReleaseHasNoVersion_ThenCheckForUpdatesReturns()
        {
            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.TagVersion).Returns((Version)null);

            var githubAdapter = new Mock<IGithubAdapter>();
            githubAdapter
                .Setup(a => a.FindLatestReleaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(release.Object);

            var taskDialog = new Mock<ITaskDialog>();

            var updateService = new UpdateService(
                CreateInstall(),
                new Mock<IGithubAdapter>().Object,
                taskDialog.Object,
                new Mock<IClock>().Object);

            updateService.CheckForUpdates(null, out var _);

            bool f = false;
            taskDialog.Verify(d => d.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out f), Times.Never);
        }

        [Test]
        public void WhenSameVersionAvailable_ThenCheckForUpdatesReturns()
        {
            var installedVersion = new UpdateService(
                CreateInstall(),
                new Mock<IGithubAdapter>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IClock>().Object).InstalledVersion;

            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.TagVersion).Returns(installedVersion);

            var githubAdapter = new Mock<IGithubAdapter>();
            githubAdapter
                .Setup(a => a.FindLatestReleaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(release.Object);

            var taskDialog = new Mock<ITaskDialog>();

            var updateService = new UpdateService(
                CreateInstall(),
                new Mock<IGithubAdapter>().Object,
                taskDialog.Object,
                new Mock<IClock>().Object);

            updateService.CheckForUpdates(null, out var _);

            bool f = false;
            taskDialog.Verify(d => d.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out f), Times.Never);
        }

        //---------------------------------------------------------------------
        // CheckForUpdates: Update available.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNewReleaseAvailableButDialogOperationCancelled_ThenCheckForUpdatesReturns()
        {
            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version(99, 99, 99, 99));

            var githubAdapter = new Mock<IGithubAdapter>();
            githubAdapter
                .Setup(a => a.FindLatestReleaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(release.Object);

            var taskDialog = new Mock<ITaskDialog>();
            bool f = false;
            taskDialog
                .Setup(d => d.ShowOptionsTaskDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<IntPtr>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IList<string>>(),
                    It.IsAny<string>(),
                    out f))
                .Throws(new OperationCanceledException());

            var updateService = new UpdateService(
                CreateInstall(),
                new Mock<IGithubAdapter>().Object,
                taskDialog.Object,
                new Mock<IClock>().Object);

            updateService.CheckForUpdates(null, out var _);
        }

        [Test]
        public void WhenNewReleaseAvailableButDialogCancelled_ThenCheckForUpdatesReturns()
        {
            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version(99, 99, 99, 99));

            var githubAdapter = new Mock<IGithubAdapter>();
            githubAdapter
                .Setup(a => a.FindLatestReleaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(release.Object);

            var taskDialog = new Mock<ITaskDialog>();
            bool f = false;
            taskDialog
                .Setup(d => d.ShowOptionsTaskDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<IntPtr>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IList<string>>(),
                    It.IsAny<string>(),
                    out f))
                .Returns(2);

            var updateService = new UpdateService(
                CreateInstall(),
                new Mock<IGithubAdapter>().Object,
                taskDialog.Object,
                new Mock<IClock>().Object);

            updateService.CheckForUpdates(null, out var _);
        }
    }
}
