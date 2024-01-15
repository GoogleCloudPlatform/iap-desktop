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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.Platform.Cryptography;
using Google.Solutions.Testing.Apis;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Rdp
{
    [TestFixture]
    public class TestSessionContextFactoryForRdp
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private Mock<IProjectModelInstanceNode> CreateInstanceNodeMock()
        {
            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);
            vmNode.SetupGet(n => n.Instance)
                .Returns(new InstanceLocator("project-1", "zone-1", "instance-1"));

            return vmNode;
        }

        private SshSettingsRepository CreateSshSettingsRepository()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            hkcu.DeleteSubKeyTree(@"Software\Google\__Test", false);

            return new SshSettingsRepository(
                hkcu.CreateSubKey(@"Software\Google\__Test"),
                null,
                null,
                UserProfile.SchemaVersion.Current);
        }

        //---------------------------------------------------------------------
        // CreateRdpSessionContext - node.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsingForcePasswordPromptFlag_ThenCreateRdpSessionContextByNodeUsesClearPassword()
        {
            var settings = new ConnectionSettingsBase(SampleLocator);
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = CreateInstanceNodeMock();

            var workspace = new Mock<IProjectWorkspace>();
            workspace
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vmNode.Object);

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                workspace.Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository());

            var context = (RdpContext)await factory
                .CreateRdpSessionContextAsync(
                    vmNode.Object,
                    RdpCreateSessionFlags.ForcePasswordPrompt,
                    CancellationToken.None)
                .ConfigureAwait(false);
            Assert.IsNotNull(context);

            Assert.AreEqual(RdpParameters.ParameterSources.Inventory, context.Parameters.Sources);
            Assert.AreEqual("existinguser", context.Credential.User);
            Assert.AreEqual("", context.Credential.Password.AsClearText());
        }


        [Test]
        public async Task WhenUsingDefaultFlags_ThenCreateRdpSessionContextByNodeUsesPersistentCredentials()
        {
            var settings = new ConnectionSettingsBase(SampleLocator);
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            var settingsSaved = false;

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => settingsSaved = true));

            var vmNode = CreateInstanceNodeMock();

            var workspace = new Mock<IProjectWorkspace>();
            workspace
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vmNode.Object);

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                workspace.Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository());

            var context = (RdpContext)await factory
                .CreateRdpSessionContextAsync(
                    vmNode.Object,
                    RdpCreateSessionFlags.None,
                    CancellationToken.None)
                .ConfigureAwait(false);
            Assert.IsNotNull(context);


            Assert.AreEqual(RdpParameters.ParameterSources.Inventory, context.Parameters.Sources);
            Assert.AreEqual("existinguser", context.Credential.User);
            Assert.AreEqual("password", context.Credential.Password.AsClearText());

            Assert.IsTrue(settingsSaved);
        }

        //---------------------------------------------------------------------
        // CreateRdpSessionContext - url.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNoCredentialsExist_ThenCreateRdpSessionContextByUrlUsesEmptyCredentials()
        {
            var settingsService = new Mock<IConnectionSettingsService>();

            var credentialDialog = new Mock<ISelectCredentialsDialog>();
            credentialDialog.Setup(p => p.SelectCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsBase>(),
                    RdpCredentialGenerationBehavior._Default,
                    It.IsAny<bool>())); // Nop -> Connect without configuring credentials.

            var workspace = new Mock<IProjectWorkspace>();
            workspace
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IProjectModelNode)null); // Not found

            var callbackService = new Mock<IRdpCredentialCallback>();

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                workspace.Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                credentialDialog.Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository());

            var context = (RdpContext)await factory
                .CreateRdpSessionContextAsync(
                    IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance"),
                    CancellationToken.None)
                .ConfigureAwait(false);
            Assert.IsNotNull(context);

            Assert.AreEqual(RdpParameters.ParameterSources.Url, context.Parameters.Sources);
            Assert.IsNull(context.Credential.User);

            settingsService.Verify(s => s.GetConnectionSettings(
                It.IsAny<IProjectModelNode>()), Times.Never);
            callbackService.Verify(s => s.GetCredentialsAsync(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task WhenUrlContainsUsername_ThenCreateRdpSessionContextByUrlUsesUsernameFromUrl()
        {
            var settingsService = new Mock<IConnectionSettingsService>();

            var credentialDialog = new Mock<ISelectCredentialsDialog>();
            credentialDialog
                .Setup(p => p.SelectCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsBase>(),
                    RdpCredentialGenerationBehavior._Default,
                    It.IsAny<bool>())); // Nop -> Connect without configuring credentials.

            var workspace = new Mock<IProjectWorkspace>();
            workspace
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IProjectModelNode)null); // Not found

            var callbackService = new Mock<IRdpCredentialCallback>();

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                workspace.Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                credentialDialog.Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository());

            var context = (RdpContext)await factory
                .CreateRdpSessionContextAsync(
                    IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance?username=john%20doe"),
                    CancellationToken.None)
                .ConfigureAwait(false);
            Assert.IsNotNull(context);

            Assert.AreEqual(RdpParameters.ParameterSources.Url, context.Parameters.Sources);
            Assert.AreEqual("john doe", context.Credential.User);

            settingsService.Verify(s => s.GetConnectionSettings(
                It.IsAny<IProjectModelNode>()), Times.Never);
            callbackService.Verify(s => s.GetCredentialsAsync(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task WhenUrlContainsUsernameAndCredentialsExist_ThenCreateRdpSessionContextByUrlUsesUsernameFromUrl()
        {
            var settings = new ConnectionSettingsBase(SampleLocator);
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var vmNode = new Mock<IProjectModelInstanceNode>();
            vmNode.SetupGet(n => n.Instance)
                .Returns(new InstanceLocator("project-1", "zone-1", "instance-1"));

            var credentialDialog = new Mock<ISelectCredentialsDialog>();
            credentialDialog
                .Setup(p => p.SelectCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsBase>(),
                    RdpCredentialGenerationBehavior._Default,
                    It.IsAny<bool>()));

            var workspace = new Mock<IProjectWorkspace>();
            workspace
                .Setup(p => p.GetNodeAsync(
                    It.IsAny<ResourceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vmNode.Object);

            var callbackService = new Mock<IRdpCredentialCallback>();

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                workspace.Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                credentialDialog.Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository());

            var context = (RdpContext)await factory
                .CreateRdpSessionContextAsync(
                    IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance-1?username=john%20doe"),
                    CancellationToken.None)
                .ConfigureAwait(false);
            Assert.IsNotNull(context);

            Assert.AreEqual(
                RdpParameters.ParameterSources.Inventory | RdpParameters.ParameterSources.Url,
                context.Parameters.Sources);
            Assert.AreEqual("john doe", context.Credential.User);

            settingsService.Verify(s => s.GetConnectionSettings(
                It.IsAny<IProjectModelNode>()), Times.Once);
            callbackService.Verify(s => s.GetCredentialsAsync(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        //---------------------------------------------------------------------
        // CreateRdpSessionContext - url + callback.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUrlCallbackFails_ThenCreateRdpSessionContextByUrlThrowsException()
        {
            var callbackService = new Mock<IRdpCredentialCallback>();
            callbackService.Setup(
                s => s.GetCredentialsAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("mock"));

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                new Mock<IProjectWorkspace>().Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                new Mock<IConnectionSettingsService>().Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                callbackService.Object,
                CreateSshSettingsRepository());

            var url = IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance-1?CredentialCallbackUrl=http://mock");

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(
                () => factory.CreateRdpSessionContextAsync(url, CancellationToken.None).Wait());

            callbackService.Verify(s => s.GetCredentialsAsync(
                new Uri("http://mock"),
                It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        public async Task WhenUrlCallbackSucceeds_ThenCreateRdpSessionContextByUrlUsesCallbackCredentials()
        {
            var callbackService = new Mock<IRdpCredentialCallback>();
            callbackService.Setup(
                s => s.GetCredentialsAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RdpCredential(
                    "user",
                    "domain",
                    SecureStringExtensions.FromClearText("password")));

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                new Mock<IProjectWorkspace>().Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                new Mock<IConnectionSettingsService>().Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<ISelectCredentialsDialog>().Object,
                callbackService.Object,
                CreateSshSettingsRepository());

            var url = IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance-1?username=john%20doe&CredentialCallbackUrl=http://mock");

            var context = (RdpContext)await factory
                .CreateRdpSessionContextAsync(url, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(context);
            Assert.AreEqual(RdpParameters.ParameterSources.Url, context.Parameters.Sources);
            Assert.AreEqual("user", context.Credential.User);
            Assert.AreEqual("domain", context.Credential.Domain);
            Assert.AreEqual("password", context.Credential.Password.AsClearText());

            callbackService.Verify(s => s.GetCredentialsAsync(
                new Uri("http://mock"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
