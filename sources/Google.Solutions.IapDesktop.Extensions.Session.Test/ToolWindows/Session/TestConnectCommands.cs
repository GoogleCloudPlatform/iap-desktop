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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Ssh;
using Moq;
using NUnit.Framework;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
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
            Mock<IProjectWorkspace> modelService,
            Mock<ISessionFactory> sessionFactory)
        {
            return new ConnectCommands(
                urlCommands,
                contextFactory.Object,
                modelService.Object,
                sessionFactory.Object,
                new Mock<ISessionBroker>().Object);
        }

        //---------------------------------------------------------------------
        // LaunchRdpUrl.
        //---------------------------------------------------------------------

        [Test]
        public void LaunchRdpUrl_IsEnabled()
        {
            var urlCommands = new UrlCommands();
            CreateConnectCommands(
                urlCommands,
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            Assert.AreEqual(
                CommandState.Enabled,
                urlCommands.LaunchRdpUrl.QueryState(SampleUrl));
        }

        [Test]
        public async Task LaunchRdpUrl_ConnectsInstance()
        {
            var context = new Mock<ISessionContext<RdpCredential, RdpParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateRdpSessionContextAsync(SampleUrl, CancellationToken.None))
                .ReturnsAsync(context.Object);

            var sessionFactory = new Mock<ISessionFactory>();
            sessionFactory
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var urlCommands = new UrlCommands();
            CreateConnectCommands(
                urlCommands,
                contextFactory,
                new Mock<IProjectWorkspace>(),
                sessionFactory);

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
        public void ToolbarActivateOrConnect_WhenApplicableAndVmRunning_ThenToolbarActivateOrConnectInstanceIsEnabled(
            [Values(
                OperatingSystems.Windows,
                OperatingSystems.Linux)] OperatingSystems os)
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(os);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ToolbarActivateOrConnectInstance.QueryState(runningInstance.Object));
        }

        [Test]
        public void ToolbarActivateOrConnect_WhenApplicableButVmNotRunning_ThenToolbarActivateOrConnectInstanceIsDisabled(
            [Values(
                OperatingSystems.Windows,
                OperatingSystems.Linux)] OperatingSystems os)
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(os);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ToolbarActivateOrConnectInstance.QueryState(stoppedInstance.Object));
        }

        [Test]
        public void ToolbarActivateOrConnect_WhenNotApplicable_ThenToolbarActivateOrConnectInstanceIsDisabled()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

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
        public async Task ToolbarActivateOrConnect_WhenInstanceSupportsRdp_ThenToolbarActivateOrConnectInstanceUsesRdp()
        {
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

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectWorkspace>(),
                sessionFactory);

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
        public async Task ToolbarActivateOrConnect_WhenInstanceSupportsSsh_ThenToolbarActivateOrConnectInstanceUsesSsh()
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

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectWorkspace>(),
                sessionFactory);

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
        public void ContextMenuActivateOrConnect_WhenApplicableAndVmRunning_ThenContextMenuActivateOrConnectInstanceIsEnabled(
            [Values(
                OperatingSystems.Windows,
                OperatingSystems.Linux)] OperatingSystems os)
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(os);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuActivateOrConnectInstance.QueryState(runningInstance.Object));
        }

        [Test]
        public void ContextMenuActivateOrConnect_WhenApplicableButVmNotRunning_ThenContextMenuActivateOrConnectInstanceIsDisabled(
            [Values(
                OperatingSystems.Windows,
                OperatingSystems.Linux)] OperatingSystems os)
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(os);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuActivateOrConnectInstance.QueryState(stoppedInstance.Object));
        }

        [Test]
        public void ContextMenuActivateOrConnect_WhenNotApplicable_ThenContextMenuActivateOrConnectInstanceIsUnavailable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

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
        public async Task ContextMenuActivateOrConnect_WhenInstanceSupportsRdp_ThenContextMenuActivateOrConnectInstanceUsesRdp()
        {
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

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectWorkspace>(),
                sessionFactory);

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
        public async Task ContextMenuActivateOrConnect_WhenInstanceSupportsSsh_ThenContextMenuActivateOrConnectInstanceUsesSsh()
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

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectWorkspace>(),
                sessionFactory);

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
        public void ContextMenuConnectAsUser_WhenApplicableAndVmRunning_ThenContextMenuConnectAsUserIsEnabled()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuConnectRdpAsUser.QueryState(runningInstance.Object));
        }

        [Test]
        public void ContextMenuConnectAsUser_WhenApplicableButVmNotRunning_ThenContextMenuConnectAsUserIsDisabled()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuConnectRdpAsUser.QueryState(stoppedInstance.Object));
        }

        [Test]
        public void ContextMenuConnectAsUser_WhenNotApplicable_ThenConnectRdpAsUserIsUnavailable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

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
        public async Task ContextMenuConnectAsUser_ContextMenuConnectAsUserDisallowsPersistentCredentialsAndForcesNewConnection()
        {
            var context = new Mock<ISessionContext<RdpCredential, RdpParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateRdpSessionContextAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    RdpCreateSessionFlags.ForcePasswordPrompt,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionFactory = new Mock<ISessionFactory>();
            sessionFactory
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectWorkspace>(),
                sessionFactory);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            await commands.ContextMenuConnectRdpAsUser
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            var sessionBroker = new Mock<ISessionBroker>();
            ISession? session;
            sessionBroker.Verify(
                s => s.TryActivateSession(It.IsAny<InstanceLocator>(), out session),
                Times.Never);

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
        public void ContextConnectSshInNewTerminal_WhenApplicableAndVmRunning_ThenContextConnectSshInNewTerminalIsEnabled()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuConnectSshInNewTerminal.QueryState(runningInstance.Object));
        }

        [Test]
        public void ContextConnectSshInNewTerminal_WhenApplicableButVmNotRunning_ThenContextConnectSshInNewTerminalIsDisabled()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuConnectSshInNewTerminal.QueryState(stoppedInstance.Object));
        }

        [Test]
        public void ContextConnectSshInNewTerminal_WhenNotApplicable_ThenContextConnectSshInNewTerminalIsUnavailable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

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
        public async Task ContextConnectSshInNewTerminal_ForcesNewConnection()
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

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                new Mock<IProjectWorkspace>(),
                sessionFactory);

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
        public void DuplicateSession_WhenApplicable_ThenDuplicateSessionIsEnabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.Close.QueryState(connectedSession.Object));
        }

        [Test]
        public void DuplicateSession_WhenNotApplicable_ThenDuplicateSessionIsDisabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var disconnectedSession = new Mock<ISession>();
            disconnectedSession.SetupGet(s => s.IsConnected).Returns(false);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.Close.QueryState(disconnectedSession.Object));
        }

        [Test]
        public async Task DuplicateSession_ForcesNewConnection()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var locator = new InstanceLocator("project-1", "zone-1", "instance-1");

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            var modelService = new Mock<IProjectWorkspace>();
            modelService
                .Setup(s => s.GetNodeAsync(
                    locator,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(runningInstance.Object);

            var connectedSession = new Mock<ISshSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedSession.SetupGet(s => s.Instance).Returns(locator);


            var context = new Mock<ISessionContext<ISshCredential, SshParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateSshSessionContextAsync(
                    runningInstance.Object,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.Object);

            var sessionFactory = new Mock<ISessionFactory>();
            sessionFactory
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                contextFactory,
                modelService,
                sessionFactory);

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
