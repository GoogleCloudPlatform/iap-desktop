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
using Google.Solutions.IapDesktop.Application.Profile;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Profile
{
    [TestFixture]
    public class TestUpdatePolicy
    {
        private static Install CreateInstall()
        {
            return new Install(Install.DefaultBaseKeyPath);
        }

        //---------------------------------------------------------------------
        // DaysBetweenUpdateChecks.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUserOnCanaryTrack_ThenDaysBetweenUpdateChecksIsLow()
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                SystemClock.Default,
                ReleaseTrack.Canary);

            Assert.AreEqual(1, policy.DaysBetweenUpdateChecks);
        }

        [Test]
        public void WhenUserOnNormalTrack_ThenDaysBetweenUpdateChecksIsHigh()
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                SystemClock.Default,
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
                SystemClock.Default,
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
                SystemClock.Default,
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
                SystemClock.Default,
                ReleaseTrack.Critical);

            var description = "This release is on the [track:canary] track!!1!";

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
                SystemClock.Default,
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
                SystemClock.Default,
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
                SystemClock.Default,
                ReleaseTrack.Canary);

            Assert.IsFalse(policy.IsUpdateAdvised(release.Object));
        }

        [Test]
        public void WhenReleaseNewerAndUserOnCanaryTrack_ThenIsUpdateAdvisedReturnsTrueForCanaryTrackAndBelow()
        {
            var Policy = new UpdatePolicy(
                CreateInstall(),
                SystemClock.Default,
                ReleaseTrack.Canary);

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var canaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            canaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            canaryRelease.SetupGet(r => r.Description).Returns("[track:canary]");

            Assert.IsTrue(Policy.IsUpdateAdvised(criticalRelease.Object));
            Assert.IsTrue(Policy.IsUpdateAdvised(normalRelease.Object));
            Assert.IsTrue(Policy.IsUpdateAdvised(canaryRelease.Object));
        }

        [Test]
        public void WhenReleaseNewerAndUserOnNormalTrack_ThenIsUpdateAdvisedReturnsTrueForNormalAndBelow()
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                SystemClock.Default,
                ReleaseTrack.Normal);

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var canaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            canaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            canaryRelease.SetupGet(r => r.Description).Returns("[track:canary]");

            Assert.IsTrue(policy.IsUpdateAdvised(criticalRelease.Object));
            Assert.IsTrue(policy.IsUpdateAdvised(normalRelease.Object));
            Assert.IsFalse(policy.IsUpdateAdvised(canaryRelease.Object));
        }

        [Test]
        public void WhenReleaseNewerAndUserOnCriticalTrack_ThenIsUpdateAdvisedReturnsTrueForCriticalOnly()
        {
            var policy = new UpdatePolicy(
                CreateInstall(),
                SystemClock.Default,
                ReleaseTrack.Critical);

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var canaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            canaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            canaryRelease.SetupGet(r => r.Description).Returns("[track:canary]");

            Assert.IsTrue(policy.IsUpdateAdvised(criticalRelease.Object));
            Assert.IsFalse(policy.IsUpdateAdvised(normalRelease.Object));
            Assert.IsFalse(policy.IsUpdateAdvised(canaryRelease.Object));
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
                clock,
                ReleaseTrack.Normal);

            Assert.IsFalse(policy.IsUpdateCheckDue(now));
            Assert.IsFalse(policy.IsUpdateCheckDue(now.AddYears(1)));
            Assert.IsFalse(policy.IsUpdateCheckDue(now.AddDays(-policy.DaysBetweenUpdateChecks).AddMinutes(1)));
            Assert.IsFalse(policy.IsUpdateCheckDue(now.AddDays(-policy.DaysBetweenUpdateChecks + 1)));
        }

        [Test]
        public void WhenTooManyDaysElapsed_ThenIsUpdateCheckDueReturnsTrue()
        {
            var clock = SystemClock.Default;
            var now = clock.UtcNow;

            var policy = new UpdatePolicy(
                CreateInstall(),
                clock,
                ReleaseTrack.Normal);

            Assert.IsTrue(policy.IsUpdateCheckDue(now.AddDays(-policy.DaysBetweenUpdateChecks)));
            Assert.IsTrue(policy.IsUpdateCheckDue(now.AddDays(-policy.DaysBetweenUpdateChecks - 1)));
        }
    }
}
