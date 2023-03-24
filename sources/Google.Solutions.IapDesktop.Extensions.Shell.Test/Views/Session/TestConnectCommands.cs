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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
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

        private static TransportParameters SampleTransportParameters =
            new TransportParameters(
                TransportParameters.TransportType.IapTunnel,
                SampleLocator,
                new IPEndPoint(IPAddress.Loopback, 1234));

        private static readonly ConnectionTemplate<RdpSessionParameters> RdpConnectionTemplate = 
            new ConnectionTemplate<RdpSessionParameters>(
                SampleTransportParameters,
                new RdpSessionParameters(RdpCredentials.Empty));

        private static readonly ConnectionTemplate<SshSessionParameters> SshConnectionTemplate =
            new ConnectionTemplate<SshSessionParameters>(
                SampleTransportParameters,
                new SshSessionParameters(
                    null,
                    null,
                    TimeSpan.MaxValue));

        private static ConnectCommands CreateConnectCommands(
            UrlCommands urlCommands,
            Mock<ISshConnectionService> sshConnectionService,
            Mock<IRdpConnectionService> rdpConnectionService,
            Mock<IProjectModelService> modelService,
            Mock<IInstanceSessionBroker> sessionBroker)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Add(sshConnectionService.Object);
            serviceProvider.Add(rdpConnectionService.Object);
            serviceProvider.Add(modelService.Object);
            serviceProvider.Add(sessionBroker.Object);

            return new ConnectCommands(
                urlCommands,
                new Service<IRdpConnectionService>(serviceProvider.Object),
                new Service<ISshConnectionService>(serviceProvider.Object),
                new Service<IProjectModelService>(serviceProvider.Object),
                new Service<IInstanceSessionBroker>(serviceProvider.Object),
                new Mock<ICommandContainer<ISession>>().Object);
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
                new Mock<IProjectModelService>(),
                new Mock<IInstanceSessionBroker>());

            Assert.AreEqual(
                CommandState.Enabled,
                urlCommands.LaunchRdpUrl.QueryState(SampleUrl));
        }

        [Test]
        public async Task LaunchRdpUrlCommandConnectsInstance()
        {
            var rdpConnectionService = new Mock<IRdpConnectionService>();
            rdpConnectionService
                .Setup(s => s.PrepareConnectionAsync(SampleUrl))
                .ReturnsAsync(RdpConnectionTemplate);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.Connect(RdpConnectionTemplate))
                .Returns(new Mock<IRemoteDesktopSession>().Object);

            var urlCommands = new UrlCommands();
            CreateConnectCommands(
                urlCommands,
                new Mock<ISshConnectionService>(),
                rdpConnectionService,
                new Mock<IProjectModelService>(),
                sessionBroker);

            await urlCommands.LaunchRdpUrl
                .ExecuteAsync(SampleUrl)
                .ConfigureAwait(false);

            rdpConnectionService.Verify(
                s => s.PrepareConnectionAsync(SampleUrl),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
            var rdpConnectionService = new Mock<IRdpConnectionService>();
            rdpConnectionService
                .Setup(s => s.PrepareConnectionAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    true))
                .ReturnsAsync(RdpConnectionTemplate);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.Connect(RdpConnectionTemplate))
                .Returns(new Mock<IRemoteDesktopSession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISshConnectionService>(),
                rdpConnectionService,
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            await commands.ToolbarActivateOrConnectInstance
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            rdpConnectionService.Verify(
                s => s.PrepareConnectionAsync(runningInstance.Object, true),
                Times.Once);
        }

        [Test]
        public async Task WhenInstanceSupportsSsh_ThenToolbarActivateOrConnectInstanceUsesSsh()
        {
            var sshConnectionService = new Mock<ISshConnectionService>();
            sshConnectionService
                .Setup(s => s.PrepareConnectionAsync(It.IsAny<IProjectModelInstanceNode>()))
                .ReturnsAsync(SshConnectionTemplate);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.ConnectAsync(SshConnectionTemplate))
                .ReturnsAsync(new Mock<ISshTerminalSession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                sshConnectionService,
                new Mock<IRdpConnectionService>(),
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            await commands.ToolbarActivateOrConnectInstance
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            sshConnectionService.Verify(
                s => s.PrepareConnectionAsync(runningInstance.Object),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
            var rdpConnectionService = new Mock<IRdpConnectionService>();
            rdpConnectionService
                .Setup(s => s.PrepareConnectionAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    true))
                .ReturnsAsync(RdpConnectionTemplate);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.Connect(RdpConnectionTemplate))
                .Returns(new Mock<IRemoteDesktopSession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISshConnectionService>(),
                rdpConnectionService,
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            await commands.ContextMenuActivateOrConnectInstance
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            rdpConnectionService.Verify(
                s => s.PrepareConnectionAsync(runningInstance.Object, true),
                Times.Once);
        }

        [Test]
        public async Task WhenInstanceSupportsSsh_ThenContextMenuActivateOrConnectInstanceUsesSsh()
        {
            var sshConnectionService = new Mock<ISshConnectionService>();
            sshConnectionService
                .Setup(s => s.PrepareConnectionAsync(It.IsAny<IProjectModelInstanceNode>()))
                .ReturnsAsync(SshConnectionTemplate);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.ConnectAsync(SshConnectionTemplate))
                .ReturnsAsync(new Mock<ISshTerminalSession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                sshConnectionService,
                new Mock<IRdpConnectionService>(),
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            await commands.ContextMenuActivateOrConnectInstance
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            sshConnectionService.Verify(
                s => s.PrepareConnectionAsync(runningInstance.Object),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
            var rdpConnectionService = new Mock<IRdpConnectionService>();
            rdpConnectionService
                .Setup(s => s.PrepareConnectionAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    false))
                .ReturnsAsync(RdpConnectionTemplate);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.Connect(RdpConnectionTemplate))
                .Returns(new Mock<IRemoteDesktopSession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISshConnectionService>(),
                rdpConnectionService,
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            await commands.ContextMenuConnectRdpAsUser
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            rdpConnectionService.Verify(
                s => s.PrepareConnectionAsync(runningInstance.Object, false),
                Times.Once);
            rdpConnectionService.Verify(
                s => s.PrepareConnectionAsync(runningInstance.Object, true),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>(),
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
            var sshConnectionService = new Mock<ISshConnectionService>();
            sshConnectionService
                .Setup(s => s.PrepareConnectionAsync(It.IsAny<IProjectModelInstanceNode>()))
                .ReturnsAsync(SshConnectionTemplate);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.ConnectAsync(SshConnectionTemplate))
                .ReturnsAsync(new Mock<ISshTerminalSession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                sshConnectionService,
                new Mock<IRdpConnectionService>(),
                new Mock<IProjectModelService>(),
                sessionBroker);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            await commands.ContextMenuConnectSshInNewTerminal
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            sshConnectionService.Verify(
                s => s.PrepareConnectionAsync(runningInstance.Object),
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

            var sshConnectionService = new Mock<ISshConnectionService>();
            sshConnectionService
                .Setup(s => s.PrepareConnectionAsync(runningInstance.Object))
                .ReturnsAsync(SshConnectionTemplate);

            var sessionBroker = new Mock<IInstanceSessionBroker>();
            sessionBroker
                .Setup(s => s.ConnectAsync(SshConnectionTemplate))
                .ReturnsAsync(new Mock<ISshTerminalSession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                sshConnectionService,
                new Mock<IRdpConnectionService>(),
                modelService,
                sessionBroker);

            await commands.DuplicateSession
                .ExecuteAsync(connectedSession.Object)
                .ConfigureAwait(false);

            sshConnectionService.Verify(
                s => s.PrepareConnectionAsync(runningInstance.Object),
                Times.Once);
        }
    }
}
