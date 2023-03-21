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

using Moq;
using NUnit.Framework;
using System;
using Google.Solutions.Testing.Common.Mocks;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Session;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Session
{
    [TestFixture]
    public class TestActivateOrConnectInstanceComand
    {
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");


        private static ActivateOrConnectInstanceCommand CreateCommand(
            Mock<ISshConnectionService> sshConnectionService,
            Mock<IRdpConnectionService> rdpConnectionService,
            Mock<IGlobalSessionBroker> sessionBroker)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Add(sshConnectionService.Object);
            serviceProvider.Add(rdpConnectionService.Object);
            serviceProvider.Add(sessionBroker.Object);

            return new ActivateOrConnectInstanceCommand(
                "&test",
                new Mock<ICommandContainer<ISession>>().Object,
                new Service<IRdpConnectionService>(serviceProvider.Object),
                new Service<ISshConnectionService>(serviceProvider.Object),
                new Service<IGlobalSessionBroker>(serviceProvider.Object))
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
            var connectionService = new Mock<IRdpConnectionService>();
            var sessionBroker = new Mock<IGlobalSessionBroker>();
            var session = (ISession)new Mock<IRemoteDesktopSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out session))
                .Returns(true);

            var command = CreateCommand(
                new Mock<ISshConnectionService>(),
                connectionService,
                sessionBroker);

            var instance = CreateInstanceNode(OperatingSystems.Windows).Object;

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            connectionService.Verify(
                s => s.ConnectInstanceAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Test]
        public async Task WhenNoRdpSessionFound_ThenExecuteCreatesNewSession()
        {
            var instance = CreateInstanceNode(OperatingSystems.Windows).Object;

            var connectionService = new Mock<IRdpConnectionService>();
            connectionService
                .Setup(s => s.ConnectInstanceAsync(instance, true))
                .ReturnsAsync(new Mock<IRemoteDesktopSession>().Object);
            var sessionBroker = new Mock<IGlobalSessionBroker>();
            ISession nullSession;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out nullSession))
                .Returns(false);

            var command = CreateCommand(
                new Mock<ISshConnectionService>(),
                connectionService,
                sessionBroker);

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            connectionService.Verify(
                s => s.ConnectInstanceAsync(instance, true),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // ExecuteAsync - SSH.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSshSessionFoundAndForceNewIsFalse_ThenExecuteDoesNotCreateNewSession()
        {
            var connectionService = new Mock<ISshConnectionService>();
            var sessionBroker = new Mock<IGlobalSessionBroker>();
            var session = (ISession)new Mock<ISshTerminalSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out session))
                .Returns(true);

            var command = CreateCommand(
                connectionService,
                new Mock<IRdpConnectionService>(),
                sessionBroker);
            command.ForceNewConnection = false;

            var instance = CreateInstanceNode(OperatingSystems.Linux).Object;

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            connectionService.Verify(
                s => s.ConnectInstanceAsync(It.IsAny<IProjectModelInstanceNode>()),
                Times.Never);
        }

        [Test]
        public async Task WhenSshSessionFoundAndForceNewIsTrue_ThenExecuteDoesNotCreateNewSession()
        {
            var instance = CreateInstanceNode(OperatingSystems.Linux).Object;

            var connectionService = new Mock<ISshConnectionService>();
            connectionService
                .Setup(s => s.ConnectInstanceAsync(instance))
                .ReturnsAsync(new Mock<ISshTerminalSession>().Object);
            var sessionBroker = new Mock<IGlobalSessionBroker>();

            var command = CreateCommand(
                connectionService,
                new Mock<IRdpConnectionService>(),
                sessionBroker);
            command.ForceNewConnection = true;

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            ISession session;
            sessionBroker.Verify(
                s => s.TryActivate(SampleLocator, out session),
                Times.Never);
            connectionService.Verify(
                s => s.ConnectInstanceAsync(instance),
                Times.Once);
        }

        [Test]
        public async Task WhenNoSshSessionFound_ThenExecuteCreatesNewSession()
        {
            var instance = CreateInstanceNode(OperatingSystems.Linux).Object;

            var connectionService = new Mock<ISshConnectionService>();
            connectionService
                .Setup(s => s.ConnectInstanceAsync(instance))
                .ReturnsAsync(new Mock<ISshTerminalSession>().Object);
            var sessionBroker = new Mock<IGlobalSessionBroker>();
            ISession nullSession;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out nullSession))
                .Returns(false);

            var command = CreateCommand(
                connectionService,
                new Mock<IRdpConnectionService>(),
                sessionBroker);

            await command
                .ExecuteAsync(instance)
                .ConfigureAwait(false);

            connectionService.Verify(
                s => s.ConnectInstanceAsync(instance),
                Times.Once);
        }
    }
}
