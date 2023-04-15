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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.Testing.Application.Mocks;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Connection
{
    [TestFixture]
    public class TestRdpConnectionService : ShellFixtureBase
    {
        private Mock<ITunnelBrokerService> CreateTunnelBrokerServiceMock()
        {
            var tunnel = new Mock<ITunnel>();
            tunnel.SetupGet(t => t.LocalPort).Returns(1);

            var tunnelBrokerService = new Mock<ITunnelBrokerService>();
            tunnelBrokerService.Setup(s => s.ConnectAsync(
                It.IsAny<TunnelDestination>(),
                It.IsAny<ISshRelayPolicy>(),
                It.IsAny<TimeSpan>())).Returns(Task.FromResult(tunnel.Object));

            return tunnelBrokerService;
        }

        private Mock<IProjectModelInstanceNode> CreateInstanceNodeMock()
        {
            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);
            vmNode.SetupGet(n => n.Instance)
                .Returns(new InstanceLocator("project-1", "zone-1", "instance-1"));

            return vmNode;
        }

        //---------------------------------------------------------------------
        // Connect by node.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnectingByNodeAndPersistentCredentialsDisallowed_ThenPasswordIsClear()
        {
            var settings = InstanceConnectionSettings.CreateNew("project", "instance-1");
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vmNode.Object);

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                modelService.Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                settingsService.Object,
                new Mock<ISelectCredentialsWorkflow>().Object,
                new Mock<IRdpCredentialCallbackService>().Object);

            var template = await service
                .PrepareConnectionAsync(vmNode.Object, false)
                .ConfigureAwait(false);
            Assert.IsNotNull(template);

            Assert.AreEqual(RdpSessionParameters.ParameterSources.Inventory, template.Session.Sources);
            Assert.IsTrue(IPAddress.IsLoopback(template.Transport.Endpoint.Address));
            Assert.AreEqual("existinguser", template.Session.Credentials.User);
            Assert.AreEqual("", template.Session.Credentials.Password.AsClearText());
        }

        [Test]
        public async Task WhenConnectingByNodeAndPersistentCredentialsAllowed_ThenCredentialsAreUsed()
        {
            var settings = InstanceConnectionSettings.CreateNew("project", "instance-1");
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            bool settingsSaved = false;

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => settingsSaved = true));

            var vmNode = CreateInstanceNodeMock();

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vmNode.Object);

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                modelService.Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                settingsService.Object,
                new Mock<ISelectCredentialsWorkflow>().Object,
                new Mock<IRdpCredentialCallbackService>().Object);

            var template = await service
                .PrepareConnectionAsync(vmNode.Object, true)
                .ConfigureAwait(false);
            Assert.IsNotNull(template);

            Assert.AreEqual(RdpSessionParameters.ParameterSources.Inventory, template.Session.Sources);
            Assert.IsTrue(IPAddress.IsLoopback(template.Transport.Endpoint.Address));
            Assert.AreEqual("existinguser", template.Session.Credentials.User);
            Assert.AreEqual("password", template.Session.Credentials.Password.AsClearText());

            Assert.IsTrue(settingsSaved);
        }

        //---------------------------------------------------------------------
        // Connect by URL.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnectingByUrlWithoutUsernameAndNoCredentialsExist_ThenConnectionIsMadeWithoutUsername()
        {
            var settingsService = new Mock<IConnectionSettingsService>();

            var credentialPrompt = new Mock<ISelectCredentialsWorkflow>();
            credentialPrompt.Setup(p => p.SelectCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsBase>(),
                    RdpCredentialGenerationBehavior._Default,
                    It.IsAny<bool>())); // Nop -> Connect without configuring credentials.

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IProjectModelNode)null); // Not found

            var callbackService = new Mock<IRdpCredentialCallbackService>();

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                modelService.Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                settingsService.Object,
                credentialPrompt.Object,
                callbackService.Object);

            var template = await service
                .PrepareConnectionAsync(
                    IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance"))
                .ConfigureAwait(false);
            Assert.IsNotNull(template);

            Assert.AreEqual(RdpSessionParameters.ParameterSources.Url, template.Session.Sources);
            Assert.IsTrue(IPAddress.IsLoopback(template.Transport.Endpoint.Address));
            Assert.IsNull(template.Session.Credentials.User);

            settingsService.Verify(s => s.GetConnectionSettings(
                It.IsAny<IProjectModelNode>()), Times.Never);
            callbackService.Verify(s => s.GetCredentialsAsync(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task WhenConnectingByUrlWithUsernameAndNoCredentialsExist_ThenConnectionIsMadeWithThisUsername()
        {
            var settingsService = new Mock<IConnectionSettingsService>();

            var credentialPrompt = new Mock<ISelectCredentialsWorkflow>();
            credentialPrompt
                .Setup(p => p.SelectCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsBase>(),
                    RdpCredentialGenerationBehavior._Default,
                    It.IsAny<bool>())); // Nop -> Connect without configuring credentials.

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IProjectModelNode)null); // Not found

            var callbackService = new Mock<IRdpCredentialCallbackService>();

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                modelService.Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                settingsService.Object,
                credentialPrompt.Object,
                callbackService.Object);

            var template = await service
                .PrepareConnectionAsync(
                    IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance?username=john%20doe"))
                .ConfigureAwait(false);
            Assert.IsNotNull(template);

            Assert.AreEqual(RdpSessionParameters.ParameterSources.Url, template.Session.Sources);
            Assert.IsTrue(IPAddress.IsLoopback(template.Transport.Endpoint.Address));
            Assert.AreEqual("john doe", template.Session.Credentials.User);

            settingsService.Verify(s => s.GetConnectionSettings(
                It.IsAny<IProjectModelNode>()), Times.Never);
            callbackService.Verify(s => s.GetCredentialsAsync(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task WhenConnectingByUrlWithUsernameAndCredentialsExist_ThenConnectionIsMadeWithUsernameFromUrl()
        {
            var settings = InstanceConnectionSettings.CreateNew("project", "instance-1");
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.Instance)
                .Returns(new InstanceLocator("project-1", "zone-1", "instance-1"));

            var credentialPrompt = new Mock<ISelectCredentialsWorkflow>();
            credentialPrompt
                .Setup(p => p.SelectCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsBase>(),
                    RdpCredentialGenerationBehavior._Default,
                    It.IsAny<bool>()));

            var modelService = new Mock<IProjectModelService>();
            modelService
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vmNode.Object);

            var callbackService = new Mock<IRdpCredentialCallbackService>();

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                modelService.Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                settingsService.Object,
                credentialPrompt.Object,
                callbackService.Object);

            var template = await service
                .PrepareConnectionAsync(
                    IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance-1?username=john%20doe"))
                .ConfigureAwait(false);
            Assert.IsNotNull(template);

            Assert.AreEqual(
                RdpSessionParameters.ParameterSources.Inventory | RdpSessionParameters.ParameterSources.Url, 
                template.Session.Sources);
            Assert.IsTrue(IPAddress.IsLoopback(template.Transport.Endpoint.Address));
            Assert.AreEqual("john doe", template.Session.Credentials.User);

            settingsService.Verify(s => s.GetConnectionSettings(
                It.IsAny<IProjectModelNode>()), Times.Once);
            callbackService.Verify(s => s.GetCredentialsAsync(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        //---------------------------------------------------------------------
        // Connect by URL (with credential callback).
        //---------------------------------------------------------------------

        [Test]
        public void WhenConnectingByUrlAndCredentialCallbackFails_ThenPrepareConnectionThrowsException()
        {
            var callbackService = new Mock<IRdpCredentialCallbackService>();
            callbackService.Setup(
                s => s.GetCredentialsAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("mock"));

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                new Mock<IProjectModelService>().Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                new Mock<IConnectionSettingsService>().Object,
                new Mock<ISelectCredentialsWorkflow>().Object,
                callbackService.Object);

            var url = IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance-1?CredentialCallbackUrl=http://mock");

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(
                () => service.PrepareConnectionAsync(url).Wait());

            callbackService.Verify(s => s.GetCredentialsAsync(
                new Uri("http://mock"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WhenConnectingByUrlAndCredentialCallbackSucceeds_ThenConnectionIsMadeWithCallbackCredentials()
        {
            var callbackService = new Mock<IRdpCredentialCallbackService>();
            callbackService.Setup(
                s => s.GetCredentialsAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RdpCredentials(
                    "user",
                    "domain",
                    SecureStringExtensions.FromClearText("password")));

            var service = new RdpConnectionService(
                new Mock<IMainWindow>().Object,
                new Mock<IProjectModelService>().Object,
                CreateTunnelBrokerServiceMock().Object,
                new SynchronousJobService(),
                new Mock<IConnectionSettingsService>().Object,
                new Mock<ISelectCredentialsWorkflow>().Object,
                callbackService.Object);

            var url = IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance-1?username=john%20doe&CredentialCallbackUrl=http://mock");

            var template = await service
                .PrepareConnectionAsync(url)
                .ConfigureAwait(false);

            Assert.IsNotNull(template);
            Assert.AreEqual(RdpSessionParameters.ParameterSources.Url, template.Session.Sources);
            Assert.AreEqual("user", template.Session.Credentials.User);
            Assert.AreEqual("domain", template.Session.Credentials.Domain);
            Assert.AreEqual("password", template.Session.Credentials.Password.AsClearText());

            callbackService.Verify(s => s.GetCredentialsAsync(
                new Uri("http://mock"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
