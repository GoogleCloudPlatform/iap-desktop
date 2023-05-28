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
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Testing.Apis.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Session
{
    [TestFixture]
    public class TestConnectInstanceComand
    {
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static ConnectInstanceCommand CreateCommand(
            Mock<ISessionContextFactory> sessionContextFactory,
            Mock<IInstanceSessionBroker> sessionBroker,
            Mock<IProjectModelService> modelService)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Add(sessionContextFactory.Object);
            serviceProvider.Add(sessionBroker.Object);
            serviceProvider.Add(modelService.Object);

            return new ConnectInstanceCommand(
                "&test",
                new Service<ISessionContextFactory>(serviceProvider.Object),
                new Service<IInstanceSessionBroker>(serviceProvider.Object),
                new Service<IProjectModelService>(serviceProvider.Object))
            {
                AvailableForRdp = true,
                AvailableForSsh = true
            };
        }

        private static Mock<IProjectModelInstanceNode> CreateInstanceNode(OperatingSystems os)
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(SampleLocator);
            node.SetupGet(n => n.OperatingSystem).Returns(os);
            node.SetupGet(n => n.IsRunning).Returns(true);
            return node;
        }

        //---------------------------------------------------------------------
        // ExecuteAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task ExecuteSetsActiveNode()
        {
            var modelService = new Mock<IProjectModelService>();
            var sessionBroker = new Mock<IInstanceSessionBroker>();
            var session = (ISession)new Mock<IRemoteDesktopSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out session))
                .Returns(true);

            var command = CreateCommand(
                new Mock<ISessionContextFactory>(),
                sessionBroker,
                modelService);

            var instance = CreateInstanceNode(OperatingSystems.Windows).Object;

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            modelService.Verify(
                s => s.SetActiveNodeAsync(instance, CancellationToken.None),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // ExecuteAsync - RDP.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenRdpSessionFound_ThenExecuteDoesNotCreateNewSession()
        {
            var contextFactory = new Mock<ISessionContextFactory>();
            var sessionBroker = new Mock<IInstanceSessionBroker>();
            var session = (ISession)new Mock<IRemoteDesktopSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out session))
                .Returns(true);

            var command = CreateCommand(
                contextFactory,
                sessionBroker,
                new Mock<IProjectModelService>());

            var instance = CreateInstanceNode(OperatingSystems.Windows).Object;

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    It.IsAny<RdpCreateSessionFlags>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task WhenNoRdpSessionFound_ThenExecuteCreatesNewSession()
        {
            var instance = CreateInstanceNode(OperatingSystems.Windows).Object;

            var context = new Mock<ISessionContext<RdpCredential, RdpSessionParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateRdpSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    RdpCreateSessionFlags.None,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);
            ISession nullSession;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out nullSession))
                .Returns(false);

            var command = CreateCommand(
                contextFactory,
                sessionBroker,
                new Mock<IProjectModelService>());

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(
                    instance,
                    RdpCreateSessionFlags.None,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // ExecuteAsync - SSH.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSshSessionFoundAndForceNewIsFalse_ThenExecuteDoesNotCreateNewSession()
        {
            var context = new Mock<ISessionContext<SshCredential, SshSessionParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateSshSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var session = (ISession)new Mock<ISshTerminalSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out session))
                .Returns(true);

            var command = CreateCommand(
                contextFactory,
                sessionBroker,
                new Mock<IProjectModelService>());
            command.ForceNewConnection = false;

            var instance = CreateInstanceNode(OperatingSystems.Linux).Object;

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            sessionBroker.Verify(
                s => s.TryActivate(SampleLocator, out session),
                Times.Once);
            contextFactory.Verify(
                s => s.CreateSshSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task WhenSshSessionFoundAndForceNewIsTrue_ThenExecuteCreatesNewSession()
        {
            var instance = CreateInstanceNode(OperatingSystems.Linux).Object;

            var context = new Mock<ISessionContext<SshCredential, SshSessionParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateSshSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var command = CreateCommand(
                contextFactory,
                sessionBroker,
                new Mock<IProjectModelService>());
            command.ForceNewConnection = true;

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            ISession session;
            sessionBroker.Verify(
                s => s.TryActivate(SampleLocator, out session),
                Times.Never);
            contextFactory.Verify(
                s => s.CreateSshSessionContextAsync(
                    instance,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenNoSshSessionFound_ThenExecuteCreatesNewSession()
        {
            var instance = CreateInstanceNode(OperatingSystems.Linux).Object;

            var context = new Mock<ISessionContext<SshCredential, SshSessionParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateSshSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            ISession nullSession;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out nullSession))
                .Returns(false);

            var command = CreateCommand(
                contextFactory,
                sessionBroker,
                new Mock<IProjectModelService>());

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateSshSessionContextAsync(
                    instance,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
