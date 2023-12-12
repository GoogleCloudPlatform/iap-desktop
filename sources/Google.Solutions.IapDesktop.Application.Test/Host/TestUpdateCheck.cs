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
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestUpdateCheck : ApplicationFixtureBase
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

            var updateService = new UpdateCheck(
                CreateInstall(),
                new Mock<IReleaseFeed>().Object,
                new Mock<ILegacyTaskDialog>().Object,
                CreateClock(now));

            Assert.IsFalse(updateService.IsUpdateCheckDue(now));
            Assert.IsFalse(updateService.IsUpdateCheckDue(now.AddYears(1)));
            Assert.IsFalse(updateService.IsUpdateCheckDue(now.AddDays(-UpdateCheck.DaysBetweenUpdateChecks).AddMinutes(1)));
            Assert.IsFalse(updateService.IsUpdateCheckDue(now.AddDays(-UpdateCheck.DaysBetweenUpdateChecks + 1)));
        }

        [Test]
        public void WhenTooManyDaysElapsed_ThenIsUpdateCheckDueIsTrue()
        {
            var now = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var updateService = new UpdateCheck(
                CreateInstall(),
                new Mock<IReleaseFeed>().Object,
                new Mock<ILegacyTaskDialog>().Object,
                CreateClock(now));

            Assert.IsTrue(updateService.IsUpdateCheckDue(now.AddDays(-UpdateCheck.DaysBetweenUpdateChecks)));
            Assert.IsTrue(updateService.IsUpdateCheckDue(now.AddDays(-UpdateCheck.DaysBetweenUpdateChecks - 1)));
        }

        //---------------------------------------------------------------------
        // CheckForUpdates: None available.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoReleaseAvailable_ThenCheckForUpdatesReturns()
        {
            var feed = new Mock<IReleaseFeed>();
            feed
                .Setup(a => a.FindLatestReleaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IRelease)null);

            var taskDialog = new Mock<ILegacyTaskDialog>();

            var updateService = new UpdateCheck(
                CreateInstall(),
                new Mock<IReleaseFeed>().Object,
                taskDialog.Object,
                new Mock<IClock>().Object);

            updateService.CheckForUpdates(null);

            var f = false;
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
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns((Version)null);

            var feed = new Mock<IReleaseFeed>();
            feed
                .Setup(a => a.FindLatestReleaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(release.Object);

            var taskDialog = new Mock<ILegacyTaskDialog>();

            var updateService = new UpdateCheck(
                CreateInstall(),
                new Mock<IReleaseFeed>().Object,
                taskDialog.Object,
                new Mock<IClock>().Object);

            updateService.CheckForUpdates(null);

            var f = false;
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
            var installedVersion = new UpdateCheck(
                CreateInstall(),
                new Mock<IReleaseFeed>().Object,
                new Mock<ILegacyTaskDialog>().Object,
                new Mock<IClock>().Object).InstalledVersion;

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(installedVersion);

            var feed = new Mock<IReleaseFeed>();
            feed
                .Setup(a => a.FindLatestReleaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(release.Object);

            var taskDialog = new Mock<ILegacyTaskDialog>();

            var updateService = new UpdateCheck(
                CreateInstall(),
                new Mock<IReleaseFeed>().Object,
                taskDialog.Object,
                new Mock<IClock>().Object);

            updateService.CheckForUpdates(null);

            var f = false;
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
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version(99, 99, 99, 99));

            var feed = new Mock<IReleaseFeed>();
            feed
                .Setup(a => a.FindLatestReleaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(release.Object);

            var taskDialog = new Mock<ILegacyTaskDialog>();
            var f = false;
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

            var updateService = new UpdateCheck(
                CreateInstall(),
                new Mock<IReleaseFeed>().Object,
                taskDialog.Object,
                new Mock<IClock>().Object);

            updateService.CheckForUpdates(null);
        }

        [Test]
        public void WhenNewReleaseAvailableButDialogCancelled_ThenCheckForUpdatesReturns()
        {
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version(99, 99, 99, 99));

            var feed = new Mock<IReleaseFeed>();
            feed
                .Setup(a => a.FindLatestReleaseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(release.Object);

            var taskDialog = new Mock<ILegacyTaskDialog>();
            var f = false;
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

            var updateService = new UpdateCheck(
                CreateInstall(),
                new Mock<IReleaseFeed>().Object,
                taskDialog.Object,
                new Mock<IClock>().Object);

            updateService.CheckForUpdates(null);
        }
    }
}
