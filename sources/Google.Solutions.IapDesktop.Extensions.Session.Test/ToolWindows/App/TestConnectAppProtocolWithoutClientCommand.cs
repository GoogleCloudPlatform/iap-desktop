//
// Copyright 2024 Google LLC
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
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Testing.Application.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.App
{
    [TestFixture]
    public class TestConnectAppProtocolWithoutClientCommand
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static AppProtocolContextFactory CreateFactory(
            Extensions.Session.Settings.ConnectionSettings settings)
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Target).Returns(SampleInstance);
            transport.SetupGet(t => t.Endpoint).Returns(new IPEndPoint(IPAddress.Loopback, 1));

            var transportFactory = new Mock<IIapTransportFactory>();
            transportFactory
                .Setup(t => t.CreateTransportAsync(
                    It.IsAny<IProtocol>(),
                    It.IsAny<ITransportPolicy>(),
                    SampleInstance,
                    8080,
                    null,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(transport.Object);

            return new AppProtocolContextFactory(
                new AppProtocol(
                    "app 1",
                    Enumerable.Empty<ITrait>(),
                    8080,
                    null,
                    null),
                transportFactory.Object,
                new Mock<IWin32ProcessFactory>().Object,
                settingsService.Object); ;
        }

        //---------------------------------------------------------------------
        // Id.
        //---------------------------------------------------------------------

        [Test]
        public void Id()
        {
            var command = new ConnectAppProtocolWithoutClientCommand(
                new SynchronousJobService(),
                CreateFactory(null),
                new Mock<INotifyDialog>().Object);

            Assert.AreEqual(
                $"{command.GetType().Name}.app-1",
                command.Id);
        }

        //---------------------------------------------------------------------
        // IsAvailable.
        //---------------------------------------------------------------------

        [Test]
        public void WhenContextOfWrongType_ThenQueryStateReturnsUnavailable()
        {
            var command = new ConnectAppProtocolWithoutClientCommand(
                new SynchronousJobService(),
                CreateFactory(null),
                new Mock<INotifyDialog>().Object);

            Assert.AreEqual(
                CommandState.Unavailable,
                command.QueryState(new Mock<IProjectModelCloudNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                command.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                command.QueryState(new Mock<IProjectModelZoneNode>().Object));
        }

        [Test]
        public void WhenContextIsInstance_ThenQueryStateReturnsEnabled()
        {
            var command = new ConnectAppProtocolWithoutClientCommand(
                new SynchronousJobService(),
                CreateFactory(null),
                new Mock<INotifyDialog>().Object);

            Assert.AreEqual(
                CommandState.Enabled,
                command.QueryState(new Mock<IProjectModelInstanceNode>().Object));
        }

        //---------------------------------------------------------------------
        // Execute.
        //---------------------------------------------------------------------

        [Test]
        public async Task ExecuteShowsBalloon()
        {
            var notifyDialog = new Mock<INotifyDialog>();

            var command = new ConnectAppProtocolWithoutClientCommand(
                new SynchronousJobService(),
                CreateFactory(new Extensions.Session.Settings.ConnectionSettings(SampleInstance)),
                notifyDialog.Object);

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(SampleInstance);

            await command
                .ExecuteAsync(node.Object)
                .ConfigureAwait(false);

            notifyDialog.Verify(
                d => d.ShowBalloon(It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }
    }
}
