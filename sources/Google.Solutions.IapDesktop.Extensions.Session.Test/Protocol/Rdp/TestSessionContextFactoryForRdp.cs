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
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Platform.Security.Cryptography;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Testing.Apis;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

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


        private static Mock<IRdpCredentialEditorFactory> CreateRdpCredentialEditorFactoryMock()
        {
            var editor = new Mock<IRdpCredentialEditor>();
            editor.SetupGet(e => e.AllowSave).Returns(true);

            var factory = new Mock<IRdpCredentialEditorFactory>();
            factory
                .Setup(f => f.Edit(It.IsAny<ConnectionSettings>()))
                .Returns(editor.Object);
            return factory;
        }

        //---------------------------------------------------------------------
        // CreateRdpSessionContext - node.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateRdpSessionContext_WhenUsingForcePasswordPromptFlag_ThenCreateRdpSessionContextPrompts()
        {
            var settings = new ConnectionSettings(SampleLocator);
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var editor = new Mock<IRdpCredentialEditor>();
            editor.SetupGet(e => e.AllowSave).Returns(true);

            var editorFactory = new Mock<IRdpCredentialEditorFactory>();
            editorFactory
                .Setup(f => f.Edit(It.IsAny<ConnectionSettings>()))
                .Returns(editor.Object);

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                editorFactory.Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository());

            var context = (RdpContext)await factory
                .CreateRdpSessionContextAsync(
                    CreateInstanceNodeMock().Object,
                    RdpCreateSessionFlags.ForcePasswordPrompt,
                    CancellationToken.None)
                .ConfigureAwait(false);
            Assert.IsNotNull(context);

            Assert.AreEqual(RdpParameters.ParameterSources.Inventory, context.Parameters.Sources);

            editor.Verify(e => e.PromptForCredentials(), Times.Once);
        }

        [Test]
        public async Task CreateRdpSessionContext_WhenUsingDefaultFlags_ThenCreateRdpSessionContextByNodeUsesPersistentCredentials()
        {
            var settings = new ConnectionSettings(SampleLocator);
            settings.RdpUsername.Value = "existinguser";
            settings.RdpPassword.Value = SecureStringExtensions.FromClearText("password");

            var settingsSaved = false;

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => settingsSaved = true));

            var vmNode = CreateInstanceNodeMock();

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                CreateRdpCredentialEditorFactoryMock().Object,
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
        public async Task CreateRdpSessionContext_WhenNoCredentialsExist_ThenCreateRdpSessionContextByUrlUsesEmptyCredentials()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance");

            var settingsService = new Mock<IConnectionSettingsService>();
            var foundInInventory = false;
            settingsService
                .Setup(s => s.GetConnectionSettings(url, out foundInInventory))
                .Returns(new ConnectionSettings(url.Instance));

            var callbackService = new Mock<IRdpCredentialCallback>();

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                CreateRdpCredentialEditorFactoryMock().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository());

            var context = (RdpContext)await factory
                .CreateRdpSessionContextAsync(
                    url,
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
        public async Task CreateRdpSessionContext_WhenCredentialsExist_ThenCreateRdpSessionContextByUrlUsesStoredCredentials()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance-1?username=john%20doe");
            var settings = new ConnectionSettings(url.Instance);
            settings.RdpUsername.Value = "john doe";

            var settingsService = new Mock<IConnectionSettingsService>();
            var foundInInventory = true;
            settingsService
                .Setup(s => s.GetConnectionSettings(url, out foundInInventory))
                .Returns(settings);

            var callbackService = new Mock<IRdpCredentialCallback>();

            var factory = new SessionContextFactory(
                new Mock<IMainWindow>().Object,
                new Mock<IAuthorization>().Object,
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                CreateRdpCredentialEditorFactoryMock().Object,
                new Mock<IRdpCredentialCallback>().Object,
                CreateSshSettingsRepository());

            var context = (RdpContext)await factory
                .CreateRdpSessionContextAsync(url, CancellationToken.None)
                .ConfigureAwait(false);
            Assert.IsNotNull(context);

            Assert.AreEqual(
                RdpParameters.ParameterSources.Inventory | RdpParameters.ParameterSources.Url,
                context.Parameters.Sources);
            Assert.AreEqual("john doe", context.Credential.User);

            settingsService.Verify(s => s.GetConnectionSettings(
                url,
                out foundInInventory), Times.Once);
            callbackService.Verify(s => s.GetCredentialsAsync(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        //---------------------------------------------------------------------
        // CreateRdpSessionContext - url + callback.
        //---------------------------------------------------------------------

        [Test]
        public void CreateRdpSessionContext_WhenUrlCallbackFails_ThenCreateRdpSessionContextByUrlThrowsException()
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
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                new Mock<IConnectionSettingsService>().Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                CreateRdpCredentialEditorFactoryMock().Object,
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
        public async Task CreateRdpSessionContext_WhenUrlCallbackSucceeds_ThenCreateRdpSessionContextByUrlUsesCallbackCredentials()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///project/us-central-1/instance-1?username=john%20doe&CredentialCallbackUrl=http://mock");

            var settingsService = new Mock<IConnectionSettingsService>();
            var foundInInventory = false;
            settingsService
                .Setup(s => s.GetConnectionSettings(url, out foundInInventory))
                .Returns(new ConnectionSettings(url.Instance));

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
                new Mock<IKeyStore>().Object,
                new Mock<IPlatformCredentialFactory>().Object,
                settingsService.Object,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                CreateRdpCredentialEditorFactoryMock().Object,
                callbackService.Object,
                CreateSshSettingsRepository());

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
