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
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestUpdatePolicy
    {
        private Mock<IAuthorization> CreateAuthorization(string email)
        {
            var session = new Mock<IGaiaOidcSession>();
            session.SetupGet(a => a.Email).Returns(email);

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Session).Returns(session.Object);
            return authorization;
        }

        private static Install CreateInstall()
        {
            return new Install(Install.DefaultBaseKeyPath);
        }

        //---------------------------------------------------------------------
        // FollowedTracks.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUserNotInternal_ThenFollowedTracksIsUnchanged(
            [Values(
                ReleaseTrack.Critical,
                ReleaseTrack.Normal,
                ReleaseTrack.Canary)] ReleaseTrack followedTracks)
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                followedTracks);

            Assert.AreEqual(followedTracks, policy.FollowedTrack);
        }

        [Test]
        public void WhenUserIsInternal_ThenFollowedTracksIncludesCanary(
            [Values(
                "_@GOOGLE.com",
                "_@x.altostrat.COM")] string email)
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization(email).Object,
                ReleaseTrack.Critical);

            Assert.AreEqual(ReleaseTrack.Canary, policy.FollowedTrack);
        }

        //---------------------------------------------------------------------
        // DaysBetweenUpdateChecks.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUserOnCanaryTrack_ThenDaysBetweenUpdateChecksIsLow()
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Canary);

            Assert.AreEqual(1, policy.DaysBetweenUpdateChecks);
        }

        [Test]
        public void WhenUserOnNormalTrack_ThenDaysBetweenUpdateChecksIsHigh()
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Normal);

            Assert.AreEqual(10, policy.DaysBetweenUpdateChecks);
        }

        //---------------------------------------------------------------------
        // GetReleaseTrack.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDescriptionContainsNoTag_ThenGetReleaseTrackReturnsNormal(
            [Values(null, "", "some description")] string description)
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Critical);

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            var track = policy.GetReleaseTrack(release.Object);

            Assert.AreEqual(ReleaseTrack.Normal, track);
        }

        [Test]
        public void WhenDescriptionContainsCriticalTag_ThenGetReleaseTrackReturnsCritical()
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Critical);

            var description = "This release is [track:critical]!!1!";

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            var track = policy.GetReleaseTrack(release.Object);

            Assert.AreEqual(ReleaseTrack.Critical, track);
        }

        [Test]
        public void WhenDescriptionContainsCanaryTag_ThenGetReleaseTrackReturnsCanary()
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Critical);

            var description = "This release is on the [track:Canary] track!!1!";

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            var track = policy.GetReleaseTrack(release.Object);

            Assert.AreEqual(ReleaseTrack.Canary, track);
        }

        //---------------------------------------------------------------------
        // IsUpdateAdvised.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReleaseHasNoVersion_ThenIsUpdateAdvisedReturnsFalse()
        {
            var release = new Mock<IRelease>();
            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Canary);

            Assert.IsFalse(policy.IsUpdateAdvised(release.Object));
        }

        [Test]
        public void WhenReleaseOlderThanInstalled_ThenIsUpdateAdvisedReturnsFalse()
        {
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version(1, 0));

            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Canary);

            Assert.IsFalse(policy.IsUpdateAdvised(release.Object));
        }

        [Test]
        public void WhenReleaseSameAsInstalled_ThenIsUpdateAdvisedReturnsFalse()
        {
            var install = CreateInstall();

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(install.CurrentVersion);

            var policy = new UpdatePolicy(
                install,
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Canary);

            Assert.IsFalse(policy.IsUpdateAdvised(release.Object));
        }

        [Test]
        public void WhenReleaseNewerAndUserOnCanaryTrack_ThenIsUpdateAdvisedReturnsTrueForCanaryTrackAndBelow()
        {
            var Policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Canary);

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var CanaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            CanaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            CanaryRelease.SetupGet(r => r.Description).Returns("[track:Canary]");

            Assert.IsTrue(Policy.IsUpdateAdvised(criticalRelease.Object));
            Assert.IsTrue(Policy.IsUpdateAdvised(normalRelease.Object));
            Assert.IsTrue(Policy.IsUpdateAdvised(CanaryRelease.Object));
        }

        [Test]
        public void WhenReleaseNewerAndUserOnNormalTrack_ThenIsUpdateAdvisedReturnsTrueForNormalAndBelow()
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Normal);

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var CanaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            CanaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            CanaryRelease.SetupGet(r => r.Description).Returns("[track:Canary]");

            Assert.IsTrue(policy.IsUpdateAdvised(criticalRelease.Object));
            Assert.IsTrue(policy.IsUpdateAdvised(normalRelease.Object));
            Assert.IsFalse(policy.IsUpdateAdvised(CanaryRelease.Object));
        }

        [Test]
        public void WhenReleaseNewerAndUserOnCriticalTrack_ThenIsUpdateAdvisedReturnsTrueForCriticalOnly()
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Critical);

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var CanaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            CanaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            CanaryRelease.SetupGet(r => r.Description).Returns("[track:Canary]");

            Assert.IsTrue(policy.IsUpdateAdvised(criticalRelease.Object));
            Assert.IsFalse(policy.IsUpdateAdvised(normalRelease.Object));
            Assert.IsFalse(policy.IsUpdateAdvised(CanaryRelease.Object));
        }

        //---------------------------------------------------------------------
        // IsUpdateCheckDue.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotEnoughDaysElapsed_ThenIsUpdateCheckDueReturnsFalse()
        {
            var clock = SystemClock.Default;
            var now = clock.UtcNow;

            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Normal);

            Assert.IsFalse(policy.IsUpdateCheckDue(clock, now));
            Assert.IsFalse(policy.IsUpdateCheckDue(clock, now.AddYears(1)));
            Assert.IsFalse(policy.IsUpdateCheckDue(clock, now.AddDays(-policy.DaysBetweenUpdateChecks).AddMinutes(1)));
            Assert.IsFalse(policy.IsUpdateCheckDue(clock, now.AddDays(-policy.DaysBetweenUpdateChecks + 1)));
        }

        [Test]
        public void WhenTooManyDaysElapsed_ThenIsUpdateCheckDueReturnsTrue()
        {
            var clock = SystemClock.Default;
            var now = clock.UtcNow;

            var policy = new UpdatePolicy(
                CreateInstall(),
                CreateAuthorization("_@example.com").Object,
                ReleaseTrack.Normal);

            Assert.IsTrue(policy.IsUpdateCheckDue(clock, now.AddDays(-policy.DaysBetweenUpdateChecks)));
            Assert.IsTrue(policy.IsUpdateCheckDue(clock, now.AddDays(-policy.DaysBetweenUpdateChecks - 1)));
        }
    }
}
