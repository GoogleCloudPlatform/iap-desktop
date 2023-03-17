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
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Testing.Common.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views
{
    [TestFixture]
    public class TestConnectCommands
    {
        private static ConnectCommands CreateConnectCommands(
            UrlCommands urlCommands,
            Mock<ISshConnectionService> sshConnectionService,
            Mock<IRdpConnectionService> rdpConnectionService)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Add(sshConnectionService.Object);
            serviceProvider.Add(rdpConnectionService.Object);

            return new ConnectCommands(
                urlCommands,
                new Service<IRdpConnectionService>(serviceProvider.Object),
                new Service<ISshConnectionService>(serviceProvider.Object),
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
                new Mock<IRdpConnectionService>());

            var url = new IapRdpUrl(
                new InstanceLocator("project", "zone", "name"),
                new NameValueCollection());

            Assert.AreEqual(
                CommandState.Enabled,
                urlCommands.LaunchRdpUrl.QueryState(url));
        }

        [Test]
        public async Task LaunchRdpUrlCommandActivatesInstance()
        {
            var connectionService = new Mock<IRdpConnectionService>();

            var urlCommands = new UrlCommands();
            CreateConnectCommands(
                urlCommands,
                new Mock<ISshConnectionService>(),
                connectionService);

            var url = new IapRdpUrl(
                new InstanceLocator("project", "zone", "name"),
                new NameValueCollection());

            await urlCommands.LaunchRdpUrl
                .ExecuteAsync(url)
                .ConfigureAwait(false);

            connectionService.Verify(
                s => s.ActivateOrConnectInstanceAsync(url),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // ActivateOrConnectInstance.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicableAndVmRunning_ThenActivateOrConnectInstanceIsEnabled(
            [Values(
                OperatingSystems.Windows,
                OperatingSystems.Linux)] OperatingSystems os)
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(os);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ActivateOrConnectInstance.QueryState(runningInstance.Object));
        }

        [Test]
        public void WhenApplicableButVmNotRunning_ThenActivateOrConnectInstanceIsDisabled(
            [Values(
                OperatingSystems.Windows,
                OperatingSystems.Linux)] OperatingSystems os)
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>());

            var stoppedInstance = new Mock<IProjectModelInstanceNode>();
            stoppedInstance.Setup(s => s.IsRunning).Returns(false);
            stoppedInstance.SetupGet(s => s.OperatingSystem).Returns(os);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ActivateOrConnectInstance.QueryState(stoppedInstance.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenActivateOrConnectInstanceIsUnavailable()
        {
            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISshConnectionService>(),
                new Mock<IRdpConnectionService>());

            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ActivateOrConnectInstance.QueryState(new Mock<IProjectModelCloudNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ActivateOrConnectInstance.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ActivateOrConnectInstance.QueryState(new Mock<IProjectModelZoneNode>().Object));
        }

        [Test]
        public async Task WhenInstanceSupportsRdp_ThenActivateOrConnectInstanceUsesRdp()
        {
            var rdpConnectionService = new Mock<IRdpConnectionService>();
            rdpConnectionService
                .Setup(s => s.ActivateOrConnectInstanceAsync(
                    It.IsAny<IProjectModelInstanceNode>(),
                    true))
                .ReturnsAsync(new Mock<IRemoteDesktopSession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                new Mock<ISshConnectionService>(),
                rdpConnectionService);

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Windows);

            await commands.ActivateOrConnectInstance
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            rdpConnectionService.Verify(
                s => s.ActivateOrConnectInstanceAsync(runningInstance.Object, true),
                Times.Once);
        }

        [Test]
        public async Task WhenInstanceSupportsSsh_ThenActivateOrConnectInstanceUsesSsh()
        {
            var sshConnectionService = new Mock<ISshConnectionService>();
            sshConnectionService
                .Setup(s => s.ActivateOrConnectInstanceAsync(It.IsAny<IProjectModelInstanceNode>()))
                .ReturnsAsync(new Mock<ISshTerminalSession>().Object);

            var commands = CreateConnectCommands(
                new UrlCommands(),
                sshConnectionService,
                new Mock<IRdpConnectionService>());

            var runningInstance = new Mock<IProjectModelInstanceNode>();
            runningInstance.Setup(s => s.IsRunning).Returns(true);
            runningInstance.SetupGet(s => s.OperatingSystem).Returns(OperatingSystems.Linux);

            await commands.ActivateOrConnectInstance
                .ExecuteAsync(runningInstance.Object)
                .ConfigureAwait(false);

            sshConnectionService.Verify(
                s => s.ActivateOrConnectInstanceAsync(runningInstance.Object),
                Times.Once);
            sshConnectionService.Verify(
                s => s.ConnectInstanceAsync(runningInstance.Object),
                Times.Never);
        }
    }
}
