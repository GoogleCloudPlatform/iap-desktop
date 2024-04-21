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
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
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
    public class TestConnectAppProtocolWithClientCommand
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

        private AppProtocolContextFactory CreateFactory(
            IAppProtocolClient client,
            Extensions.Session.Settings.ConnectionSettings? settings)
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            if (settings != null)
            {
                settingsService
                    .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                    .Returns(settings.ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));
            }

            return new AppProtocolContextFactory(
                new AppProtocol(
                    "app-1",
                    Enumerable.Empty<ITrait>(),
                    80,
                    null,
                    client),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                settingsService.Object);
        }


        //---------------------------------------------------------------------
        // Id.
        //---------------------------------------------------------------------

        [Test]
        public void Id()
        {
            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(new Mock<IAppProtocolClient>().Object, null),
                new Mock<ICredentialDialog>().Object,
                new Mock<INotifyDialog>().Object);

            Assert.AreEqual(
                $"{command.GetType().Name}.app-1",
                command.Id);
        }

        //---------------------------------------------------------------------
        // IsAvailable.
        //---------------------------------------------------------------------

        [Test]
        public void WhenContextOfWrongType_ThenQueryStateReturnsUnavailable()
        {
            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(new Mock<IAppProtocolClient>().Object, null),
                new Mock<ICredentialDialog>().Object,
                new Mock<INotifyDialog>().Object);

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
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(false);

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                new Mock<ICredentialDialog>().Object,
                new Mock<INotifyDialog>().Object);

            Assert.AreEqual(
                CommandState.Disabled,
                command.QueryState(new Mock<IProjectModelInstanceNode>().Object));
        }

        //---------------------------------------------------------------------
        // Icon.
        //---------------------------------------------------------------------

        [Test]
        public void WhenClientNotAvailable_ThenImageIsNull()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(false);
            client.SetupGet(c => c.Executable).Throws(new Exception());

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                new Mock<ICredentialDialog>().Object,
                new Mock<INotifyDialog>().Object);

            Assert.IsNull(command.Image);
        }

        [Test]
        public void WhenClientAvailableButExecutableNotFound_ThenImageIsNull()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.Executable).Returns("doesnotexist.exe");

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                new Mock<ICredentialDialog>().Object,
                new Mock<INotifyDialog>().Object);

            Assert.IsNull(command.Image);
        }

        [Test]
        public void WhenClientAvailable_ThenImageIsAvailable()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.Executable).Returns(CmdExe);

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                new Mock<ICredentialDialog>().Object,
                new Mock<INotifyDialog>().Object);

            Assert.IsNotNull(command.Image);
        }

        //---------------------------------------------------------------------
        // CreateContext - non-NLA.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenClientDoesNotSupportNla_ThenCreateContextResetsNetworkCredential()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(false);
            client.SetupGet(c => c.IsUsernameRequired).Returns(false);

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(
                    client.Object,
                    new Extensions.Session.Settings.ConnectionSettings(SampleLocator)),
                new Mock<ICredentialDialog>().Object,
                new Mock<INotifyDialog>().Object);

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
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(false);
            client.SetupGet(c => c.IsUsernameRequired).Returns(true);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleLocator);
            settings.AppUsername.Value = "user";

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                new Mock<ICredentialDialog>().Object,
                new Mock<INotifyDialog>().Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(context.NetworkCredential);
            Assert.AreEqual("user", context.Parameters.PreferredUsername);
        }

        [Test]
        public async Task WhenUsernameRequiredButMissing_ThenCreateContextPromptsForUsername()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);
            client.SetupGet(c => c.IsUsernameRequired).Returns(true);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleLocator);
            settings.AppNetworkLevelAuthentication.Value = AppNetworkLevelAuthenticationState.Disabled;

            var username = "user";
            var dialog = new Mock<ICredentialDialog>();
            dialog
                .Setup(d => d.PromptForUsername(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    out username))
                .Returns(DialogResult.OK);

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object,
                new Mock<INotifyDialog>().Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(context.NetworkCredential);
            Assert.AreEqual("user", context.Parameters.PreferredUsername);
        }

        [Test]
        public async Task WhenUsernameRequiredAndPromptForced_ThenCreateContextPromptsForUsername()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);
            client.SetupGet(c => c.IsUsernameRequired).Returns(true);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleLocator);
            settings.AppNetworkLevelAuthentication.Value = AppNetworkLevelAuthenticationState.Disabled;
            settings.AppUsername.Value = "ignore";

            var username = "user";
            var dialog = new Mock<ICredentialDialog>();
            dialog
                .Setup(d => d.PromptForUsername(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    out username))
                .Returns(DialogResult.OK);

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object,
                new Mock<INotifyDialog>().Object,
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
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);
            client.SetupGet(c => c.IsUsernameRequired).Returns(true);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleLocator);
            settings.AppNetworkLevelAuthentication.Value = AppNetworkLevelAuthenticationState.Disabled;

            string? username = null;
            var dialog = new Mock<ICredentialDialog>();
            dialog
                .Setup(d => d.PromptForUsername(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    out username))
                .Returns(DialogResult.Cancel);

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object,
                new Mock<INotifyDialog>().Object);

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
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleLocator);
            settings.AppNetworkLevelAuthentication.Value = AppNetworkLevelAuthenticationState.Enabled;
            settings.RdpUsername.Value = "user";
            settings.RdpPassword.SetClearTextValue("password");
            settings.RdpDomain.Value = "domain";

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                new Mock<ICredentialDialog>().Object,
                new Mock<INotifyDialog>().Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(context.NetworkCredential);
            Assert.AreEqual("user", context.NetworkCredential!.UserName);
            Assert.AreEqual("domain", context.NetworkCredential.Domain);
            Assert.AreEqual("password", context.NetworkCredential.Password);
        }


        [Test]
        public async Task WhenNlaEnabledButCredentialsMissing_ThenCreateContextPromptsForWindowsCredentials()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleLocator);
            settings.AppNetworkLevelAuthentication.Value = AppNetworkLevelAuthenticationState.Enabled;

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

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object,
                new Mock<INotifyDialog>().Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(context.NetworkCredential);
            Assert.AreEqual("user", context.NetworkCredential!.UserName);
            Assert.AreEqual("domain", context.NetworkCredential.Domain);
            Assert.AreEqual("password", context.NetworkCredential.Password);
        }

        [Test]
        public async Task WhenNlaEnabledAndPromptForced_ThenCreateContextPromptsForWindowsCredentials()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleLocator);
            settings.AppNetworkLevelAuthentication.Value = AppNetworkLevelAuthenticationState.Enabled;
            settings.RdpUsername.Value = "ignore";
            settings.RdpPassword.SetClearTextValue("ignore");
            settings.RdpDomain.Value = "ignore";

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

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object,
                new Mock<INotifyDialog>().Object,
                true);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(context.NetworkCredential);
            Assert.AreEqual("user", context!.NetworkCredential!.UserName);
            Assert.AreEqual("domain", context.NetworkCredential.Domain);
            Assert.AreEqual("password", context.NetworkCredential.Password);
        }

        [Test]
        public void WhenWindowsCredentialPromptCancelled_ThenCreateContextThrowsException()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.IsNetworkLevelAuthenticationSupported).Returns(true);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleLocator);
            settings.AppNetworkLevelAuthentication.Value = AppNetworkLevelAuthenticationState.Enabled;

            NetworkCredential? userCredential = null;
            var dialog = new Mock<ICredentialDialog>();
            dialog
                .Setup(d => d.PromptForWindowsCredentials(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthenticationPackage>(),
                    out userCredential))
                .Returns(DialogResult.Cancel);

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, settings),
                dialog.Object,
                new Mock<INotifyDialog>().Object);

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
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);

            var process = new Mock<IWin32Process>();
            var processFactory = new Mock<IWin32ProcessFactory>();
            processFactory
                .Setup(f => f.CreateProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(process.Object);

            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.GetConnectionSettings(It.IsAny<IProjectModelNode>()))
                .Returns(new Extensions.Session.Settings.ConnectionSettings(SampleLocator)
                    .ToPersistentSettingsCollection(s => Assert.Fail("should not be called")));

            var factory = new AppProtocolContextFactory(
                new AppProtocol(
                    "app-1",
                    Enumerable.Empty<ITrait>(),
                    80,
                    null,
                    client.Object),
                new Mock<IIapTransportFactory>().Object,
                processFactory.Object,
                settingsService.Object);

            var command = new ConnectAppProtocolWithClientCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                factory,
                new Mock<ICredentialDialog>().Object,
                new Mock<INotifyDialog>().Object);

            await command
                .ExecuteAsync(CreateInstanceNode())
                .ConfigureAwait(false);

            process.Verify(p => p.Resume(), Times.Once);
        }
    }
}
