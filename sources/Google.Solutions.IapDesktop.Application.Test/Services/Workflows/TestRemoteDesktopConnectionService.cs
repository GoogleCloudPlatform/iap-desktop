//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ConnectionSettings;
using Google.Solutions.IapDesktop.Application.Services.Windows.RemoteDesktop;
using Google.Solutions.IapDesktop.Application.Services.Workflows;
using Google.Solutions.IapDesktop.Application.Util;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Workflows
{
    [TestFixture]
    public class TestRemoteDesktopConnectionService
    {
        private readonly ServiceRegistry serviceRegistry = new ServiceRegistry();

        [SetUp]
        public void SetUp()
        {
            this.serviceRegistry.AddSingleton<IJobService, SynchronousJobService>();

            var tunnel = new Mock<ITunnel>();
            tunnel.SetupGet(t => t.LocalPort).Returns(1);

            var tunnelBrokerService = new Mock<ITunnelBrokerService>();
            tunnelBrokerService.Setup(s => s.ConnectAsync(
                It.IsAny<TunnelDestination>(),
                It.IsAny<TimeSpan>())).Returns(Task.FromResult(tunnel.Object));
            this.serviceRegistry.AddSingleton<ITunnelBrokerService>(tunnelBrokerService.Object);

            var settingsEditor = new Mock<IConnectionSettingsWindow>();
            this.serviceRegistry.AddSingleton<IConnectionSettingsWindow>(settingsEditor.Object);

            var credentialsService = new Mock<ICredentialsService>();
            this.serviceRegistry.AddSingleton<ICredentialsService>(credentialsService.Object);
        }

        [Test]
        public async Task WhenConnectingByUrlWithoutUsername_ThenConnectionIsMadeWithoutUsername()
        {
            var taskDialog = new Mock<ITaskDialog>();
            this.serviceRegistry.AddSingleton<ITaskDialog>(taskDialog.Object);
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(1);    // Enter existing credentials

            var remoteDesktopService = new Mock<IRemoteDesktopService>();
            remoteDesktopService.Setup(s => s.Connect(
                It.IsAny<InstanceLocator>(),
                "localhost",
                It.IsAny<ushort>(),
                It.IsAny<VmInstanceConnectionSettings>())).Returns<IRemoteDesktopSession>(null);

            this.serviceRegistry.AddSingleton<IRemoteDesktopService>(remoteDesktopService.Object);

            var service = new RemoteDesktopConnectionService(this.serviceRegistry);
            await service.ActivateOrConnectInstanceWithCredentialPromptAsync(
                null,
                IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance"));

            remoteDesktopService.Verify(s => s.Connect(
                It.IsAny<InstanceLocator>(),
                "localhost",
                It.IsAny<ushort>(),
                It.Is<VmInstanceConnectionSettings>(i => i.Username == null)), Times.Once);
        }

        [Test]
        public async Task WhenConnectingByUrlWithUsername_ThenConnectionIsMadeWithThisUsername()
        {
            var taskDialog = new Mock<ITaskDialog>();
            this.serviceRegistry.AddSingleton<ITaskDialog>(taskDialog.Object);
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(1);    // Enter existing credentials

            var remoteDesktopService = new Mock<IRemoteDesktopService>();
            remoteDesktopService.Setup(s => s.Connect(
                It.IsAny<InstanceLocator>(),
                "localhost",
                It.IsAny<ushort>(),
                It.IsAny<VmInstanceConnectionSettings>())).Returns<IRemoteDesktopSession>(null);

            this.serviceRegistry.AddSingleton<IRemoteDesktopService>(remoteDesktopService.Object);

            var service = new RemoteDesktopConnectionService(this.serviceRegistry);
            await service.ActivateOrConnectInstanceWithCredentialPromptAsync(
                null,
                IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance?username=john%20doe"));

            remoteDesktopService.Verify(s => s.Connect(
                It.IsAny<InstanceLocator>(),
                "localhost",
                It.IsAny<ushort>(),
                It.Is<VmInstanceConnectionSettings>(i => i.Username == "john doe")), Times.Once);
        }


        [Test]
        public async Task WhenConnectingByUrlWithoutUsername_ThenSuggestedUsernameForCredentialGenerationIsDerivedFromSigninName()
        {
            var taskDialog = new Mock<ITaskDialog>();
            this.serviceRegistry.AddSingleton<ITaskDialog>(taskDialog.Object);
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);    // Generate new credentials

            var credentialsService = new Mock<ICredentialsService>();
            credentialsService.Setup(s => s.GenerateCredentialsAsync(
                It.IsAny<IWin32Window>(),
                It.IsAny<InstanceLocator>(),
                It.IsAny<string>())).Returns(Task.FromResult(new NetworkCredential("user", "password")));
            this.serviceRegistry.AddSingleton<ICredentialsService>(credentialsService.Object);

            var service = new RemoteDesktopConnectionService(this.serviceRegistry);
            await service.ActivateOrConnectInstanceWithCredentialPromptAsync(
                null,
                IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance"));

            credentialsService.Verify(s => s.GenerateCredentialsAsync(
                It.IsAny<IWin32Window>(),
                It.IsAny<InstanceLocator>(),
                It.Is<string>(user => user == null)), Times.Once);
        }

        [Test]
        public async Task WhenConnectingByUrlWithUsername_ThenSuggestedUsernameForCredentialGenerationIsThisUsername()
        {
            var taskDialog = new Mock<ITaskDialog>();
            this.serviceRegistry.AddSingleton<ITaskDialog>(taskDialog.Object);
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);    // Generate new credentials

            var credentialsService = new Mock<ICredentialsService>();
            credentialsService.Setup(s => s.GenerateCredentialsAsync(
                It.IsAny<IWin32Window>(),
                It.IsAny<InstanceLocator>(),
                It.IsAny<string>())).Returns(Task.FromResult(new NetworkCredential("user", "password")));
            this.serviceRegistry.AddSingleton<ICredentialsService>(credentialsService.Object);

            var service = new RemoteDesktopConnectionService(this.serviceRegistry);
            await service.ActivateOrConnectInstanceWithCredentialPromptAsync(
                null,
                IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance?username=john%20doe"));

            credentialsService.Verify(s => s.GenerateCredentialsAsync(
                It.IsAny<IWin32Window>(),
                It.IsAny<InstanceLocator>(),
                It.Is<string>(user => user == "john doe")), Times.Once);
        }
    }
}
