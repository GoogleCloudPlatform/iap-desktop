﻿//
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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.App
{
    [TestFixture]
    public class TestAppContextFactory
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static AppProtocol CreateProtocol(bool clientAvailable)
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(clientAvailable);

            return new AppProtocol(
                "app-1",
                Enumerable.Empty<ITrait>(),
                new Mock<ITransportPolicy>().Object,
                80,
                null,
                client.Object);
        }

        private static IProjectModelInstanceNode CreateInstanceNode()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(i => i.Instance).Returns(SampleLocator);
            return node.Object;
        }

        private static Mock<IConnectionSettingsService> CreateSettingsService(
            InstanceConnectionSettings settings)
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));
            return settingsService;
        }

        //---------------------------------------------------------------------
        // CreateContext - targets.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTargetUnsupported_ThenCreateContextThrowsException()
        {
            var factory = new AppContextFactory(
                CreateProtocol(true),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                new Mock<IConnectionSettingsService>().Object);

            ExceptionAssert.ThrowsAggregateException<ProtocolTargetException>(
                () => factory.CreateContextAsync(
                    new Mock<IProtocolTarget>().Object,
                    0,
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenClientUnavailable_ThenCreateContextThrowsException()
        {
            var factory = new AppContextFactory(
                CreateProtocol(false),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                new Mock<IConnectionSettingsService>().Object);

            ExceptionAssert.ThrowsAggregateException<ProtocolTargetException>(
                () => factory.CreateContextAsync(
                    CreateInstanceNode(),
                    (uint)AppProtocolContextFlags.None,
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // CreateContext - flags.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFlagsUnsupported_ThenCreateContextThrowsException()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            var settingsService = CreateSettingsService(settings);

            var factory = new AppContextFactory(
                CreateProtocol(true),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                settingsService.Object);

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(
                () => factory.CreateContextAsync(
                    CreateInstanceNode(),
                    0x10000,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenFlagsClear_ThenCreateContextUsesNoNetworkCredentials()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            var settingsService = CreateSettingsService(settings);

            var factory = new AppContextFactory(
                CreateProtocol(true),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                settingsService.Object);

            var context = (AppProtocolContext)await factory
                .CreateContextAsync(
                    CreateInstanceNode(),
                    (uint)AppProtocolContextFlags.None,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(context.NetworkCredential);
        }


        [Test]
        public async Task WhenTryUseRdpNetworkCredentialsIsSetButNoCredentialsFound_ThenCreateContextUsesNoNetworkCredentials()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            var settingsService = CreateSettingsService(settings);

            var factory = new AppContextFactory(
                CreateProtocol(true),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                settingsService.Object);

            var context = (AppProtocolContext)await factory
                .CreateContextAsync(
                    CreateInstanceNode(),
                    (uint)AppProtocolContextFlags.TryUseRdpNetworkCredentials,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(context.NetworkCredential);

            settingsService.Verify(
                s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()),
                Times.Once);
        }

        [Test]
        public async Task WhenTryUseRdpNetworkCredentials_ThenCreateContextUsesRdpNetworkCredentials()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.RdpUsername.StringValue = "user";
            settings.RdpPassword.ClearTextValue = "password";
            settings.RdpDomain.StringValue = "domain";

            var settingsService = CreateSettingsService(settings);

            var factory = new AppContextFactory(
                CreateProtocol(true),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                settingsService.Object);

            var context = (AppProtocolContext)await factory
                .CreateContextAsync(
                    CreateInstanceNode(),
                    (uint)AppProtocolContextFlags.TryUseRdpNetworkCredentials,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(context.NetworkCredential);
            Assert.AreEqual("password", context.NetworkCredential.Password);
            Assert.AreEqual("domain", context.NetworkCredential.Domain);
        }

        //---------------------------------------------------------------------
        // CreateContext - settings.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateContextAppliesSettings()
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.AppUsername.StringValue = "user";

            var settingsService = CreateSettingsService(settings);

            var factory = new AppContextFactory(
                CreateProtocol(true),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                settingsService.Object);

            var context = (AppProtocolContext)await factory
                .CreateContextAsync(
                    CreateInstanceNode(),
                    (uint)AppProtocolContextFlags.None,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("user", context.Parameters.PreferredUsername);
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse()
        {
            var factory = new AppContextFactory(
                CreateProtocol(true),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                new Mock<IConnectionSettingsService>().Object);

            Assert.IsFalse(factory.TryParse(new System.Uri("app-1:///test"), out var _));
        }
    }
}
