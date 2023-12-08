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
using Google.Solutions.IapDesktop.Application.ToolWindows.Update;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.Platform.Net;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace Google.Solutions.IapDesktop.Application.Test.ToolWindows.Update
{
    [TestFixture]
    public class TestCheckForUpdateCommand
    {
        private static IUpdatePolicyFactory CreateUpdatePolicy(
            bool adviceAllUpdates)
        {
            var policy = new Mock<IUpdatePolicy>();
            policy
                .Setup(p => p.IsUpdateAdvised(It.IsAny<IRelease>()))
                .Returns(adviceAllUpdates);

            var policyFactory = new Mock<IUpdatePolicyFactory>();
            policyFactory
                .Setup(f => f.GetPolicy(It.IsAny<ReleaseTrack>()))
                .Returns(policy.Object);

            return policyFactory.Object;
        }

        private static ITaskDialog CreateDialog(int result)
        {
            bool save;
            var dialog = new Mock<ITaskDialog>();
            dialog
                .Setup(d => d.ShowOptionsTaskDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<IntPtr>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IList<string>>(),
                    It.IsAny<string>(),
                    out save))
                .Returns(result);
                    
            return dialog.Object;
        }

        private static ITaskDialog CreateCancelledDialog()
        {
            bool save;
            var dialog = new Mock<ITaskDialog>();
            dialog
                .Setup(d => d.ShowOptionsTaskDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<IntPtr>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IList<string>>(),
                    It.IsAny<string>(),
                    out save))
                .Throws(new OperationCanceledException());

            return dialog.Object;
        }

        //---------------------------------------------------------------------
        // IsAutomatedUpdateCheckDue.
        //---------------------------------------------------------------------

        [Test]
        public void IsAutomatedUpdateCheckDue()
        {
            var policy = new Mock<IUpdatePolicy>();
            policy
                .Setup(p => p.IsUpdateCheckDue(It.IsAny<DateTime>()))
                .Returns(true);

            var policyFactory = new Mock<IUpdatePolicyFactory>();
            policyFactory
                .Setup(f => f.GetPolicy(ReleaseTrack.Critical))
                .Returns(policy.Object);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policyFactory.Object,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object,
                ReleaseTrack.Critical);

            var time = DateTime.Now;
            Assert.IsTrue(command.IsAutomatedUpdateCheckDue(time));

            policy.Verify(p => p.IsUpdateCheckDue(time), Times.Once);
        }

        //---------------------------------------------------------------------
        // PromptForDownload.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReleaseIsNull_ThenPromptForDownloadReturns()
        {
            var policy = new Mock<IUpdatePolicy>();
            var policyFactory = new Mock<IUpdatePolicyFactory>();
            policyFactory
                .Setup(f => f.GetPolicy(ReleaseTrack.Critical))
                .Returns(policy.Object);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policyFactory.Object,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object,
            ReleaseTrack.Critical);

            command.PromptForDownload(null);

            policy.Verify(p => p.IsUpdateAdvised(It.IsAny<IRelease>()), Times.Never);
        }

        [Test]
        public void WhenPolicyDoesNotAdviseUpdate_ThenPromptForDownloadReturns()
        {
            var policy = new Mock<IUpdatePolicy>();
            policy
                .Setup(p => p.IsUpdateAdvised(It.IsAny<IRelease>()))
                .Returns(false);

            var policyFactory = new Mock<IUpdatePolicyFactory>();
            policyFactory
                .Setup(f => f.GetPolicy(ReleaseTrack.Critical))
                .Returns(policy.Object);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policyFactory.Object,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object,
            ReleaseTrack.Critical);

            command.PromptForDownload(new Mock<IRelease>().Object);

            policy.Verify(p => p.IsUpdateAdvised(It.IsAny<IRelease>()), Times.Once);
        }

        [Test]
        public void WhenUserCancels_ThenPromptForDownloadReturns()
        {
            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicy(true),
                new Mock<IReleaseFeed>().Object,
                CreateCancelledDialog(),
                new Mock<IBrowser>().Object,
            ReleaseTrack.Critical);

            command.PromptForDownload(new Mock<IRelease>().Object);
        }

        [Test]
        public void WhenUserSelectsDownload_ThenPromptForDownloadOpensDownload()
        {
            var browser = new Mock<IBrowser>();

            var downloadUrl = "http://example.com/download";
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.DownloadUrl).Returns(downloadUrl);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicy(true),
                new Mock<IReleaseFeed>().Object,
                CreateDialog(0),
                browser.Object,
            ReleaseTrack.Critical);

            command.PromptForDownload(release.Object);

            browser.Verify(b => b.Navigate(downloadUrl), Times.Once);
        }

        [Test]
        public void WhenUserSelectsMoreDetails_ThenPromptForDownloadOpensDetails()
        {
            var browser = new Mock<IBrowser>();

            var downloadUrl = "http://example.com/download";
            var detailsUrl = "http://example.com/details";
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.DownloadUrl).Returns(downloadUrl);
            release.SetupGet(r => r.DetailsUrl).Returns(detailsUrl);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicy(true),
                new Mock<IReleaseFeed>().Object,
                CreateDialog(1),
                browser.Object,
            ReleaseTrack.Critical);

            command.PromptForDownload(release.Object);

            browser.Verify(b => b.Navigate(detailsUrl), Times.Once);
        }

        [Test]
        public void WhenUserSelectsLater_ThenPromptForDownloadReturns()
        {
            var browser = new Mock<IBrowser>();

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicy(true),
                new Mock<IReleaseFeed>().Object,
                CreateDialog(2),
                browser.Object,
            ReleaseTrack.Critical);

            command.PromptForDownload(new Mock<IRelease>().Object);

            browser.Verify(b => b.Navigate(It.IsAny<string>()), Times.Never);
        }
    }
}
