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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Download;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Platform.Security;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Views;
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

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Ssh
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSshTerminalView : WindowTestFixtureBase
    {
        private readonly IPEndPoint NonSshEndpoint =
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 443);
        private readonly IPEndPoint UnboundEndpoint =
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23);


        private static ITransport CreateTransportForEndpoint(IPEndPoint endpoint)
        {
            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Protocol).Returns(SshProtocol.Protocol);
            transport.SetupGet(t => t.Endpoint).Returns(endpoint);
            return transport.Object;
        }

        private static async Task<ITransport> CreateTransportForPublicAddress(
            InstanceLocator instanceLocator,
            ushort port)
        {
            var addressResolver = new AddressResolver(new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(),
                TestProject.AdminAuthorization,
                TestProject.UserAgent));

            return await new DirectTransportFactory(addressResolver)
                .CreateTransportAsync(
                    SshProtocol.Protocol,
                    instanceLocator,
                    NetworkInterfaceType.External,
                    port,
                    CancellationToken.None)
                .ConfigureAwait(true);
        }

        private IServiceProvider CreateServiceProvider()
        {
            var registry = new ServiceRegistry(this.ServiceRegistry);
            registry.AddTransient<SshTerminalView>();
            registry.AddTransient<SshTerminalViewModel>();
            registry.AddMock<IThemeService>();
            registry.AddMock<IConfirmationDialog>();
            registry.AddMock<IOperationProgressDialog>();
            registry.AddMock<IDownloadFileDialog>();
            registry.AddMock<IQuarantine>();
            registry.AddMock<IThemeService>();
            registry.AddMock<IBindingContext>();
            registry.AddTransient<IToolWindowHost, ToolWindowHost>();
            return registry;
        }

        private async Task<SshTerminalView> ConnectSshTerminalPane(
            InstanceLocator instance,
            IAuthorization authorization,
            SshKeyType keyType,
            CultureInfo language = null)
        {
            var serviceProvider = CreateServiceProvider();

            var keyAdapter = new PlatformCredentialFactory(
                authorization,
                new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(), 
                    authorization,
                    TestProject.UserAgent),
                new ResourceManagerClient(
                    ResourceManagerClient.CreateEndpoint(), 
                    authorization,
                    TestProject.UserAgent),
                new Mock<IOsLoginProfile>().Object);

            var sshCredential = await keyAdapter
                .CreateCredentialAsync(
                    instance,
                    AsymmetricKeySigner.CreateEphemeral(keyType),
                    TimeSpan.FromMinutes(10),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None)
                .ConfigureAwait(true);

            var broker = new InstanceSessionBroker(
                serviceProvider.GetService<IMainWindow>(),
                serviceProvider.GetService<ISessionBroker>(),
                serviceProvider.GetService<IToolWindowHost>(),
                serviceProvider.GetService<IJobService>());

            var sshParameters = new SshParameters()
            {
                ConnectionTimeout = TimeSpan.FromSeconds(10),
                Language = language
            };

            var transport = await CreateTransportForPublicAddress(instance, 22)
                .ConfigureAwait(true);

            SshTerminalView pane = null;
            await AssertRaisesEventAsync<SessionStartedEvent>(
                async () => pane = (SshTerminalView)await broker
                    .ConnectSshSessionAsync(instance, transport, sshParameters, sshCredential)
                    .ConfigureAwait(true))
                .ConfigureAwait(true);

            PumpWindowMessages();

            pane.Disposed += (_, __) => transport.Dispose();
            return pane;
        }

        [SetUp]
        public void SetUpTerminalSettingsRepository()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            this.ServiceRegistry.AddSingleton<ITerminalSettingsRepository>(
                new TerminalSettingsRepository(hkcu.CreateSubKey(TestKeyPath)));
            this.ServiceRegistry.AddSingleton<IRepository<ISshSettings>>(
                new SshSettingsRepository(
                    hkcu.CreateSubKey(TestKeyPath),
                    null,
                    null,
                    UserProfile.SchemaVersion.Current));
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
            for (var i = 0; i < 20; i++)
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
            var sshCredential = new PlatformCredential(
                AsymmetricKeySigner.CreateEphemeral(keyType),
                KeyAuthorizationMethods.InstanceMetadata,
                "test");
            var sshParameters = new SshParameters()
            {
                ConnectionTimeout = TimeSpan.FromSeconds(10)
            };

            var transport = CreateTransportForEndpoint(this.UnboundEndpoint);

            var serviceProvider = CreateServiceProvider();
            var broker = new InstanceSessionBroker(
                serviceProvider.GetService<IMainWindow>(),
                serviceProvider.GetService<ISessionBroker>(),
                serviceProvider.GetService<IToolWindowHost>(),
                serviceProvider.GetService<IJobService>());

            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");
            await AssertRaisesEventAsync<SessionAbortedEvent>(
                () => broker.ConnectSshSessionAsync(instance, transport, sshParameters, sshCredential))
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
            var sshCredential = new PlatformCredential(
                AsymmetricKeySigner.CreateEphemeral(keyType),
                KeyAuthorizationMethods.InstanceMetadata,
                "test");
            var sshParameters = new SshParameters()
            {
                ConnectionTimeout = TimeSpan.FromSeconds(10)
            };

            var instance = new InstanceLocator("project-1", "zone-1", "instance-1");
            var transport = CreateTransportForEndpoint(this.NonSshEndpoint);

            var serviceProvider = CreateServiceProvider();
            var broker = new InstanceSessionBroker(
                serviceProvider.GetService<IMainWindow>(),
                serviceProvider.GetService<ISessionBroker>(),
                serviceProvider.GetService<IToolWindowHost>(),
                serviceProvider.GetService<IJobService>());

            await AssertRaisesEventAsync<SessionAbortedEvent>(
                () => broker.ConnectSshSessionAsync(instance, transport, sshParameters, sshCredential))
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
            var sshCredential = new PlatformCredential(
                AsymmetricKeySigner.CreateEphemeral(keyType),
                KeyAuthorizationMethods.InstanceMetadata,
                "test");
            var sshParameters = new SshParameters()
            {
                ConnectionTimeout = TimeSpan.FromSeconds(10)
            };

            var instance = await instanceLocatorTask;
            var transport = await CreateTransportForPublicAddress(instance, 22)
                .ConfigureAwait(true);

            var serviceProvider = CreateServiceProvider();
            var broker = new InstanceSessionBroker(
                serviceProvider.GetService<IMainWindow>(),
                serviceProvider.GetService<ISessionBroker>(),
                serviceProvider.GetService<IToolWindowHost>(),
                serviceProvider.GetService<IJobService>());

            await AssertRaisesEventAsync<SessionAbortedEvent>(
                () => broker.ConnectSshSessionAsync(instance, transport, sshParameters, sshCredential))
                .ConfigureAwait(true);

            Assert.IsInstanceOf(typeof(MetadataKeyAuthenticationFailedException), this.ExceptionShown);

            await CompleteBackgroundWorkAsync().ConfigureAwait(true);
        }

        [Test]
        public async Task WhenAuthenticationSucceeds_ThenConnectionSuceededEventEventIsFired(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType,
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            await AssertRaisesEventAsync<SessionStartedEvent>(
                async () =>
                {
                    using (var pane = await ConnectSshTerminalPane(
                            await instanceLocatorTask,
                            await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            // Disable Ctrl+C/V.

            var serviceProvider = CreateServiceProvider();
            var settingsRepository = serviceProvider.GetService<ITerminalSettingsRepository>();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await auth,
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
