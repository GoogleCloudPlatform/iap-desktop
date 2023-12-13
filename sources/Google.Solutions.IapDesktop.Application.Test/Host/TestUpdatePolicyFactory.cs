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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Moq;
using NUnit.Framework;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestUpdatePolicyFactory
    {
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

        private Mock<IAuthorization> CreateAuthorization(IOidcSession session)
        {
            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Session).Returns(session);
            return authorization;
        }

        //---------------------------------------------------------------------
        // GetPolicy.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUpdateCheckDisabled_ThenPolicyUsesCriticalTrack()
        {
            var factory = new UpdatePolicyFactory(
                CreateSettingsRepository(false).Object,
                CreateAuthorization(CreateWorkforceSession().Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Critical, factory.GetPolicy().FollowedTrack);
        }

        [Test]
        public void WhenUserSignedInWithWorkforceIdentity_ThenPolicyUsesNormalTrack()
        {
            var factory = new UpdatePolicyFactory(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateWorkforceSession().Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Normal, factory.GetPolicy().FollowedTrack);
        }

        [Test]
        public void WhenUserSignedInWithGaia_ThenPolicyUsesNormalTrack()
        {
            var factory = new UpdatePolicyFactory(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession("_@example.com").Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Normal, factory.GetPolicy().FollowedTrack);
        }

        [Test]
        public void WhenUserIsGoogler_ThenPolicyUsesCanaryTrack(
            [Values("_@google.com", "_@test.altostrat.com")] string email)
        {
            var factory = new UpdatePolicyFactory(
                CreateSettingsRepository(true).Object,
                CreateAuthorization(CreateGaiaSession(email).Object).Object,
                new Mock<IInstall>().Object,
                SystemClock.Default);

            Assert.AreEqual(ReleaseTrack.Canary, factory.GetPolicy().FollowedTrack);
        }
    }
}
