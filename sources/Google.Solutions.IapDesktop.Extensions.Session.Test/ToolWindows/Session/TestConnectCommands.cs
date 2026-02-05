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

            Assert.That(
                urlCommands.LaunchRdpUrl.QueryState(SampleUrl), Is.EqualTo(CommandState.Enabled));
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
        public void ToolbarActivateOrConnect_WhenApplicableAndVmRunning(
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

            Assert.That(
                commands.ToolbarActivateOrConnectInstance.QueryState(runningInstance.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void ToolbarActivateOrConnect_WhenApplicableButVmNotRunning(
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

            Assert.That(
                commands.ToolbarActivateOrConnectInstance.QueryState(stoppedInstance.Object), Is.EqualTo(CommandState.Disabled));
        }

        [Test]
        public void ToolbarActivateOrConnect_WhenNotApplicable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            Assert.That(
                commands.ToolbarActivateOrConnectInstance.QueryState(new Mock<IProjectModelCloudNode>().Object), Is.EqualTo(CommandState.Disabled));
            Assert.That(
                commands.ToolbarActivateOrConnectInstance.QueryState(new Mock<IProjectModelProjectNode>().Object), Is.EqualTo(CommandState.Disabled));
            Assert.That(
                commands.ToolbarActivateOrConnectInstance.QueryState(new Mock<IProjectModelZoneNode>().Object), Is.EqualTo(CommandState.Disabled));
        }

        [Test]
        public async Task ToolbarActivateOrConnect_WhenInstanceSupportsRdp()
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
        public async Task ToolbarActivateOrConnect_WhenInstanceSupportsSsh()
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
        public void ContextMenuActivateOrConnect_WhenApplicableAndVmRunning(
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

            Assert.That(
                commands.ContextMenuActivateOrConnectInstance.QueryState(runningInstance.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void ContextMenuActivateOrConnect_WhenApplicableButVmNotRunning(
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

            Assert.That(
                commands.ContextMenuActivateOrConnectInstance.QueryState(stoppedInstance.Object), Is.EqualTo(CommandState.Disabled));
        }

        [Test]
        public void ContextMenuActivateOrConnect_WhenNotApplicable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            Assert.That(
                commands.ContextMenuActivateOrConnectInstance.QueryState(new Mock<IProjectModelCloudNode>().Object), Is.EqualTo(CommandState.Unavailable));
            Assert.That(
                commands.ContextMenuActivateOrConnectInstance.QueryState(new Mock<IProjectModelProjectNode>().Object), Is.EqualTo(CommandState.Unavailable));
            Assert.That(
                commands.ContextMenuActivateOrConnectInstance.QueryState(new Mock<IProjectModelZoneNode>().Object), Is.EqualTo(CommandState.Unavailable));
        }

        [Test]
        public async Task ContextMenuActivateOrConnect_WhenInstanceSupportsRdp()
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
        public async Task ContextMenuActivateOrConnect_WhenInstanceSupportsSsh()
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
        public void ContextMenuConnectAsUser_WhenApplicableAndVmRunning()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            Assert.That(
                commands.ContextMenuConnectRdpAsUser.QueryState(runningInstance.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void ContextMenuConnectAsUser_WhenApplicableButVmNotRunning()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            Assert.That(
                commands.ContextMenuConnectRdpAsUser.QueryState(stoppedInstance.Object), Is.EqualTo(CommandState.Disabled));
        }

        [Test]
        public void ContextMenuConnectAsUser_WhenNotApplicable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var runningLinuxInstance = new Mock<IProjectModelInstanceNode>();
            runningLinuxInstance.Setup(s => s.IsRunning).Returns(true);
            runningLinuxInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            Assert.That(
                commands.ContextMenuConnectRdpAsUser.QueryState(runningLinuxInstance.Object), Is.EqualTo(CommandState.Unavailable));
            Assert.That(
                commands.ContextMenuConnectRdpAsUser.QueryState(new Mock<IProjectModelCloudNode>().Object), Is.EqualTo(CommandState.Unavailable));
            Assert.That(
                commands.ContextMenuConnectRdpAsUser.QueryState(new Mock<IProjectModelProjectNode>().Object), Is.EqualTo(CommandState.Unavailable));
            Assert.That(
                commands.ContextMenuConnectRdpAsUser.QueryState(new Mock<IProjectModelZoneNode>().Object), Is.EqualTo(CommandState.Unavailable));
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
        public void ContextConnectSshInNewTerminal_WhenApplicableAndVmRunning()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            Assert.That(
                commands.ContextMenuConnectSshInNewTerminal.QueryState(runningInstance.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void ContextConnectSshInNewTerminal_WhenApplicableButVmNotRunning()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            Assert.That(
                commands.ContextMenuConnectSshInNewTerminal.QueryState(stoppedInstance.Object), Is.EqualTo(CommandState.Disabled));
        }

        [Test]
        public void ContextConnectSshInNewTerminal_WhenNotApplicable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISessionContextFactory>(),
                new Mock<IProjectWorkspace>(),
                new Mock<ISessionFactory>());

            var runningWindowsInstance = new Mock<IProjectModelInstanceNode>();
            runningWindowsInstance.Setup(s => s.IsRunning).Returns(true);
            runningWindowsInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            Assert.That(
                commands.ContextMenuConnectSshInNewTerminal.QueryState(runningWindowsInstance.Object), Is.EqualTo(CommandState.Unavailable));
            Assert.That(
                commands.ContextMenuConnectSshInNewTerminal.QueryState(new Mock<IProjectModelCloudNode>().Object), Is.EqualTo(CommandState.Unavailable));
            Assert.That(
                commands.ContextMenuConnectSshInNewTerminal.QueryState(new Mock<IProjectModelProjectNode>().Object), Is.EqualTo(CommandState.Unavailable));
            Assert.That(
                commands.ContextMenuConnectSshInNewTerminal.QueryState(new Mock<IProjectModelZoneNode>().Object), Is.EqualTo(CommandState.Unavailable));
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
        public void DuplicateSession_WhenApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.Close.QueryState(connectedSession.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void DuplicateSession_WhenNotApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var disconnectedSession = new Mock<ISession>();
            disconnectedSession.SetupGet(s => s.IsConnected).Returns(false);

            Assert.That(
                sessionCommands.Close.QueryState(disconnectedSession.Object), Is.EqualTo(CommandState.Disabled));
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
