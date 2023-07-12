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

using Google.Apis.Util;
using Google.Solutions.Apis.Auth;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Host.Adapters;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Moq;
using NUnit.Framework;
using System;
using static Google.Solutions.IapDesktop.Application.Host.Adapters.GithubAdapter;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestUpdatePolicy
    {
        private Mock<IAuthorization> CreateAuthorization(string email)
        {
            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Email).Returns(email);
            return authorization;
        }

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
        // GetReleaseTrack.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDescriptionContainsNoTag_ThenGetReleaseTrackReturnsNormal(
            [Values(null, "", "some description")] string description)
        {
            var authorization = CreateAuthorization("bob@example.com");
            var policy = new UpdatePolicy(
                authorization.Object,
                CreateInstall(),
                CreateClock(DateTime.Now));

            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            Assert.AreEqual(ReleaseTrack.Normal, policy.GetReleaseTrack(release.Object));
        }

        [Test]
        public void WhenDescriptionContainsCriticalTag_ThenGetReleaseTrackReturnsCritical()
        {
            var authorization = CreateAuthorization("bob@example.com");
            var policy = new UpdatePolicy(
                authorization.Object,
                CreateInstall(),
                CreateClock(DateTime.Now));

            var description = "This release is [track:critical]!!1!";

            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            Assert.AreEqual(ReleaseTrack.Critical, policy.GetReleaseTrack(release.Object));
        }

        [Test]
        public void WhenDescriptionContainsRapidTag_ThenGetReleaseTrackReturnsRapid()
        {
            var authorization = CreateAuthorization("bob@example.com");
            var policy = new UpdatePolicy(
                authorization.Object,
                CreateInstall(),
                CreateClock(DateTime.Now));

            var description = "This release is on the [track:rapid] track!!1!";

            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            Assert.AreEqual(ReleaseTrack.Rapid, policy.GetReleaseTrack(release.Object));
        }

        [Test]
        public void WhenDescriptionContainsOptionalTag_ThenGetReleaseTrackReturnsOptional()
        {
            var authorization = CreateAuthorization("bob@example.com");
            var policy = new UpdatePolicy(
                authorization.Object,
                CreateInstall(),
                CreateClock(DateTime.Now));

            var description = "This release is [track:optional]";

            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            Assert.AreEqual(ReleaseTrack.Optional, policy.GetReleaseTrack(release.Object));
        }

        //---------------------------------------------------------------------
        // FollowedTracks.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUserFromListedDomains_ThenFollowedTracksIncludesRapid(
            [Values(
                "_@gmail.com",
                "_@GOOGLE.com",
                "_@x.joonix.Net",
                "_@x.altostrat.COM")] string email)
        {

            var authorization = CreateAuthorization(email);
            var policy = new UpdatePolicy(
                authorization.Object,
                CreateInstall(),
                CreateClock(DateTime.Now));

            Assert.AreEqual(
                ReleaseTrack.Critical | ReleaseTrack.Normal | ReleaseTrack.Rapid,
                policy.FollowedTracks);
        }

        [Test]
        public void WhenUserFromOtherDomains_ThenFollowedTracksExcludesRapid(
            [Values(
                "_@x.gmail.com",
                "_@example.com")] string email)
        {
            var authorization = CreateAuthorization(email);
            var policy = new UpdatePolicy(
                authorization.Object,
                CreateInstall(),
                CreateClock(DateTime.Now));

            Assert.AreEqual(
                ReleaseTrack.Critical | ReleaseTrack.Normal,
                policy.FollowedTracks);
        }

        //---------------------------------------------------------------------
        // IsUpdateAdvised.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReleaseHasNoVersion_ThenIsUpdateAdvisedReturnsFalse()
        {
            var release = new Mock<IGitHubRelease>();
            var policy = new UpdatePolicy(
                CreateAuthorization("_@example.net").Object,
                CreateInstall(),
                CreateClock(DateTime.Now));

            Assert.IsFalse(policy.IsUpdateAdvised(release.Object));
        }

        [Test]
        public void WhenReleaseOlderThanInstalled_ThenIsUpdateAdvisedReturnsFalse()
        {
            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version(1, 0));

            var policy = new UpdatePolicy(
                CreateAuthorization("_@example.net").Object,
                CreateInstall(),
                CreateClock(DateTime.Now));

            Assert.IsFalse(policy.IsUpdateAdvised(release.Object));
        }

        [Test]
        public void WhenReleaseSameAsInstalled_ThenIsUpdateAdvisedReturnsFalse()
        {
            var install = CreateInstall();

            var release = new Mock<IGitHubRelease>();
            release.SetupGet(r => r.TagVersion).Returns(install.CurrentVersion);

            var policy = new UpdatePolicy(
                CreateAuthorization("_@example.net").Object,
                install,
                CreateClock(DateTime.Now));

            Assert.IsFalse(policy.IsUpdateAdvised(release.Object));

        }

        [Test]
        public void WhenReleaseNewerAndUserOnRapidTrack_ThenIsUpdateAdvisedReturnsTrueBasedOnTrack()
        {
            var rapidPolicy = new UpdatePolicy(
                CreateAuthorization("_@x.joonix.Net").Object,
                CreateInstall(),
                CreateClock(DateTime.Now));

            var normalRelease = new Mock<IGitHubRelease>();
            var criticalRelease = new Mock<IGitHubRelease>();
            var optionalRelease = new Mock<IGitHubRelease>();
            var rapidRelease = new Mock<IGitHubRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            optionalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            rapidRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            optionalRelease.SetupGet(r => r.Description).Returns("[track:optional]");
            rapidRelease.SetupGet(r => r.Description).Returns("[track:rapid]");

            Assert.IsTrue(rapidPolicy.IsUpdateAdvised(normalRelease.Object));
            Assert.IsTrue(rapidPolicy.IsUpdateAdvised(criticalRelease.Object));
            Assert.IsTrue(rapidPolicy.IsUpdateAdvised(rapidRelease.Object));
            Assert.IsFalse(rapidPolicy.IsUpdateAdvised(optionalRelease.Object));
        }

        [Test]
        public void WhenReleaseNewerAndUserOnNormalTrack_ThenIsUpdateAdvisedReturnsTrueBasedOnTrack()
        {
            var normalPolicy = new UpdatePolicy(
                CreateAuthorization("_@example.net").Object,
                CreateInstall(),
                CreateClock(DateTime.Now));

            var normalRelease = new Mock<IGitHubRelease>();
            var criticalRelease = new Mock<IGitHubRelease>();
            var optionalRelease = new Mock<IGitHubRelease>();
            var rapidRelease = new Mock<IGitHubRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            optionalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            rapidRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            optionalRelease.SetupGet(r => r.Description).Returns("[track:optional]");
            rapidRelease.SetupGet(r => r.Description).Returns("[track:rapid]");

            Assert.IsTrue(normalPolicy.IsUpdateAdvised(normalRelease.Object));
            Assert.IsTrue(normalPolicy.IsUpdateAdvised(criticalRelease.Object));
            Assert.IsFalse(normalPolicy.IsUpdateAdvised(rapidRelease.Object));
            Assert.IsFalse(normalPolicy.IsUpdateAdvised(optionalRelease.Object));
        }

        //---------------------------------------------------------------------
        // IsUpdateCheckDue.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotEnoughDaysElapsed_ThenIsUpdateCheckDueReturnsFalse()
        {
            var now = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var authorization = CreateAuthorization("bob@example.com");
            var policy = new UpdatePolicy(
                authorization.Object,
                CreateInstall(),
                CreateClock(now));

            Assert.IsFalse(policy.IsUpdateCheckDue(now));
            Assert.IsFalse(policy.IsUpdateCheckDue(now.AddYears(1)));
            Assert.IsFalse(policy.IsUpdateCheckDue(now.AddDays(-UpdatePolicy.DaysBetweenUpdateChecks).AddMinutes(1)));
            Assert.IsFalse(policy.IsUpdateCheckDue(now.AddDays(-UpdatePolicy.DaysBetweenUpdateChecks + 1)));
        }

        [Test]
        public void WhenTooManyDaysElapsed_ThenIsUpdateCheckDueReturnsTrue()
        {
            var now = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var authorization = CreateAuthorization("bob@example.com");
            var policy = new UpdatePolicy(
                authorization.Object,
                CreateInstall(),
                CreateClock(now));

            Assert.IsTrue(policy.IsUpdateCheckDue(now.AddDays(-UpdatePolicy.DaysBetweenUpdateChecks)));
            Assert.IsTrue(policy.IsUpdateCheckDue(now.AddDays(-UpdatePolicy.DaysBetweenUpdateChecks - 1)));
        }
    }
}
