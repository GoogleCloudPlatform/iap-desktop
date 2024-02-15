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
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Apis.Auth;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Moq;
using NUnit.Framework;
using System;
using Google.Solutions.IapDesktop.Application.Profile.Settings;

namespace Google.Solutions.IapDesktop.Application.Test.Profile
{
    [TestFixture]
    public class TestUpdatePolicy
    {
        private static Install CreateInstall()
        {
            return new Install(Install.DefaultBaseKeyPath);
        }

        private static Mock<IGaiaOidcSession> CreateGaiaSession(string email)
        {
            var session = new Mock<IGaiaOidcSession>();
            session.SetupGet(s => s.Email).Returns(email);

            return session;
        }

        private static Mock<IWorkforcePoolSession> CreateWorkforceSession()
        {
            return new Mock<IWorkforcePoolSession>();
        }

        private static Mock<IAuthorization> CreateAuthorization(IOidcSession session)
        {
            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Session).Returns(session);
            return authorization;
        }

        private static Mock<IRepository<IApplicationSettings>> CreateSettingsRepository(
            bool updatesEnabled)
        {
            var updateSetting = new Mock<IBoolSetting>();
            updateSetting.SetupGet(s => s.BoolValue).Returns(updatesEnabled);

            var settings = new Mock<IApplicationSettings>();
            settings.SetupGet(s => s.IsUpdateCheckEnabled).Returns(updateSetting.Object);

            var settingsRepository = new Mock<IRepository<IApplicationSettings>>();
            settingsRepository.Setup(s => s.GetSettings()).Returns(settings.Object);

            return settingsRepository;
        }

        //---------------------------------------------------------------------
        // FollowedTrack.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUpdateCheckDisabled_ThenFollowedTrackIsCritical()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(false).Object,
                CreateAuthorization(CreateWorkforceSession().Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Critical, policy.FollowedTrack);
        }

        [Test]
        public void WhenUserSignedInWithWorkforceIdentity_ThenFollowedTrackIsIsNormal()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateWorkforceSession().Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Normal, policy.FollowedTrack);
        }

        [Test]
        public void WhenUserSignedInWithGaia_ThenFollowedTrackIsNormal()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Normal, policy.FollowedTrack);
        }

        [Test]
        public void WhenUserIsGoogler_ThenFollowedTrackIsCanary(
            [Values("_@google.com", "_@test.altostrat.com")] string email)
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession(email).Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Canary, policy.FollowedTrack);
        }

        //---------------------------------------------------------------------
        // DaysBetweenUpdateChecks.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUserIsGoogler_ThenDaysBetweenUpdateChecksIsLow(
            [Values("_@google.com", "_@test.altostrat.com")] string email)
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession(email).Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.AreEqual(1, policy.DaysBetweenUpdateChecks);
        }

        [Test]
        public void WhenUserNotGoogler_ThenDaysBetweenUpdateChecksIsHigh()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.AreEqual(10, policy.DaysBetweenUpdateChecks);
        }

        //---------------------------------------------------------------------
        // GetReleaseTrackForRelease.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDescriptionContainsNoTag_ThenGetReleaseTrackReturnsNormal(
            [Values(null, "", "some description")] string description)
        {
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            var track = UpdatePolicy.GetReleaseTrackForRelease(release.Object);

            Assert.AreEqual(ReleaseTrack.Normal, track);
        }

        [Test]
        public void WhenDescriptionContainsCriticalTag_ThenGetReleaseTrackReturnsCritical()
        {
            var description = "This release is [track:critical]!!1!";

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            var track = UpdatePolicy.GetReleaseTrackForRelease(release.Object);

            Assert.AreEqual(ReleaseTrack.Critical, track);
        }

        [Test]
        public void WhenPrerelease_ThenGetReleaseTrackReturnsCanary()
        {
            var description = "This release is on the canary track!!1!";

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Description).Returns(description);
            release.SetupGet(r => r.IsCanaryRelease).Returns(true);

            var track = UpdatePolicy.GetReleaseTrackForRelease(release.Object);

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
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@google.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.IsFalse(policy.IsUpdateAdvised(release.Object));
        }

        [Test]
        public void WhenReleaseOlderThanInstalled_ThenIsUpdateAdvisedReturnsFalse()
        {
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version(1, 0));

            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@google.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.IsFalse(policy.IsUpdateAdvised(release.Object));
        }

        [Test]
        public void WhenReleaseSameAsInstalled_ThenIsUpdateAdvisedReturnsFalse()
        {
            var install = CreateInstall();

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(install.CurrentVersion);

            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@google.com").Object).Object,
                install,
                SystemClock.Default);

            Assert.IsFalse(policy.IsUpdateAdvised(release.Object));
        }

        [Test]
        public void WhenReleaseNewerAndUserOnCanaryTrack_ThenIsUpdateAdvisedReturnsTrueForCanaryTrackAndBelow()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@google.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Canary, policy.FollowedTrack);

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var canaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            canaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            canaryRelease.SetupGet(r => r.IsCanaryRelease).Returns(true);

            Assert.IsTrue(policy.IsUpdateAdvised(criticalRelease.Object));
            Assert.IsTrue(policy.IsUpdateAdvised(normalRelease.Object));
            Assert.IsTrue(policy.IsUpdateAdvised(canaryRelease.Object));
        }

        [Test]
        public void WhenReleaseNewerAndUserOnNormalTrack_ThenIsUpdateAdvisedReturnsTrueForNormalAndBelow()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Normal, policy.FollowedTrack);

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var canaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            canaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            canaryRelease.SetupGet(r => r.IsCanaryRelease).Returns(true);

            Assert.IsTrue(policy.IsUpdateAdvised(criticalRelease.Object));
            Assert.IsTrue(policy.IsUpdateAdvised(normalRelease.Object));
            Assert.IsFalse(policy.IsUpdateAdvised(canaryRelease.Object));
        }

        [Test]
        public void WhenReleaseNewerAndUserOnCriticalTrack_ThenIsUpdateAdvisedReturnsTrueForCriticalOnly()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(false).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Critical, policy.FollowedTrack);

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var canaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            canaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            canaryRelease.SetupGet(r => r.IsCanaryRelease).Returns(true);

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
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                CreateInstall(),
                clock);

            Assert.AreEqual(ReleaseTrack.Normal, policy.FollowedTrack);

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
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                CreateInstall(),
                clock);

            Assert.AreEqual(ReleaseTrack.Normal, policy.FollowedTrack);

            Assert.IsTrue(policy.IsUpdateCheckDue(now.AddDays(-policy.DaysBetweenUpdateChecks)));
            Assert.IsTrue(policy.IsUpdateCheckDue(now.AddDays(-policy.DaysBetweenUpdateChecks - 1)));
        }
    }
}
