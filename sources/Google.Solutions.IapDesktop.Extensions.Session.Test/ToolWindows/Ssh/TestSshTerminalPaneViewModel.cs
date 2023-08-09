﻿//
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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Download;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Platform.Security;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Ssh
{
    [TestFixture]
    [UsesCloudResources]
    [Apartment(ApartmentState.STA)]
    public class TestSshTerminalPaneViewModel
    {
        private Mock<IEventQueue> eventService;
        private Mock<IConfirmationDialog> confirmationDialog;
        private Mock<IExceptionDialog> exceptionDialog;
        private Mock<IDownloadFileDialog> downloadFileDialog;
        private Mock<IQuarantine> quarantine;

        [SetUp]
        public void SetUp()
        {
            this.eventService = new Mock<IEventQueue>();
            this.confirmationDialog = new Mock<IConfirmationDialog>();
            this.exceptionDialog = new Mock<IExceptionDialog>();
            this.downloadFileDialog = new Mock<IDownloadFileDialog>();
            this.quarantine = new Mock<IQuarantine>();
        }

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

        private static Mock<IAuthorization> CreateAuthorizationMock()
        {
            var session = new Mock<IOidcSession>();
            session
                .SetupGet(a => a.Username)
                .Returns("test@example.com");

            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Session)
                .Returns(session.Object);

            return authorization;
        }

        private static async Task<AuthorizedKeyPair> CreateAuthorizedKeyAsync(
            InstanceLocator instance,
            IAuthorization authorization,
            SshKeyType keyType)
        {
            var authorizationSource = CreateAuthorizationMock();

            var keyAdapter = new KeyAuthorizer(
                authorizationSource.Object,
                new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(),
                    authorization,
                    TestProject.UserAgent),
                new ResourceManagerClient(
                    ResourceManagerClient.CreateEndpoint(), 
                    authorization,
                    TestProject.UserAgent),
                new Mock<IOsLoginProfile>().Object);

            return await keyAdapter.AuthorizeKeyAsync(
                    instance,
                    SshKeyPair.NewEphemeralKeyPair(keyType),
                    TimeSpan.FromMinutes(10),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None)
                .ConfigureAwait(true);
        }

        private async Task<SshTerminalViewModel> CreateViewModelAsync(
            InstanceLocator instance,
            IAuthorization authorization,
            SshKeyType keyType,
            CultureInfo language = null)
        {
            var authorizedKey = await 
                CreateAuthorizedKeyAsync(
                    instance,
                    authorization,
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

            return new SshTerminalViewModel(
                this.eventService.Object,
                new SynchronousJobService(),
                this.confirmationDialog.Object,
                progressDialog.Object,
                this.downloadFileDialog.Object,
                this.exceptionDialog.Object,
                this.quarantine.Object)
            {
                Instance = instance,
                Endpoint = new IPEndPoint(address, 22),
                AuthorizedKey = authorizedKey,
                Language = language,
                ConnectionTimeout = TimeSpan.FromSeconds(10)
            };
        }

        //---------------------------------------------------------------------
        // ITextTerminal.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDataReceived_ThenEventFires(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    await instanceLocatorTask,
                    await auth,
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
                this.eventService.Verify(s => s.PublishAsync(
                    It.IsAny<SessionAbortedEvent>()), Times.Once());
            }

            await SshWorkerThread
                .JoinAllWorkerThreadsAsync()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenConectionFailedErrorReceived_ThenEventFires(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    await instanceLocatorTask,
                    await auth,
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
                this.eventService.Verify(s => s.PublishAsync(
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    await instanceLocatorTask,
                    await auth,
                    keyType,
                    null)
                .ConfigureAwait(true))
            {
                viewModel.View = window;

                await viewModel
                    .ConnectAsync(new TerminalSize(80, 24))
                    .ConfigureAwait(false);

                this.eventService.Verify(s => s.PublishAsync(
                    It.IsAny<SessionStartedEvent>()), Times.Once());

                Assert.AreEqual(
                    TerminalViewModelBase.Status.Connected,
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
            var authorizationSource = CreateAuthorizationMock();
            var eventService = new Mock<IEventQueue>();

            var nonAuthorizedKey = AuthorizedKeyPair.ForMetadata(
                SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072),
                "invalid",
                true,
                authorizationSource.Object);

            using (var window = new Form())
            {
                var instance = await instanceLocatorTask;
                var address = await PublicAddressFromLocator(instance)
                    .ConfigureAwait(true);

                var viewModel = new SshTerminalViewModel(
                    eventService.Object,
                    new SynchronousJobService(),
                    new Mock<IConfirmationDialog>().Object,
                    new Mock<IOperationProgressDialog>().Object,
                    new Mock<IDownloadFileDialog>().Object,
                    new Mock<IExceptionDialog>().Object,
                    new Mock<IQuarantine>().Object)
                {
                    View = window,
                    Instance = instance,
                    Endpoint = new IPEndPoint(address, 22),
                    AuthorizedKey = nonAuthorizedKey,
                    Language = null,
                    ConnectionTimeout = TimeSpan.FromSeconds(10)
                };

                ConnectionErrorEventArgs argsReceived = null;
                viewModel.ConnectionFailed += (s, a) =>
                {
                    argsReceived = a;
                };

                await viewModel
                    .ConnectAsync(new TerminalSize(80, 24))
                    .ConfigureAwait(true);

                Assert.IsInstanceOf<MetadataKeyAuthenticationFailedException>(argsReceived.Error);
                eventService.Verify(s => s.PublishAsync(
                    It.IsAny<SessionAbortedEvent>()), Times.Once());

                Assert.AreEqual(
                    TerminalViewModelBase.Status.ConnectionFailed,
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

            var droppableFiles = SshTerminalViewModel.GetDroppableFiles(dropData);
            Assert.AreEqual(1, droppableFiles.Count());
            Assert.AreEqual(existingFile, droppableFiles.First().FullName);
        }

        //---------------------------------------------------------------------
        // DownloadFiles.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNoFileSelected_ThenDownloadIsCancelled(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    await instanceLocatorTask,
                    await auth,
                    SshKeyType.Rsa3072,
                    null)
                .ConfigureAwait(true))
            {
                viewModel.View = window;
                viewModel.ConnectionFailed += (s, e) => Assert.Fail("Connection failed: " + e.Error);
                viewModel.ConnectionLost += (s, e) => Assert.Fail("Connection lost: " + e.Error);

                await viewModel
                    .ConnectAsync(new TerminalSize(80, 24))
                    .ConfigureAwait(false);

                var selection = It.IsAny<IEnumerable<FileBrowser.IFileItem>>();
                var targetDir = It.IsAny<DirectoryInfo>();
                this.downloadFileDialog
                    .Setup(d => d.SelectDownloadFiles(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<string>(),
                        It.IsAny<FileBrowser.IFileSystem>(),
                        out selection,
                        out targetDir))
                    .Returns(DialogResult.Cancel);

                Assert.IsFalse(await viewModel.DownloadFilesAsync());
            }

            await SshWorkerThread
                .JoinAllWorkerThreadsAsync()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenFileExists_ThenDownloadShowsConfirmationPrompt(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    await instanceLocatorTask,
                    await auth,
                    SshKeyType.Rsa3072,
                    null)
                .ConfigureAwait(true))
            {
                viewModel.View = window;
                viewModel.ConnectionFailed += (s, e) => Assert.Fail("Connection failed: " + e.Error);
                viewModel.ConnectionLost += (s, e) => Assert.Fail("Connection lost: " + e.Error);

                await viewModel
                    .ConnectAsync(new TerminalSize(80, 24))
                    .ConfigureAwait(false);

                var targetDirectory = Path.GetTempPath();
                var existingFileName = $"{Guid.NewGuid()}.txt";
                File.WriteAllText(Path.Combine(targetDirectory, existingFileName), string.Empty);

                //
                // Download existing file, but deny overwrite.
                //
                this.confirmationDialog
                    .Setup(c => c.Confirm(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .Returns(DialogResult.Cancel);

                var existingFile = new Mock<FileBrowser.IFileItem>();
                existingFile.SetupGet(f => f.Name).Returns(existingFileName);

                var selection = (IEnumerable<FileBrowser.IFileItem>)new[] { existingFile.Object };
                var targetDir = new DirectoryInfo(targetDirectory);
                this.downloadFileDialog
                    .Setup(d => d.SelectDownloadFiles(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<string>(),
                        It.IsAny<FileBrowser.IFileSystem>(),
                        out selection,
                        out targetDir))
                    .Returns(DialogResult.OK);

                Assert.IsFalse(await viewModel.DownloadFilesAsync());
                this.confirmationDialog
                    .Verify(c => c.Confirm(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()), Times.Once);
            }

            await SshWorkerThread
                .JoinAllWorkerThreadsAsync()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenQuarantineScanFails_ThenDownloadShowsErrorDialog(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    await instanceLocatorTask,
                    await auth,
                    SshKeyType.Rsa3072,
                    null)
                .ConfigureAwait(true))
            {
                viewModel.View = window;
                viewModel.ConnectionFailed += (s, e) => Assert.Fail("Connection failed: " + e.Error);
                viewModel.ConnectionLost += (s, e) => Assert.Fail("Connection lost: " + e.Error);

                await viewModel
                    .ConnectAsync(new TerminalSize(80, 24))
                    .ConfigureAwait(false);

                var targetDirectory = Directory.CreateDirectory(
                    $"{Path.GetTempPath()}\\{Guid.NewGuid()}.txt");

                var bash = new Mock<FileBrowser.IFileItem>();
                bash.SetupGet(f => f.Name).Returns("bash");
                bash.SetupGet(f => f.Path).Returns("/bin/bash");

                var selection = (IEnumerable<FileBrowser.IFileItem>)new[] { bash.Object };
                this.downloadFileDialog
                    .Setup(d => d.SelectDownloadFiles(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<string>(),
                        It.IsAny<FileBrowser.IFileSystem>(),
                        out selection,
                        out targetDirectory))
                    .Returns(DialogResult.OK);

                this.quarantine
                    .Setup(a => a.ScanAsync(
                        It.IsAny<IntPtr>(),
                        It.IsAny<FileInfo>()))
                    .ThrowsAsync(new QuarantineException("mock"));

                Assert.IsFalse(await viewModel.DownloadFilesAsync());

                this.quarantine.Verify(a => a.ScanAsync(
                        It.IsAny<IntPtr>(),
                        It.IsAny<FileInfo>()), Times.Once);
                this.exceptionDialog.Verify(d => d.Show(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<QuarantineException>()), Times.Once);
            }

            await SshWorkerThread
                .JoinAllWorkerThreadsAsync()
                .ConfigureAwait(false);
        }


        [Test]
        public async Task WhenNoConflictsFound_ThenDownloadSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    await instanceLocatorTask,
                    await auth,
                    SshKeyType.Rsa3072,
                    null)
                .ConfigureAwait(true))
            {
                viewModel.View = window;
                viewModel.ConnectionFailed += (s, e) => Assert.Fail("Connection failed: " + e.Error);
                viewModel.ConnectionLost += (s, e) => Assert.Fail("Connection lost: " + e.Error);

                await viewModel
                    .ConnectAsync(new TerminalSize(80, 24))
                    .ConfigureAwait(false);

                var targetDirectory = Directory.CreateDirectory(
                    $"{Path.GetTempPath()}\\{Guid.NewGuid()}.txt");

                var bash = new Mock<FileBrowser.IFileItem>();
                bash.SetupGet(f => f.Name).Returns("bash");
                bash.SetupGet(f => f.Path).Returns("/bin/bash");

                var selection = (IEnumerable<FileBrowser.IFileItem>)new[] { bash.Object };
                this.downloadFileDialog
                    .Setup(d => d.SelectDownloadFiles(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<string>(),
                        It.IsAny<FileBrowser.IFileSystem>(),
                        out selection,
                        out targetDirectory))
                    .Returns(DialogResult.OK);

                Assert.IsTrue(await viewModel.DownloadFilesAsync());
                Assert.IsTrue(File.Exists(Path.Combine(targetDirectory.FullName, "bash")));
            }

            await SshWorkerThread
                .JoinAllWorkerThreadsAsync()
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // UploadFiles.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileExistsAndOverwriteDenied_ThenUploadIsCancelled(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var window = new Form())
            using (var viewModel = await CreateViewModelAsync(
                    await instanceLocatorTask,
                    await auth,
                    SshKeyType.Rsa3072,
                    null)
                .ConfigureAwait(true))
            {
                viewModel.View = window;
                viewModel.ConnectionFailed += (s, e) => Assert.Fail("Connection failed: " + e.Error);
                viewModel.ConnectionLost += (s, e) => Assert.Fail("Connection lost: " + e.Error);

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
                this.confirmationDialog
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
                this.confirmationDialog
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
