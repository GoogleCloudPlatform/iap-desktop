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
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.Mocks;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.App
{
    [TestFixture]
    public class TestOpenWithAppCommand
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static IProjectModelInstanceNode CreateInstanceNode()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(i => i.Instance).Returns(SampleLocator);
            return node.Object;
        }

        private AppContextFactory CreateFactory(
            IAppProtocolClient client,
            NetworkCredential savedRdpCredentials)
        {
            var settings = InstanceConnectionSettings.CreateNew(SampleLocator);
            settings.RdpUsername.StringValue = savedRdpCredentials?.UserName;
            settings.RdpPassword.ClearTextValue = savedRdpCredentials?.Password;
            settings.RdpDomain.StringValue = savedRdpCredentials?.Domain;

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
            var command = new OpenWithAppCommand(
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
            var client = new Mock<IWindowsAppClient>();
            client.SetupGet(c => c.IsAvailable).Returns(false);

            var command = new OpenWithAppCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                new Mock<ICredentialDialog>().Object);

            Assert.AreEqual(
                CommandState.Disabled,
                command.QueryState(new Mock<IProjectModelInstanceNode>().Object));
        }

        //---------------------------------------------------------------------
        // CreateContext.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenWindowsClientRequiresDefaultCredential_ThenCreateContextUsesNullCredential()
        {
            var client = new Mock<IWindowsAppClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.RequiredCredential).Returns(NetworkCredentialType.Default);

            var command = new OpenWithAppCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                new Mock<ICredentialDialog>().Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(context.NetworkCredential);
        }

        [Test]
        public async Task WhenWindowsClientRequiresRdpCredential_ThenCreateContextUsesRdpCredential()
        {
            var client = new Mock<IWindowsAppClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.RequiredCredential).Returns(NetworkCredentialType.Rdp);

            var rdpCredential = new NetworkCredential("user", "password", "domain");
            var command = new OpenWithAppCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, rdpCredential),
                new Mock<ICredentialDialog>().Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(rdpCredential.UserName, context.NetworkCredential.UserName);
            Assert.AreEqual(rdpCredential.Domain, context.NetworkCredential.Domain);
            Assert.AreEqual(rdpCredential.Password, context.NetworkCredential.Password);
        }

        [Test]
        public void WhenWindowsClientRequiresPromptCredentialAndPromptCancelled_ThenCreateContextThrowsException()
        {
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

            var client = new Mock<IWindowsAppClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.RequiredCredential).Returns(NetworkCredentialType.Prompt);

            var command = new OpenWithAppCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                dialog.Object);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => command
                    .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                    .Wait());
        }

        [Test]
        public async Task WhenWindowsClientRequiresPromptCredential_ThenCreateContextPromptsUserForCredential()
        {
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

            var client = new Mock<IWindowsAppClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.RequiredCredential).Returns(NetworkCredentialType.Prompt);

            var command = new OpenWithAppCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, new NetworkCredential("notused", "notused", "notused")),
                dialog.Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(userCredential.UserName, context.NetworkCredential.UserName);
            Assert.AreEqual(userCredential.Domain, context.NetworkCredential.Domain);
            Assert.AreEqual(userCredential.Password, context.NetworkCredential.Password);
        }

        [Test]
        public async Task WhenWindowsClientRequiresRdpCredentialAndNoCredentialFound_ThenCreateContextPromptsUserForCredential()
        {
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

            var client = new Mock<IWindowsAppClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.RequiredCredential).Returns(NetworkCredentialType.Rdp);

            var command = new OpenWithAppCommand(
                new Mock<IWin32Window>().Object,
                new SynchronousJobService(),
                CreateFactory(client.Object, null),
                dialog.Object);

            var context = await command
                .CreateContextAsync(CreateInstanceNode(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(userCredential.UserName, context.NetworkCredential.UserName);
            Assert.AreEqual(userCredential.Domain, context.NetworkCredential.Domain);
            Assert.AreEqual(userCredential.Password, context.NetworkCredential.Password);
        }

        //---------------------------------------------------------------------
        // ConnectContext.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenClientLaunchable_ThenConnectContextLaunchesProcess()
        {
            var client = new Mock<IWindowsAppClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.RequiredCredential).Returns(NetworkCredentialType.Default);

            var process = new Mock<IWin32Process>();
            var processFactory = new Mock<IWin32ProcessFactory>();
            processFactory
                .Setup(f => f.CreateProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(process.Object);

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
                new Mock<IConnectionSettingsService>().Object);

            var command = new OpenWithAppCommand(
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
