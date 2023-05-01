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
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Testing.Common.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Session
{
    [TestFixture]
    public class TestConnectCommands
    {
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static readonly IapRdpUrl SampleUrl = new IapRdpUrl(
            SampleLocator,
            new NameValueCollection());

        private static ConnectCommands CreateConnectCommands(
            UrlCommands urlCommands,
            Mock<ISessionContextFactory> contextFactory,
            Mock<IProjectModelService> modelService,
            Mock<IInstanceSessionBroker> sessionBroker)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Add(contextFactory.Object);
            serviceProvider.Add(modelService.Object);
            serviceProvider.Add(sessionBroker.Object);

            return new ConnectCommands(
                urlCommands,
                new Service<ISessionContextFactory>(serviceProvider.Object),
                new Service<IProjectModelService>(serviceProvider.Object),
                new Service<IInstanceSessionBroker>(serviceProvider.Object));
        }

        //---------------------------------------------------------------------
        // LaunchRdpUrl.
        //---------------------------------------------------------------------

        [Test]
        public void LaunchRdpUrlIsEnabled()
        {
            var urlCommands = new UrlCommands();
            CreateConnectCommands(
                urlCommands,
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            Assert.AreEqual(
                CommandState.Enabled,
                urlCommands.LaunchRdpUrl.QueryState(SampleUrl));
        }

        [Test]
        public async Task LaunchRdpUrlCommandConnectsInstance()
        {
            var context = new Mock<ISessionContext<RdpCredential, RdpSessionParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateRdpSessionContextAsync(SampleUrl, CancellationToken.None))
                .ReturnsAsync(context.Object);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var urlCommands = new UrlCommands();
            CreateConnectCommands(
                urlCommands,
                contextFactory,
                new Mock<IProjectModelService>(),
                sessionBroker);

            await urlCommands.LaunchRdpUrl
                .ExecuteAsync(SampleUrl)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(SampleUrl, CancellationToken.None),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // ToolbarActivateOrConnect.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicableAndVmRunning_ThenToolbarActivateOrConnectInstanceIsEnabled(
            [Values(
                OperatingSystems.Windows,
                OperatingSystems.Linux)] OperatingSystems os)
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(os);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ToolbarActivateOrConnectInstance.QueryState(runningInstance.Object));
        }

        [Test]
        public void WhenApplicableButVmNotRunning_ThenToolbarActivateOrConnectInstanceIsDisabled(
            [Values(
                OperatingSystems.Windows,
                OperatingSystems.Linux)] OperatingSystems os)
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(os);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ToolbarActivateOrConnectInstance.QueryState(stoppedInstance.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenToolbarActivateOrConnectInstanceIsDisabled()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ToolbarActivateOrConnectInstance.QueryState(new Mock<IProjectModelCloudNode>().Object));
            Assert.AreEqual(
                CommandState.Disabled,
                commands.ToolbarActivateOrConnectInstance.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Disabled,
                commands.ToolbarActivateOrConnectInstance.QueryState(new Mock<IProjectModelZoneNode>().Object));
        }

        [Test]
        public async Task WhenInstanceSupportsRdp_ThenToolbarActivateOrConnectInstanceUsesRdp()
        {
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

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            await commands.ToolbarActivateOrConnectInstance
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(
                    runningInstance.Object, 
                    RdpCreateSessionFlags.None,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenInstanceSupportsSsh_ThenToolbarActivateOrConnectInstanceUsesSsh()
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

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            await commands.ToolbarActivateOrConnectInstance
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateSshSessionContextAsync(
                    runningInstance.Object,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // ContextMenuActivateOrConnect.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicableAndVmRunning_ThenContextMenuActivateOrConnectInstanceIsEnabled(
            [Values(
                OperatingSystems.Windows,
                OperatingSystems.Linux)] OperatingSystems os)
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(os);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuActivateOrConnectInstance.QueryState(runningInstance.Object));
        }

        [Test]
        public void WhenApplicableButVmNotRunning_ThenContextMenuActivateOrConnectInstanceIsDisabled(
            [Values(
                OperatingSystems.Windows,
                OperatingSystems.Linux)] OperatingSystems os)
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(os);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuActivateOrConnectInstance.QueryState(stoppedInstance.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenContextMenuActivateOrConnectInstanceIsUnavailable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuActivateOrConnectInstance.QueryState(new Mock<IProjectModelCloudNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuActivateOrConnectInstance.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuActivateOrConnectInstance.QueryState(new Mock<IProjectModelZoneNode>().Object));
        }

        [Test]
        public async Task WhenInstanceSupportsRdp_ThenContextMenuActivateOrConnectInstanceUsesRdp()
        {
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

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            await commands.ContextMenuActivateOrConnectInstance
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(
                    runningInstance.Object,
                    RdpCreateSessionFlags.None,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WhenInstanceSupportsSsh_ThenContextMenuActivateOrConnectInstanceUsesSsh()
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

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            await commands.ContextMenuActivateOrConnectInstance
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateSshSessionContextAsync(
                    runningInstance.Object,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // ContextMenuConnectAsUser.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicableAndVmRunning_ThenContextMenuConnectAsUserIsEnabled()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuConnectRdpAsUser.QueryState(runningInstance.Object));
        }

        [Test]
        public void WhenApplicableButVmNotRunning_ThenContextMenuConnectAsUserIsDisabled()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuConnectRdpAsUser.QueryState(stoppedInstance.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenConnectRdpAsUserIsUnavailable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            var runningLinuxInstance = new Mock<IProjectModelInstanceNode>();
            runningLinuxInstance.Setup(s => s.IsRunning).Returns(true);
            runningLinuxInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuConnectRdpAsUser.QueryState(runningLinuxInstance.Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuConnectRdpAsUser.QueryState(new Mock<IProjectModelCloudNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuConnectRdpAsUser.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuConnectRdpAsUser.QueryState(new Mock<IProjectModelZoneNode>().Object));
        }

        [Test]
        public async Task ContextMenuConnectAsUserDisallowsPersistentCredentials()
        {
            var context = new Mock<ISessionContext<RdpCredential, RdpSessionParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateRdpSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    RdpCreateSessionFlags.ForcePasswordPrompt,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            await commands.ContextMenuConnectRdpAsUser
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(
                    runningInstance.Object, 
                    RdpCreateSessionFlags.ForcePasswordPrompt, 
                    It.IsAny<CancellationToken>()),
                Times.Once);
            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(
                    runningInstance.Object, 
                    RdpCreateSessionFlags.None,
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        //---------------------------------------------------------------------
        // ContextConnectSshInNewTerminal.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicableAndVmRunning_ThenContextConnectSshInNewTerminalIsEnabled()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuConnectSshInNewTerminal.QueryState(runningInstance.Object));
        }

        [Test]
        public void WhenApplicableButVmNotRunning_ThenContextConnectSshInNewTerminalIsDisabled()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuConnectSshInNewTerminal.QueryState(stoppedInstance.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenContextConnectSshInNewTerminalIsUnavailable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            var runningWindowsInstance = new Mock<IProjectModelInstanceNode>();
            runningWindowsInstance.Setup(s => s.IsRunning).Returns(true);
            runningWindowsInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuConnectSshInNewTerminal.QueryState(runningWindowsInstance.Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuConnectSshInNewTerminal.QueryState(new Mock<IProjectModelCloudNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuConnectSshInNewTerminal.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuConnectSshInNewTerminal.QueryState(new Mock<IProjectModelZoneNode>().Object));
        }

        [Test]
        public async Task ContextMenuConnectSshInNewTerminalForcesNewConnection()
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

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            await commands.ContextMenuConnectSshInNewTerminal
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateSshSessionContextAsync(
                    runningInstance.Object,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // DuplicateSession.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenDuplicateSessionIsEnabled()
        {
            var sessionCommands = new SessionCommands();

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.Disconnect.QueryState(connectedSession.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenDuplicateSessionIsDisabled()
        {
            var sessionCommands = new SessionCommands();

            var disconnectedSession = new Mock<ISession>();
            disconnectedSession.SetupGet(s => s.IsConnected).Returns(false);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.Disconnect.QueryState(disconnectedSession.Object));
        }

        [Test]
        public async Task DuplicateSessionForcesNewConnection()
        {
            var sessionCommands = new SessionCommands();

            var locator = new InstanceLocator("project-1", "zone-1", "instance-1");

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(s => s.GetNodeAsync(
                    locator,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(runningInstance.Object);

            var connectedSession = new Mock<ISshTerminalSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedSession.SetupGet(s => s.Instance).Returns(locator);


            var context = new Mock<ISessionContext<SshCredential, SshSessionParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateSshSessionContextAsync(
                    runningInstance.Object,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                modelService,
                sessionBroker);

            await commands.DuplicateSession
                .ExecuteAsync(connectedSession.Object)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateSshSessionContextAsync(
                    runningInstance.Object,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
