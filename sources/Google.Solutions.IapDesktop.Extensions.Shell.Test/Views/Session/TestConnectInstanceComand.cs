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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Testing.Common.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Session
{
    [TestFixture]
    public class TestConnectInstanceComand
    {
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static readonly TransportParameters SampleTransportParameters =
            new TransportParameters(
                TransportParameters.TransportType.IapTunnel,
                SampleLocator,
                new IPEndPoint(IPAddress.Loopback, 1234));

        private static ConnectInstanceCommand CreateCommand(
            Mock<ISessionContextFactory> sessionContextFactory,
            Mock<IInstanceSessionBroker> sessionBroker)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Add(sessionContextFactory.Object);
            serviceProvider.Add(sessionBroker.Object);

            return new ConnectInstanceCommand(
                "&test",
                new Service<ISessionContextFactory>(serviceProvider.Object),
                new Service<IInstanceSessionBroker>(serviceProvider.Object))
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
                sessionBroker);

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
                sessionBroker);

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
                sessionBroker);
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
                sessionBroker);
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
                sessionBroker);

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
