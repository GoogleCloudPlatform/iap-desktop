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


using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.ToolWindows.Update;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Platform.Net;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.ToolWindows.Update
{
    [TestFixture]
    public class TestCheckForUpdateCommand
    {
        private static IUpdatePolicy CreatePolicy(
            bool adviseAllUpdates,
            ReleaseTrack followedTrack)
        {
            var policy = new Mock<IUpdatePolicy>();
            policy.SetupGet(p => p.FollowedTrack).Returns(followedTrack);
            policy
                .Setup(p => p.IsUpdateAdvised(It.IsAny<IRelease>()))
                .Returns(adviseAllUpdates);

            return policy.Object;
        }

        private static ITaskDialog CreateDialog(string commandLinkToClick)
        {
            var dialog = new Mock<ITaskDialog>();
            dialog
                .Setup(d => d.ShowDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<TaskDialogParameters>()))
                .Callback<IWin32Window, TaskDialogParameters>((w, p) =>
                {
                    p.Buttons
                        .OfType<TaskDialogCommandLinkButton>()
                        .First(b => b.Text == commandLinkToClick)
                        .PerformClick();
                })
                .Returns(DialogResult.OK);

            return dialog.Object;
        }

        private static ITaskDialog CreateCancelledDialog()
        {
            var dialog = new Mock<ITaskDialog>();
            dialog
                .Setup(d => d.ShowDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<TaskDialogParameters>()))
                .Returns(DialogResult.Cancel);

            return dialog.Object;
        }

        //---------------------------------------------------------------------
        // IsAutomatedCheckDue.
        //---------------------------------------------------------------------

        [Test]
        public void IsAutomatedCheckDue()
        {
            var policy = new Mock<IUpdatePolicy>();
            policy
                .Setup(p => p.IsUpdateCheckDue(It.IsAny<DateTime>()))
                .Returns(true);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policy.Object,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object);

            var time = DateTime.Now;
            Assert.IsTrue(command.IsAutomatedCheckDue(time));

            policy.Verify(p => p.IsUpdateCheckDue(time), Times.Once);
        }

        //---------------------------------------------------------------------
        // FeedOptions.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUserFollowsCanaryTrack_ThenFeedOptionsIncludeCanaryReleases()
        {
            var policyFactory = CreatePolicy(true, ReleaseTrack.Canary);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policyFactory,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object);

            Assert.AreEqual(ReleaseFeedOptions.IncludeCanaryReleases, command.FeedOptions);
        }

        [Test]
        public void WhenUserFollowsNormalTrack_ThenFeedOptionsAreClear()
        {
            var policyFactory = CreatePolicy(true, ReleaseTrack.Normal);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policyFactory,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object);

            Assert.AreEqual(ReleaseFeedOptions.None, command.FeedOptions);
        }

        //---------------------------------------------------------------------
        // PromptForAction - download.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReleaseIsNull_ThenPromptForActionReturns()
        {
            var policy = new Mock<IUpdatePolicy>();

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policy.Object,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object);

            command.PromptForAction(null);

            policy.Verify(p => p.IsUpdateAdvised(It.IsAny<IRelease>()), Times.Never);
        }

        [Test]
        public void WhenPolicyDoesNotAdviseUpdate_ThenPromptForActionReturns()
        {
            var policy = new Mock<IUpdatePolicy>();

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policy.Object,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object);

            command.PromptForAction(new Mock<IRelease>().Object);

            policy.Verify(p => p.IsUpdateAdvised(It.IsAny<IRelease>()), Times.Once);
        }

        [Test]
        public void WhenUserCancels_ThenPromptForActionReturns()
        {
            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(true, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateCancelledDialog(),
                new Mock<IBrowser>().Object);

            command.PromptForAction(new Mock<IRelease>().Object);
        }

        [Test]
        public void WhenUserSelectsDownload_ThenPromptForActionOpensDownload()
        {
            var browser = new Mock<IBrowser>();
            var release = new Mock<IRelease>();

            var downloadUrl = "http://example.com/download";
            release
                .Setup(r => r.TryGetDownloadUrl(Install.ProcessArchitecture, out downloadUrl))
                .Returns(true);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(true, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateDialog("Yes, download now"),
                browser.Object);

            command.PromptForAction(release.Object);

            browser.Verify(b => b.Navigate(downloadUrl), Times.Once);
        }

        [Test]
        public void WhenDownloadUrlNotFound_ThenPromptForActionOpensDetails()
        {
            var browser = new Mock<IBrowser>();

            var detailsUrl = "http://example.com/details";
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.DetailsUrl).Returns(detailsUrl);

            string downloadUrl = null;
            release
                .Setup(r => r.TryGetDownloadUrl(Install.ProcessArchitecture, out downloadUrl))
                .Returns(false);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(true, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateDialog("Yes, download now"),
                browser.Object);

            command.PromptForAction(release.Object);

            browser.Verify(b => b.Navigate(detailsUrl), Times.Once);
        }

        [Test]
        public void WhenUserSelectsMoreDetails_ThenPromptForActionOpensDetails()
        {
            var browser = new Mock<IBrowser>();

            var downloadUrl = "http://example.com/download";
            var detailsUrl = "http://example.com/details";

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.DetailsUrl).Returns(detailsUrl);
            release
                .Setup(r => r.TryGetDownloadUrl(Install.ProcessArchitecture, out downloadUrl))
                .Returns(true);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(true, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateDialog("Show release notes"),
                browser.Object);

            command.PromptForAction(release.Object);

            browser.Verify(b => b.Navigate(detailsUrl), Times.Once);
        }

        [Test]
        public void WhenUserSelectsLater_ThenPromptForActionReturns()
        {
            var browser = new Mock<IBrowser>();

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(true, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateDialog("No, download later"),
                browser.Object);

            command.PromptForAction(new Mock<IRelease>().Object);

            browser.Verify(b => b.Navigate(It.IsAny<string>()), Times.Never);
        }

        //---------------------------------------------------------------------
        // PromptForAction - survey.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReleaseHasNoSurvey_ThenPromptForActionReturns()
        {
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Survey).Returns((IReleaseSurvey)null);

            var dialog = new Mock<ITaskDialog>();

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(false, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                dialog.Object,
                new Mock<IBrowser>().Object)
            {
                EnableSurveys = true
            };

            command.PromptForAction(release.Object);

            dialog.Verify(b => b.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<TaskDialogParameters>()), 
                Times.Never);
        }

        [Test]
        public void WhenReleaseHasSurveyButSurveysAreDisabled_ThenPromptForActionReturns()
        {
            var survey = new Mock<IReleaseSurvey>();
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Survey).Returns(survey.Object);

            var dialog = new Mock<ITaskDialog>();

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(false, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                dialog.Object,
                new Mock<IBrowser>().Object)
            {
                EnableSurveys = false
            };

            command.PromptForAction(release.Object);

            dialog.Verify(b => b.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<TaskDialogParameters>()),
                Times.Never);
        }

        [Test]
        public void WhenReleaseHasSurveyButSurveysWasTakenBefore_ThenPromptForActionReturns()
        {
            var lastVersion = new Version("2.1.3");

            var survey = new Mock<IReleaseSurvey>();
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(lastVersion);
            release.SetupGet(r => r.Survey).Returns(survey.Object);

            var dialog = new Mock<ITaskDialog>();

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(false, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                dialog.Object,
                new Mock<IBrowser>().Object)
            {
                EnableSurveys = true,
                LastSurveyVersion = lastVersion
            };

            command.PromptForAction(release.Object);

            dialog.Verify(b => b.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<TaskDialogParameters>()),
                Times.Never);
        }

        [Test]
        public void WhenReleaseHasSurvey_ThenPromptForActionShowsDialog()
        {
            var survey = new Mock<IReleaseSurvey>();
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version("2.1.3"));
            release.SetupGet(r => r.Survey).Returns(survey.Object);

            var dialog = new Mock<ITaskDialog>();

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(false, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                dialog.Object,
                new Mock<IBrowser>().Object)
            {
                EnableSurveys = true,
                LastSurveyVersion = new Version("1.0")
            };

            command.PromptForAction(release.Object);

            dialog.Verify(b => b.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<TaskDialogParameters>()),
                Times.Once);
        }

        [Test]
        public void WhenUserOpensSurvey_ThenPromptForActionUpdatesLastSurveyVersion()
        {
            var survey = new Mock<IReleaseSurvey>();
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version("2.1.3"));
            release.SetupGet(r => r.Survey).Returns(survey.Object);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(false, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateDialog("Start survey"),
                new Mock<IBrowser>().Object)
            {
                EnableSurveys = true,
                LastSurveyVersion = new Version("1.0")
            };

            command.PromptForAction(release.Object);

            Assert.AreEqual(release.Object.TagVersion, command.LastSurveyVersion);
        }

        //---------------------------------------------------------------------
        // Execute.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPolicyUsesNormalOrCriticalTrack_ThenExecuteReadsCanaryFeed(
            [Values(ReleaseTrack.Normal, ReleaseTrack.Critical)] ReleaseTrack track)
        {
            var feed = new Mock<IReleaseFeed>();
            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(false, track),
                feed.Object,
                CreateCancelledDialog(),
                new Mock<IBrowser>().Object);

            command.Execute(null, CancellationToken.None);

            feed.Verify(f => f.FindLatestReleaseAsync(
                ReleaseFeedOptions.None, 
                CancellationToken.None), 
                Times.Once);
        }

        [Test]
        public void WhenPolicyUsesCanaryTrack_ThenExecuteReadsCanaryFeed()
        {
            var feed = new Mock<IReleaseFeed>();
            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreatePolicy(false, ReleaseTrack.Canary),
                feed.Object,
                CreateCancelledDialog(),
                new Mock<IBrowser>().Object);

            command.Execute(null, CancellationToken.None);

            feed.Verify(f => f.FindLatestReleaseAsync(
                ReleaseFeedOptions.IncludeCanaryReleases,
                CancellationToken.None),
                Times.Once);
        }
    }
}
