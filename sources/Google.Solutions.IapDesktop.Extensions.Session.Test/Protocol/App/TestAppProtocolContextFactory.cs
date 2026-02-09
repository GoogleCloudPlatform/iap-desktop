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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
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
    public class TestAppProtocolContextFactory
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
            ConnectionSettings settings)
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
        public void CreateContext_WhenTargetUnsupported()
        {
            var factory = new AppProtocolContextFactory(
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
        public void CreateContext_WhenClientUnavailable()
        {
            var factory = new AppProtocolContextFactory(
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
        public void CreateContext_WhenFlagsUnsupported()
        {
            var settings = new ConnectionSettings(SampleLocator);
            var settingsService = CreateSettingsService(settings);

            var factory = new AppProtocolContextFactory(
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
        public async Task CreateContext_WhenFlagsClear()
        {
            var settings = new ConnectionSettings(SampleLocator);
            var settingsService = CreateSettingsService(settings);

            var factory = new AppProtocolContextFactory(
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

            Assert.That(context.NetworkCredential, Is.Null);
        }


        [Test]
        public async Task CreateContext_WhenTryUseRdpNetworkCredentialsIsSetButNoCredentialsFound()
        {
            var settings = new ConnectionSettings(SampleLocator);
            var settingsService = CreateSettingsService(settings);

            var factory = new AppProtocolContextFactory(
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

            Assert.That(context.NetworkCredential, Is.Null);

            settingsService.Verify(
                s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()),
                Times.Once);
        }

        [Test]
        public async Task CreateContext_WhenTryUseRdpNetworkCredentials()
        {
            var settings = new ConnectionSettings(SampleLocator);
            settings.RdpUsername.Value = "user";
            settings.RdpPassword.SetClearTextValue("password");
            settings.RdpDomain.Value = "domain";

            var settingsService = CreateSettingsService(settings);

            var factory = new AppProtocolContextFactory(
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

            Assert.That(context.NetworkCredential, Is.Not.Null);
            Assert.That(context.NetworkCredential!.Password, Is.EqualTo("password"));
            Assert.That(context.NetworkCredential.Domain, Is.EqualTo("domain"));
        }

        //---------------------------------------------------------------------
        // CreateContext - settings.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateContext_AppliesSettings()
        {
            var settings = new ConnectionSettings(SampleLocator);
            settings.AppUsername.Value = "user";
            settings.AppNetworkLevelAuthentication.Value
                = AppNetworkLevelAuthenticationState.Disabled;

            var settingsService = CreateSettingsService(settings);

            var factory = new AppProtocolContextFactory(
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

            Assert.That(context.Parameters.PreferredUsername, Is.EqualTo("user"));
            Assert.That(
                context.Parameters.NetworkLevelAuthentication, Is.EqualTo(AppNetworkLevelAuthenticationState.Disabled));
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse()
        {
            var factory = new AppProtocolContextFactory(
                CreateProtocol(true),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                new Mock<IConnectionSettingsService>().Object);

            Assert.That(factory.TryParse(new System.Uri("app-1:///test"), out var _), Is.False);
        }
    }
}
