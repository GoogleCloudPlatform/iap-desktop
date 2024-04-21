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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh;
using Google.Solutions.Ssh;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
{
    [TestFixture]
    public class TestConnectInstanceComand
    {
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static ConnectInstanceCommand CreateCommand(
            Mock<ISessionContextFactory> sessionContextFactory,
            Mock<ISessionFactory> sessionFactory,
            Mock<ISessionBroker> sessionBroker,
            Mock<IProjectWorkspace> workspace)
        {
            return new ConnectInstanceCommand(
                "&test",
                sessionContextFactory.Object,
                sessionFactory.Object,
                sessionBroker.Object,
                workspace.Object)
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
            var workspace = new Mock<IProjectWorkspace>();
            var sessionBroker = new Mock<ISessionBroker>();
            var session = (ISession)new Mock<IRdpSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivateSession(SampleLocator, out session))
                .Returns(true);

            var command = CreateCommand(
                new Mock<ISessionContextFactory>(),
                new Mock<ISessionFactory>(),
                sessionBroker,
                workspace);

            var instance = CreateInstanceNode(OperatingSystems.Windows).Object;

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            workspace.Verify(
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
            var sessionBroker = new Mock<ISessionBroker>();
            var session = (ISession)new Mock<IRdpSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivateSession(SampleLocator, out session))
                .Returns(true);

            var command = CreateCommand(
                contextFactory,
                new Mock<ISessionFactory>(),
                sessionBroker,
                new Mock<IProjectWorkspace>());

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

            var context = new Mock<ISessionContext<RdpCredential, RdpParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateRdpSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    RdpCreateSessionFlags.None,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionFactory = new Mock<ISessionFactory>();
            sessionFactory
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var sessionBroker = new Mock<ISessionBroker>();
            ISession nullSession;
            sessionBroker
                .Setup(s => s.TryActivateSession(SampleLocator, out nullSession))
                .Returns(false);

            var command = CreateCommand(
                contextFactory,
                sessionFactory,
                sessionBroker,
                new Mock<IProjectWorkspace>());

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
            var context = new Mock<ISessionContext<ISshCredential, SshParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateSshSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionFactory = new Mock<ISessionFactory>();
            sessionFactory
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var sessionBroker = new Mock<ISessionBroker>();
            var session = (ISession)new Mock<ISshTerminalSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivateSession(SampleLocator, out session))
                .Returns(true);

            var command = CreateCommand(
                contextFactory,
                sessionFactory,
                sessionBroker,
                new Mock<IProjectWorkspace>());
            command.ForceNewConnection = false;

            var instance = CreateInstanceNode(OperatingSystems.Linux).Object;

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            sessionBroker.Verify(
                s => s.TryActivateSession(SampleLocator, out session),
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

            var context = new Mock<ISessionContext<ISshCredential, SshParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateSshSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionFactory = new Mock<ISessionFactory>();
            sessionFactory
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var sessionBroker = new Mock<ISessionBroker>();
            var command = CreateCommand(
                contextFactory,
                sessionFactory,
                sessionBroker,
                new Mock<IProjectWorkspace>());
            command.ForceNewConnection = true;

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            ISession session;
            sessionBroker.Verify(
                s => s.TryActivateSession(SampleLocator, out session),
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

            var context = new Mock<ISessionContext<ISshCredential, SshParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateSshSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionFactory = new Mock<ISessionFactory>();
            sessionFactory
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var sessionBroker = new Mock<ISessionBroker>();
            ISession? nullSession;
            sessionBroker
                .Setup(s => s.TryActivateSession(SampleLocator, out nullSession))
                .Returns(false);

            var command = CreateCommand(
                contextFactory,
                sessionFactory,
                sessionBroker,
                new Mock<IProjectWorkspace>());

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
