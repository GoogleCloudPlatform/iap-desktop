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
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Download;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Views;
using Google.Solutions.Testing.Common.Integration;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.SshTerminal
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSshTerminalPane : WindowTestFixtureBase
    {
        private readonly IPEndPoint NonSshEndpoint =
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 443);
        private readonly IPEndPoint UnboundEndpoint =
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23);

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

        [SetUp]
        public void SetUpServices()
        {
            this.ServiceRegistry.AddMock<IConfirmationDialog>();
            this.ServiceRegistry.AddMock<IOperationProgressDialog>();
            this.ServiceRegistry.AddMock<IDownloadFileDialog>();
            this.ServiceRegistry.AddMock<IQuarantineAdapter>();
            this.ServiceRegistry.AddMock<IThemeService>();
        }

        private async Task<SshTerminalPane> ConnectSshTerminalPane(
            InstanceLocator instanceLocator,
            ICredential credential,
            SshKeyType keyType,
            CultureInfo language = null)
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("test@example.com");
            var authorizationSource = new Mock<IAuthorizationSource>();
            authorizationSource
                .Setup(a => a.Authorization)
                .Returns(authorization.Object);

            var keyAdapter = new KeyAuthorizationService(
                authorizationSource.Object,
                new ComputeEngineAdapter(credential),
                new ResourceManagerAdapter(credential),
                new Mock<IOsLoginService>().Object);
            
            var authorizedKey = await keyAdapter.AuthorizeKeyAsync(
                    instanceLocator,
                    SshKeyPair.NewEphemeralKeyPair(keyType),
                    TimeSpan.FromMinutes(10),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None)
                .ConfigureAwait(true);

            var broker = new SshTerminalSessionBroker(
                this.ServiceProvider);

            var address = await PublicAddressFromLocator(instanceLocator)
                .ConfigureAwait(true);

            SshTerminalPane pane = null;
            await AssertRaisesEventAsync<SessionStartedEvent>(
                async () =>
                {
                    pane = (SshTerminalPane)await broker.ConnectAsync(
                            instanceLocator,
                            new IPEndPoint(address, 22),
                            authorizedKey,
                            language,
                            TimeSpan.FromSeconds(10))
                        .ConfigureAwait(true);
                })
                .ConfigureAwait(true);

            PumpWindowMessages();

            return (SshTerminalPane)pane;
        }

        [SetUp]
        public void SetUpTerminalSettingsRepository()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            this.ServiceRegistry.AddSingleton(new TerminalSettingsRepository(
                hkcu.CreateSubKey(TestKeyPath)));
            this.ServiceRegistry.AddSingleton(new SshSettingsRepository(
                hkcu.CreateSubKey(TestKeyPath),
                null,
                null,
                Profile.SchemaVersion.Current));
        }

        public static async Task CompleteBackgroundWorkAsync()
        {
            //
            // Join worker thread to prevent NUnit from aborting it, 
            // causing un unorderly cleanup.
            //
            await SshWorkerThread
                .JoinAllWorkerThreadsAsync()
                .ConfigureAwait(false);

            //
            // While a worker thread is running down, it might
            // still post work to the current synchroniation context.
            //
            // Drain the synchronization context's backlog to prevent
            // the dreaded 'Work posted to the synchronization context did
            // not complete within ten seconds' error.
            //
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(5).ConfigureAwait(true);
                await Task.Yield();
            }
        }

        //---------------------------------------------------------------------
        // Connect
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPortNotListening_ThenErrorIsShownAndWindowIsClosed(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType)
        {
            var key = SshKeyPair.NewEphemeralKeyPair(keyType);

            var broker = new SshTerminalSessionBroker(
                this.ServiceProvider);

            await AssertRaisesEventAsync<SessionAbortedEvent>(
                () => broker.ConnectAsync(
                    new InstanceLocator("project-1", "zone-1", "instance-1"),
                    this.UnboundEndpoint,
                    AuthorizedKeyPair.ForMetadata(key, "test", true, null),
                    null,
                    TimeSpan.FromSeconds(10)))
                .ConfigureAwait(true);

            Assert.IsInstanceOf(typeof(SocketException), this.ExceptionShown);
            Assert.AreEqual(
                SocketError.ConnectionRefused,
                ((SocketException)this.ExceptionShown).SocketErrorCode);

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenWrongPort_ThenErrorIsShownAndWindowIsClosed(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType)
        {
            var key = SshKeyPair.NewEphemeralKeyPair(keyType);
            var broker = new SshTerminalSessionBroker(
                this.ServiceProvider);

            await AssertRaisesEventAsync<SessionAbortedEvent>(
                () => broker.ConnectAsync(
                    new InstanceLocator("project-1", "zone-1", "instance-1"),
                    this.NonSshEndpoint,
                    AuthorizedKeyPair.ForMetadata(key, "test", true, null),
                    null,
                    TimeSpan.FromSeconds(10)))
                .ConfigureAwait(true);

            Assert.IsInstanceOf(typeof(SocketException), this.ExceptionShown);
            Assert.AreEqual(
                SocketError.ConnectionRefused,
                ((SocketException)this.ExceptionShown).SocketErrorCode);

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenKeyUnknown_ThenErrorIsShownAndWindowIsClosed(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instanceLocator = await instanceLocatorTask;
            var key = SshKeyPair.NewEphemeralKeyPair(keyType);

            var broker = new SshTerminalSessionBroker(
                this.ServiceProvider);

            var address = await PublicAddressFromLocator(instanceLocator)
                .ConfigureAwait(true);

            await AssertRaisesEventAsync<SessionAbortedEvent>(
                () => broker.ConnectAsync(
                    instanceLocator,
                    new IPEndPoint(address, 22),
                    AuthorizedKeyPair.ForMetadata(key, "test", true, null),
                    null,
                    TimeSpan.FromSeconds(10)))
                .ConfigureAwait(true);

            Assert.IsInstanceOf(typeof(MetadataKeyAuthenticationFailedException), this.ExceptionShown);

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenAuthenticationSucceeds_ThenConnectionSuceededEventEventIsFired(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            await AssertRaisesEventAsync<SessionStartedEvent>(
                async () =>
                {
                    using (var pane = await ConnectSshTerminalPane(
                            await instanceLocatorTask,
                            await credential,
                            keyType)
                        .ConfigureAwait(true))
                    {
                        Assert.IsTrue(pane.IsConnected);

                        // Close the pane (not the window).
                        pane.Close();
                    }
                })
                .ConfigureAwait(true);

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Disonnect
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSendingExit_ThenDisconnectedEventIsFiredAndWindowIsClosed(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    keyType)
                .ConfigureAwait(true))
            {
                await Task.Delay(50).ConfigureAwait(true);

                // Send command and wait for event
                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => pane.SendAsync("exit\n"))
                    .ConfigureAwait(true);
            }

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenSendingCtrlD_ThenDisconnectedEventIsFiredAndWindowIsClosed(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    keyType)
                .ConfigureAwait(true))
            {
                await Task.Delay(50).ConfigureAwait(true);

                // Send keystroke and wait for event
                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => pane.Terminal.SimulateKey(Keys.D | Keys.Control))
                    .ConfigureAwait(true);
            }

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenClosingPane_ThenDisposeDoesNotHang(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    keyType)
                .ConfigureAwait(true))
            {
                pane.Terminal.SimulateKey(Keys.A);
                pane.Terminal.SimulateKey(Keys.B);
                pane.Terminal.SimulateKey(Keys.C);

                await AssertRaisesEventAsync<SessionEndedEvent>(
                    () => pane.Close())
                    .ConfigureAwait(true);
            }

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Clipboard
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPastingMultiLineTextFromClipboard_ThenLineEndingsAreConverted(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.EcdsaNistp384)
                .ConfigureAwait(true))
            {
                await AssertRaisesEventAsync<SessionEndedEvent>(() =>
                    {
                        // Copy command with a line continuation.
                        ClipboardUtil.SetText("whoami \\\r\n--help;exit\n\n");
                        pane.Terminal.PasteClipboard();
                        pane.Terminal.SimulateKey(Keys.Enter);

                        return Task.CompletedTask;
                    })
                    .ConfigureAwait(true);

                var buffer = pane.Terminal.GetBuffer();

                StringAssert.Contains(
                    "Usage: whoami",
                    buffer);
            }

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Terminal input processing
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenPseudoterminalHasRightSize(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.EcdsaNistp384)
                .ConfigureAwait(true))
            {
                // Measure initial window.
                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => pane.SendAsync("echo 1: $COLUMNS x $LINES;exit\n"))
                    .ConfigureAwait(true);

                var expectedInitialSize = $"1: {pane.Terminal.Columns} x {pane.Terminal.Rows}";
                var buffer = pane.Terminal.GetBuffer();

                StringAssert.Contains(
                    expectedInitialSize,
                    buffer);
            }

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenConnected_ThenPseudoterminalHasRightEncoding(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.EcdsaNistp384,
                    new CultureInfo("en-AU"))
                .ConfigureAwait(true))
            {
                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => pane.SendAsync("locale;sleep 1;exit\n"))
                    .ConfigureAwait(true);

                var buffer = pane.Terminal.GetBuffer();

                StringAssert.Contains(
                    "LC_ALL=en_AU.UTF-8",
                    buffer);
                StringAssert.Contains(
                    "LC_CTYPE=\"en_AU.UTF-8\"",
                    buffer);
            }

            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenSendingBackspace_ThenLastCharacterIsRemoved(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.EcdsaNistp384)
                .ConfigureAwait(true))
            {
                pane.Terminal.SimulateKey(Keys.A);
                pane.Terminal.SimulateKey(Keys.B);
                pane.Terminal.SimulateKey(Keys.C);
                pane.Terminal.SimulateKey(Keys.Back);

                await Task.Delay(50).ConfigureAwait(true); // Do not let the Ctrl+C abort the echo.
                pane.Terminal.SimulateKey(Keys.C | Keys.Control);

                await Task.Delay(50).ConfigureAwait(true);

                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => pane.Terminal.SimulateKey(Keys.D | Keys.Control))
                    .ConfigureAwait(true);

                StringAssert.Contains("ab^C", pane.Terminal.GetBuffer().Trim());
            }

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenUsingHomeAndEnd_ThenCursorJumpsToPosition(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.EcdsaNistp384)
                .ConfigureAwait(true))
            {
                pane.Terminal.SimulateKey(Keys.A);
                pane.Terminal.SimulateKey(Keys.B);
                pane.Terminal.SimulateKey(Keys.C);

                pane.Terminal.SimulateKey(Keys.Home);
                pane.Terminal.SimulateKey(Keys.Delete);
                pane.Terminal.SimulateKey(Keys.E);
                pane.Terminal.SimulateKey(Keys.C);
                pane.Terminal.SimulateKey(Keys.H);
                pane.Terminal.SimulateKey(Keys.O);
                pane.Terminal.SimulateKey(Keys.Space);
                pane.Terminal.SimulateKey(Keys.X);
                pane.Terminal.SimulateKey(Keys.End);
                pane.Terminal.SimulateKey(Keys.Z);
                pane.Terminal.SimulateKey(Keys.Enter);

                await Task.Delay(50).ConfigureAwait(true);
                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => pane.Terminal.SimulateKey(Keys.D | Keys.Control))
                    .ConfigureAwait(true);

                StringAssert.Contains("echo xbcz", pane.Terminal.GetBuffer().Trim());
            }

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenUsingCtrlAAndCtrlE_ThenCursorJumpsToPosition(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.EcdsaNistp384)
                .ConfigureAwait(true))
            {
                pane.Terminal.SimulateKey(Keys.A);
                pane.Terminal.SimulateKey(Keys.B);
                pane.Terminal.SimulateKey(Keys.C);

                pane.Terminal.SimulateKey(Keys.A | Keys.Control);
                pane.Terminal.SimulateKey(Keys.Delete);
                pane.Terminal.SimulateKey(Keys.E);
                pane.Terminal.SimulateKey(Keys.C);
                pane.Terminal.SimulateKey(Keys.H);
                pane.Terminal.SimulateKey(Keys.O);
                pane.Terminal.SimulateKey(Keys.Space);
                pane.Terminal.SimulateKey(Keys.X);
                pane.Terminal.SimulateKey(Keys.E | Keys.Control);
                pane.Terminal.SimulateKey(Keys.Z);
                pane.Terminal.SimulateKey(Keys.Enter);

                await Task.Delay(50).ConfigureAwait(true);
                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => pane.Terminal.SimulateKey(Keys.D | Keys.Control))
                    .ConfigureAwait(true);

                StringAssert.Contains("echo xbcz", pane.Terminal.GetBuffer().Trim());
            }

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenUsingAlt_ThenInputIsNotInterpretedAsKeySequence(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.EcdsaNistp384)
                .ConfigureAwait(true))
            {
                pane.Terminal.SimulateKey(Keys.A);
                pane.Terminal.SimulateKey(Keys.C | Keys.Control | Keys.Alt);
                pane.Terminal.SimulateKey(Keys.B);
                pane.Terminal.SimulateKey(Keys.Enter);

                await Task.Delay(50).ConfigureAwait(true);
                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => pane.Terminal.SimulateKey(Keys.D | Keys.Control))
                    .ConfigureAwait(true);

                StringAssert.Contains("ab", pane.Terminal.GetBuffer().Trim());
            }

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenTerminalSettingsChange_ThenSettingsAreReapplied(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            // Disable Ctrl+C/V.
            var settingsRepository = this.ServiceProvider.GetService<TerminalSettingsRepository>();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    SshKeyType.EcdsaNistp384)
                .ConfigureAwait(true))
            {
                Assert.IsFalse(pane.Terminal.EnableCtrlC);
                Assert.IsFalse(pane.Terminal.EnableCtrlV);

                // Re-enable Ctrl+C/V.
                settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue = true;
                settingsRepository.SetSettings(settings);

                Assert.IsTrue(pane.Terminal.EnableCtrlC);
                Assert.IsTrue(pane.Terminal.EnableCtrlV);
            }

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }
    }
}
