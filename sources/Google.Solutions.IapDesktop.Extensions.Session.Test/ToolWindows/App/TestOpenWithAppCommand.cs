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
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.App
{
    [TestFixture]
    public class TestOpenWithClientCommand
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static readonly string CmdExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\cmd.exe";

        private static IProjectModelInstanceNode CreateInstanceNode()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(i => i.Instance).Returns(SampleLocator);
            return node.Object;
        }

        private AppContextFactory CreateFactory(
            IAppProtocolClient client,
            InstanceConnectionSettings settings)
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            return new AppContextFactory(
                new AppProtocol(
                    "app-1",
                    Enumerable.Empty<ITrait>(),
                    new Mock<ITransportPolicy>().Object,
                    80,
                    null,
                    client),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                settingsService.Object);
        }

        //---------------------------------------------------------------------
        // IsAvailable.
        //---------------------------------------------------------------------

        [Test]
        public void WhenContextOfWrongType_ThenQueryStateReturnsUnavailable()
        {
            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(new Mock<IAppProtocolClient>().Object, null),
                new Mock<ICredentialDialog>().Object);

            Assert.AreEqual(
                CommandState.Unavailable,
                command.QueryState(new Mock<IProjectModelCloudNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                command.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                command.QueryState(new Mock<IProjectModelZoneNode>().Object));
        }

        [Test]
        public void WhenClientUnavailable_ThenQueryStateReturnsDisabled()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(false);

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                new Mock<ICredentialDialog>().Object);

            Assert.AreEqual(
                CommandState.Disabled,
                command.QueryState(new Mock<IProjectModelInstanceNode>().Object));
        }

        //---------------------------------------------------------------------
        // Icon.
        //---------------------------------------------------------------------

        [Test]
        public void WhenClientExecutableNotFound_ThenImageIsNull()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.Executable).Returns("doesnotexist.exe");

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                new Mock<ICredentialDialog>().Object);

            Assert.IsNull(command.Image);
        }

        [Test]
        public void WhenClientExecutableFound_ThenImageIsAvailable()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.Executable).Returns(CmdExe);

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                new Mock<ICredentialDialog>().Object);

            Assert.IsNotNull(command.Image);
        }

        //---------------------------------------------------------------------
        // CreateContext - non-NLA.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenClientDoesNotSupportNla_ThenCreateContextResetsNetworkCredential()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(false);
            client.SetupGet(c => c.IsUsernameRequired).Returns(false);

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(
                    client.Object,
                    InstanceConnectionSettings.CreateNew(SampleLocator)),
                new Mock<ICredentialDialog>().Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(context.NetworkCredential);
        }

        //---------------------------------------------------------------------
        // CreateContext - non-NLA with username required.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsernameRequiredAndPresent_ThenCreateContextReturns()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(false);
            client.SetupGet(c => c.IsUsernameRequired).Returns(true);

            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.AppUsername.StringValue = "user";

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                new Mock<ICredentialDialog>().Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(context.NetworkCredential);
            Assert.AreEqual("user", context.Parameters.PreferredUsername);
        }

        [Test]
        public async Task WhenUsernameRequiredButMissing_ThenCreateContextPromptsForUsername()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);
            client.SetupGet(c => c.IsUsernameRequired).Returns(true);

            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.AppNetworkLevelAuthentication.EnumValue = AppNetworkLevelAuthenticationState.Disabled;

            var username = "user";
            var dialog = new Mock<ICredentialDialog>();
            dialog
                .Setup(d => d.PromptForUsername(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    out username))
                .Returns(DialogResult.OK);

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(context.NetworkCredential);
            Assert.AreEqual("user", context.Parameters.PreferredUsername);
        }

        [Test]
        public async Task WhenUsernameRequiredAndPromptForced_ThenCreateContextPromptsForUsername()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);
            client.SetupGet(c => c.IsUsernameRequired).Returns(true);

            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.AppNetworkLevelAuthentication.EnumValue = AppNetworkLevelAuthenticationState.Disabled;
            settings.AppUsername.StringValue = "ignore";

            var username = "user";
            var dialog = new Mock<ICredentialDialog>();
            dialog
                .Setup(d => d.PromptForUsername(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    out username))
                .Returns(DialogResult.OK);

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object,
                true);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(context.NetworkCredential);
            Assert.AreEqual("user", context.Parameters.PreferredUsername);
        }

        [Test]
        public void WhenUsernamePromptCancelled_ThenCreateContextThrowsException()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);
            client.SetupGet(c => c.IsUsernameRequired).Returns(true);

            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.AppNetworkLevelAuthentication.EnumValue = AppNetworkLevelAuthenticationState.Disabled;

            string username = null;
            var dialog = new Mock<ICredentialDialog>();
            dialog
                .Setup(d => d.PromptForUsername(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    out username))
                .Returns(DialogResult.Cancel);

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => command
                    .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                    .Wait());
        }

        //---------------------------------------------------------------------
        // CreateContext - NLA.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNlaEnabledAndCredentialsPresent_ThenCreateContextReturns()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);

            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.AppNetworkLevelAuthentication.EnumValue = AppNetworkLevelAuthenticationState.Enabled;
            settings.RdpUsername.StringValue = "user";
            settings.RdpPassword.ClearTextValue = "password";
            settings.RdpDomain.StringValue = "domain";

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                new Mock<ICredentialDialog>().Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(context.NetworkCredential);
            Assert.AreEqual("user", context.NetworkCredential.UserName);
            Assert.AreEqual("domain", context.NetworkCredential.Domain);
            Assert.AreEqual("password", context.NetworkCredential.Password);
        }


        [Test]
        public async Task WhenNlaEnabledButCredentialsMissing_ThenCreateContextPromptsForWindowsCredentials()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);

            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.AppNetworkLevelAuthentication.EnumValue = AppNetworkLevelAuthenticationState.Enabled;

            var userCredential = new NetworkCredential("user", "password", "domain");
            var dialog = new Mock<ICredentialDialog>();
            dialog
                .Setup(d => d.PromptForWindowsCredentials(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthenticationPackage>(),
                    out userCredential))
                .Returns(DialogResult.OK);

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(context.NetworkCredential);
            Assert.AreEqual("user", context.NetworkCredential.UserName);
            Assert.AreEqual("domain", context.NetworkCredential.Domain);
            Assert.AreEqual("password", context.NetworkCredential.Password);
        }

        [Test]
        public async Task WhenNlaEnabledAndPromptForced_ThenCreateContextPromptsForWindowsCredentials()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);

            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.AppNetworkLevelAuthentication.EnumValue = AppNetworkLevelAuthenticationState.Enabled;
            settings.RdpUsername.StringValue = "ignore";
            settings.RdpPassword.ClearTextValue = "ignore";
            settings.RdpDomain.StringValue = "ignore";

            var userCredential = new NetworkCredential("user", "password", "domain");
            var dialog = new Mock<ICredentialDialog>();
            dialog
                .Setup(d => d.PromptForWindowsCredentials(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthenticationPackage>(),
                    out userCredential))
                .Returns(DialogResult.OK);

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object,
                true);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(context.NetworkCredential);
            Assert.AreEqual("user", context.NetworkCredential.UserName);
            Assert.AreEqual("domain", context.NetworkCredential.Domain);
            Assert.AreEqual("password", context.NetworkCredential.Password);
        }

        [Test]
        public void WhenWindowsCredentialPromptCancelled_ThenCreateContextThrowsException()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);

            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.AppNetworkLevelAuthentication.EnumValue = AppNetworkLevelAuthenticationState.Enabled;

            NetworkCredential userCredential = null;
            var dialog = new Mock<ICredentialDialog>();
            dialog
                .Setup(d => d.PromptForWindowsCredentials(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthenticationPackage>(),
                    out userCredential))
                .Returns(DialogResult.Cancel);

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => command
                    .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                    .Wait());
        }

        //---------------------------------------------------------------------
        // ConnectContext.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenClientLaunchable_ThenConnectContextLaunchesProcess()
        {
            var client = new Mock<IWindowsProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);

            var process = new Mock<IWin32Process>();
            var processFactory = new Mock<IWin32ProcessFactory>();
            processFactory
                .Setup(f => f.CreateProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(process.Object);

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(InstanceConnectionSettings
                    .CreateNew(SampleLocator)
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var factory = new AppContextFactory(
                new AppProtocol(
                    "app-1",
                    Enumerable.Empty<ITrait>(),
                    new Mock<ITransportPolicy>().Object,
                    80,
                    null,
                    client.Object),
                new Mock<IIapTransportFactory>().Object,
                processFactory.Object,
                settingsService.Object);

            var command = new OpenWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                factory,
                new Mock<ICredentialDialog>().Object);

            await command
                .ExecuteAsync(CreateInstanceNode())
                .ConfigureAwait(false);

            process.Verify(p => p.Resume(), Times.Once);
        }
    }
}
