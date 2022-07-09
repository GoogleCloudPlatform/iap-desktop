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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Locator;
using Google.Solutions.Support.Nunit.Integration;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Test.Services;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Ssh.Native;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.SshTerminal
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestSshTerminalPaneViewModel : ShellFixtureBase
    {
        private static async Task<IPAddress> PublicAddressFromLocator(
            InstanceLocator instanceLocator)
        {
            using (var service = TestProject.CreateComputeService())
            {
                var instance = await service
                    .Instances.Get(
                            instanceLocator.ProjectId,
                            instanceLocator.Zone,
                            instanceLocator.Name)
                    .ExecuteAsync()
                    .ConfigureAwait(true);
                return instance.PublicAddress();
            }
        }

        private static Mock<IAuthorizationSource> CreateAuthorizationSource()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("test@example.com");
            var authorizationSource = new Mock<IAuthorizationSource>();
            authorizationSource
                .Setup(a => a.Authorization)
                .Returns(authorization.Object);

            return authorizationSource;
        }

        private static async Task<AuthorizedKeyPair> CreateAuthorizedKeyAsync(
            InstanceLocator instance,
            ICredential credential,
            SshKeyType keyType)
        {
            var authorizationSource = CreateAuthorizationSource();

            using (var keyAdapter = new KeyAuthorizationService(
                authorizationSource.Object,
                new ComputeEngineAdapter(credential),
                new ResourceManagerAdapter(credential),
                new Mock<IOsLoginService>().Object))
            {
                return await keyAdapter.AuthorizeKeyAsync(
                        instance,
                        SshKeyPair.NewEphemeralKeyPair(keyType),
                        TimeSpan.FromMinutes(10),
                        null,
                        KeyAuthorizationMethods.InstanceMetadata,
                        CancellationToken.None)
                    .ConfigureAwait(true);
            }
        }

        private async Task<SshTerminalPaneViewModel> CreateViewModelAsync(
            IEventService eventService,
            InstanceLocator instance,
            ICredential credential,
            SshKeyType keyType,
            CultureInfo language = null,
            IConfirmationDialog confirmationDialog = null)
        {
            var authorizedKey = await CreateAuthorizedKeyAsync(
                    instance,
                    credential,
                    keyType)
                .ConfigureAwait(false);

            var address = await PublicAddressFromLocator(instance)
                .ConfigureAwait(true);

            var progressOperation = new Mock<IOperation>();
            progressOperation
                .SetupGet(p => p.CancellationToken)
                .Returns(CancellationToken.None);
            var progressDialog = new Mock<IOperationProgressDialog>();
            progressDialog
                .Setup(d => d.ShowCopyDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<ulong>(),
                    It.IsAny<ulong>()))
                .Returns(progressOperation.Object);

            return new SshTerminalPaneViewModel(
                eventService,
                new SynchronousJobService(),
                confirmationDialog ?? new Mock<IConfirmationDialog>().Object,
                progressDialog.Object,
                instance,
                new IPEndPoint(address, 22),
                authorizedKey,
                language,
                TimeSpan.FromSeconds(10));
        }

        //---------------------------------------------------------------------
        // ITextTerminal.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDataReceived_ThenEventFires(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var eventService = new Mock<IEventService>();

            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    eventService.Object,
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.Rsa3072,
                    null)
                .ConfigureAwait(true))
            {
                viewModel.View = window;

                DataEventArgs argsReceived = null;
                viewModel.DataReceived += (s, a) =>
                {
                    argsReceived = a;
                };

                var terminal = (ITextTerminal)viewModel;
                terminal.OnDataReceived("some data");

                Assert.AreEqual("some data", argsReceived.Data);
            }
        }

        [Test]
        public async Task WhenConectionLostErrorReceived_ThenEventFires(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var eventService = new Mock<IEventService>();

            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    eventService.Object,
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.Rsa3072,
                    null)
                .ConfigureAwait(true))
            {
                viewModel.View = window;

                await viewModel
                    .ConnectAsync(new TerminalSize(80, 24))
                    .ConfigureAwait(false);

                ConnectionErrorEventArgs argsReceived = null;
                viewModel.ConnectionLost += (s, a) =>
                {
                    argsReceived = a;
                };

                var terminal = (ITextTerminal)viewModel;

                terminal.OnError(
                    TerminalErrorType.ConnectionLost, 
                    new ArgumentException());

                Assert.IsInstanceOf<ArgumentException>(argsReceived.Error);
                eventService.Verify(s => s.FireAsync(
                    It.IsAny<SessionAbortedEvent>()), Times.Once());
            }

            await SshWorkerThread
                .JoinAllWorkerThreadsAsync()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenConectionFailedErrorReceived_ThenEventFires(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var eventService = new Mock<IEventService>();

            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    eventService.Object,
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.Rsa3072,
                    null)
                .ConfigureAwait(true))
            {
                viewModel.View = window;

                ConnectionErrorEventArgs argsReceived = null;
                viewModel.ConnectionFailed += (s, a) =>
                {
                    argsReceived = a;
                };

                var terminal = (ITextTerminal)viewModel;

                terminal.OnError(
                    TerminalErrorType.ConnectionFailed,
                    new ArgumentException());

                Assert.IsInstanceOf<ArgumentException>(argsReceived.Error);
                eventService.Verify(s => s.FireAsync(
                    It.IsAny<SessionAbortedEvent>()), Times.Once());
            }
        }

        //---------------------------------------------------------------------
        // ConnectAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnectionSucceeds_ThenEventFires(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var eventService = new Mock<IEventService>();

            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    eventService.Object,
                    await instanceLocatorTask,
                    await credential,
                    keyType,
                    null)
                .ConfigureAwait(true))
            {
                viewModel.View = window;

                await viewModel
                    .ConnectAsync(new TerminalSize(80, 24))
                    .ConfigureAwait(false);

                eventService.Verify(s => s.FireAsync(
                    It.IsAny<SessionStartedEvent>()), Times.Once());

                Assert.AreEqual(
                    TerminalPaneViewModelBase.Status.Connected, 
                    viewModel.ConnectionStatus);
            }

            await SshWorkerThread
                .JoinAllWorkerThreadsAsync()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenConnectionFails_ThenEventFires(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var authorizationSource = CreateAuthorizationSource();
            var eventService = new Mock<IEventService>();

            var nonAuthorizedKey = AuthorizedKeyPair.ForMetadata(
                SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072),
                "invalid",
                true,
                authorizationSource.Object.Authorization);

            using (var window = new Form())
            {
                var instance = await instanceLocatorTask;
                var address = await PublicAddressFromLocator(instance)
                    .ConfigureAwait(true);

                var viewModel = new SshTerminalPaneViewModel(
                    eventService.Object,
                    new SynchronousJobService(),
                    new Mock<IConfirmationDialog>().Object,
                    new Mock<IOperationProgressDialog>().Object,
                    instance,
                    new IPEndPoint(address, 22),
                    nonAuthorizedKey,
                    null,
                    TimeSpan.FromSeconds(10))
                {
                    View = window
                };

                ConnectionErrorEventArgs argsReceived = null;
                viewModel.ConnectionFailed += (s, a) =>
                {
                    argsReceived = a;
                };

                await viewModel
                    .ConnectAsync(new TerminalSize(80, 24))
                    .ConfigureAwait(true);

                Assert.IsInstanceOf<SshNativeException>(argsReceived.Error);
                eventService.Verify(s => s.FireAsync(
                    It.IsAny<SessionAbortedEvent>()), Times.Once());

                Assert.AreEqual(
                    TerminalPaneViewModelBase.Status.ConnectionFailed,
                    viewModel.ConnectionStatus);
            }

            await SshWorkerThread
                .JoinAllWorkerThreadsAsync()
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // GetDroppableFiles.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDropContainsFilesAndDirectories_ThenOnlyFilesAreAccepted()
        {
            var existingFile = Assembly.GetExecutingAssembly().Location;
            var dropData = new string[]
            {
                Path.GetTempPath(),
                "does-not-exist.txt",
                existingFile
            };

            var droppableFiles = SshTerminalPaneViewModel.GetDroppableFiles(dropData);
            Assert.AreEqual(1, droppableFiles.Count());
            Assert.AreEqual(existingFile, droppableFiles.First().FullName);
        }

        //---------------------------------------------------------------------
        // UploadFiles.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileExistsAndOverwriteDenied_ThenUploadIsCancelled(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var confirmationDialog = new Mock<IConfirmationDialog>();
            var eventService = new Mock<IEventService>();

            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    eventService.Object,
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.Rsa3072,
                    null,
                    confirmationDialog.Object)
                .ConfigureAwait(true))
            {
                viewModel.View = window;

                await viewModel
                    .ConnectAsync(new TerminalSize(80, 24))
                    .ConfigureAwait(false);

                var tempFilePath = Path.Combine(
                    Path.GetTempPath(),
                    $"{Guid.NewGuid()}.txt");
                File.WriteAllText(tempFilePath, "some data");

                //
                // First upload -> file does not exist yet.
                //
                confirmationDialog
                    .Setup(d => d.Confirm(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .Returns(DialogResult.Yes);

                var uploaded = await viewModel
                    .UploadFilesAsync(new[] { new FileInfo(tempFilePath) })
                    .ConfigureAwait(true);

                Assert.IsTrue(uploaded);

                //
                // Second upload -> file exists.
                //
                confirmationDialog
                    .Setup(d => d.Confirm(
                        It.IsAny<IWin32Window>(),
                        It.Is<string>(m => m.Contains("exist")),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .Returns(DialogResult.No); // Don't overwrite.

                uploaded = await viewModel
                    .UploadFilesAsync(new[] { new FileInfo(tempFilePath) })
                    .ConfigureAwait(true);
                Assert.IsFalse(uploaded);
            }

            await SshWorkerThread
                .JoinAllWorkerThreadsAsync()
                .ConfigureAwait(false);
        }
    }
}
