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
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
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
            var updateSetting = new Mock<ISetting<bool>>();
            updateSetting.SetupGet(s => s.Value).Returns(updatesEnabled);

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
        public void FollowedTrack_WhenUpdateCheckDisabled_ThenFollowedTrackIsCritical()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(false).Object,
                CreateAuthorization(CreateWorkforceSession().Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.That(policy.FollowedTrack, Is.EqualTo(ReleaseTrack.Critical));
        }

        [Test]
        public void FollowedTrack_WhenUserSignedInWithWorkforceIdentity_ThenFollowedTrackIsIsNormal()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateWorkforceSession().Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.That(policy.FollowedTrack, Is.EqualTo(ReleaseTrack.Normal));
        }

        [Test]
        public void FollowedTrack_WhenUserSignedInWithGaia_ThenFollowedTrackIsNormal()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.That(policy.FollowedTrack, Is.EqualTo(ReleaseTrack.Normal));
        }

        [Test]
        public void FollowedTrack_WhenUserIsGoogler_ThenFollowedTrackIsCanary(
            [Values("_@google.com", "_@test.altostrat.com")] string email)
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession(email).Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.That(policy.FollowedTrack, Is.EqualTo(ReleaseTrack.Canary));
        }

        //---------------------------------------------------------------------
        // DaysBetweenUpdateChecks.
        //---------------------------------------------------------------------

        [Test]
        public void DaysBetweenUpdateChecks_WhenUserIsGoogler_ThenDaysBetweenUpdateChecksIsLow(
            [Values("_@google.com", "_@test.altostrat.com")] string email)
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession(email).Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.That(policy.DaysBetweenUpdateChecks, Is.EqualTo(1));
        }

        [Test]
        public void DaysBetweenUpdateChecks_WhenUserNotGoogler_ThenDaysBetweenUpdateChecksIsHigh()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.That(policy.DaysBetweenUpdateChecks, Is.EqualTo(10));
        }

        //---------------------------------------------------------------------
        // GetReleaseTrackForRelease.
        //---------------------------------------------------------------------

        [Test]
        public void GetReleaseTrackForRelease_WhenDescriptionContainsNoTag_ThenGetReleaseTrackReturnsNormal(
            [Values(null, "", "some description")] string? description)
        {
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            var track = UpdatePolicy.GetReleaseTrackForRelease(release.Object);

            Assert.That(track, Is.EqualTo(ReleaseTrack.Normal));
        }

        [Test]
        public void GetReleaseTrackForRelease_WhenDescriptionContainsCriticalTag_ThenGetReleaseTrackReturnsCritical()
        {
            var description = "This release is [track:critical]!!1!";

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Description).Returns(description);

            var track = UpdatePolicy.GetReleaseTrackForRelease(release.Object);

            Assert.That(track, Is.EqualTo(ReleaseTrack.Critical));
        }

        [Test]
        public void GetReleaseTrackForRelease_WhenPrerelease_ThenGetReleaseTrackReturnsCanary()
        {
            var description = "This release is on the canary track!!1!";

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.Description).Returns(description);
            release.SetupGet(r => r.IsCanaryRelease).Returns(true);

            var track = UpdatePolicy.GetReleaseTrackForRelease(release.Object);

            Assert.That(track, Is.EqualTo(ReleaseTrack.Canary));
        }

        //---------------------------------------------------------------------
        // IsUpdateAdvised.
        //---------------------------------------------------------------------

        [Test]
        public void IsUpdateAdvised_WhenReleaseHasNoVersion_ThenIsUpdateAdvisedReturnsFalse()
        {
            var release = new Mock<IRelease>();
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@google.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.That(policy.IsUpdateAdvised(release.Object), Is.False);
        }

        [Test]
        public void IsUpdateAdvised_WhenReleaseOlderThanInstalled_ThenIsUpdateAdvisedReturnsFalse()
        {
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(new Version(1, 0));

            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@google.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.That(policy.IsUpdateAdvised(release.Object), Is.False);
        }

        [Test]
        public void IsUpdateAdvised_WhenReleaseSameAsInstalled_ThenIsUpdateAdvisedReturnsFalse()
        {
            var install = CreateInstall();

            var release = new Mock<IRelease>();
            release.SetupGet(r => r.TagVersion).Returns(install.CurrentVersion);

            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@google.com").Object).Object,
                install,
                SystemClock.Default);

            Assert.That(policy.IsUpdateAdvised(release.Object), Is.False);
        }

        [Test]
        public void IsUpdateAdvised_WhenReleaseNewerAndUserOnCanaryTrack_ThenIsUpdateAdvisedReturnsTrueForCanaryTrackAndBelow()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@google.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.That(policy.FollowedTrack, Is.EqualTo(ReleaseTrack.Canary));

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var canaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            canaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            canaryRelease.SetupGet(r => r.IsCanaryRelease).Returns(true);

            Assert.That(policy.IsUpdateAdvised(criticalRelease.Object), Is.True);
            Assert.That(policy.IsUpdateAdvised(normalRelease.Object), Is.True);
            Assert.That(policy.IsUpdateAdvised(canaryRelease.Object), Is.True);
        }

        [Test]
        public void IsUpdateAdvised_WhenReleaseNewerAndUserOnNormalTrack_ThenIsUpdateAdvisedReturnsTrueForNormalAndBelow()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.That(policy.FollowedTrack, Is.EqualTo(ReleaseTrack.Normal));

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var canaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            canaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            canaryRelease.SetupGet(r => r.IsCanaryRelease).Returns(true);

            Assert.That(policy.IsUpdateAdvised(criticalRelease.Object), Is.True);
            Assert.That(policy.IsUpdateAdvised(normalRelease.Object), Is.True);
            Assert.That(policy.IsUpdateAdvised(canaryRelease.Object), Is.False);
        }

        [Test]
        public void IsUpdateAdvised_WhenReleaseNewerAndUserOnCriticalTrack_ThenIsUpdateAdvisedReturnsTrueForCriticalOnly()
        {
            var policy = new UpdatePolicy(
                CreateSettingsRepository(false).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                CreateInstall(),
                SystemClock.Default);

            Assert.That(policy.FollowedTrack, Is.EqualTo(ReleaseTrack.Critical));

            var normalRelease = new Mock<IRelease>();
            var criticalRelease = new Mock<IRelease>();
            var canaryRelease = new Mock<IRelease>();

            normalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            criticalRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));
            canaryRelease.SetupGet(r => r.TagVersion).Returns(new Version(9, 0));

            normalRelease.SetupGet(r => r.Description).Returns("");
            criticalRelease.SetupGet(r => r.Description).Returns("[track:critical]");
            canaryRelease.SetupGet(r => r.IsCanaryRelease).Returns(true);

            Assert.That(policy.IsUpdateAdvised(criticalRelease.Object), Is.True);
            Assert.That(policy.IsUpdateAdvised(normalRelease.Object), Is.False);
            Assert.That(policy.IsUpdateAdvised(canaryRelease.Object), Is.False);
        }

        //---------------------------------------------------------------------
        // IsUpdateCheckDue.
        //---------------------------------------------------------------------

        [Test]
        public void IsUpdateCheckDue_WhenNotEnoughDaysElapsed_ThenIsUpdateCheckDueReturnsFalse()
        {
            var clock = SystemClock.Default;
            var now = clock.UtcNow;

            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                CreateInstall(),
                clock);

            Assert.That(policy.FollowedTrack, Is.EqualTo(ReleaseTrack.Normal));

            Assert.That(policy.IsUpdateCheckDue(now), Is.False);
            Assert.That(policy.IsUpdateCheckDue(now.AddYears(1)), Is.False);
            Assert.That(policy.IsUpdateCheckDue(now.AddDays(-policy.DaysBetweenUpdateChecks).AddMinutes(1)), Is.False);
            Assert.That(policy.IsUpdateCheckDue(now.AddDays(-policy.DaysBetweenUpdateChecks + 1)), Is.False);
        }

        [Test]
        public void IsUpdateCheckDue_WhenTooManyDaysElapsed_ThenIsUpdateCheckDueReturnsTrue()
        {
            var clock = SystemClock.Default;
            var now = clock.UtcNow;

            var policy = new UpdatePolicy(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                CreateInstall(),
                clock);

            Assert.That(policy.FollowedTrack, Is.EqualTo(ReleaseTrack.Normal));

            Assert.That(policy.IsUpdateCheckDue(now.AddDays(-policy.DaysBetweenUpdateChecks)), Is.True);
            Assert.That(policy.IsUpdateCheckDue(now.AddDays(-policy.DaysBetweenUpdateChecks - 1)), Is.True);
        }
    }
}
