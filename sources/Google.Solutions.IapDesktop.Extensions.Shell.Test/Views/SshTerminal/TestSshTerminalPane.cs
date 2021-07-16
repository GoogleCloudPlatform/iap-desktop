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
using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Test.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.SshTerminal
{
    [TestFixture]
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

        private async Task<SshTerminalPane> ConnectSshTerminalPane(
            InstanceLocator instanceLocator,
            ICredential credential,
            CultureInfo language = null)
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("test@example.com");
            var authorizationAdapter = new Mock<IAuthorizationAdapter>();
            authorizationAdapter
                .Setup(a => a.Authorization)
                .Returns(authorization.Object);

            using (var keyAdapter = new AuthorizedKeyService(
                authorizationAdapter.Object,
                new ComputeEngineAdapter(credential),
                new ResourceManagerAdapter(credential),
                new Mock<IOsLoginService>().Object))
            {
                var authorizedKey = await keyAdapter.AuthorizeKeyAsync(
                        instanceLocator,
                        new RsaSshKey(new RSACng()),
                        TimeSpan.FromMinutes(10),
                        null,
                        AuthorizeKeyMethods.InstanceMetadata,
                        CancellationToken.None)
                    .ConfigureAwait(true);

                // Connect and wait for event
                SessionStartedEvent connectedEvent = null;
                this.eventService.BindHandler<SessionStartedEvent>(e => connectedEvent = e);

                var broker = new SshTerminalSessionBroker(
                    this.serviceProvider);
                var pane = await broker.ConnectAsync(
                        instanceLocator,
                        new IPEndPoint(await PublicAddressFromLocator(instanceLocator).ConfigureAwait(true), 22),
                        authorizedKey,
                        language,
                        TimeSpan.FromSeconds(10))
                    .ConfigureAwait(true);

                Assert.IsNotNull(connectedEvent, "ConnectionSuceededEvent event fired");
                PumpWindowMessages();

                return (SshTerminalPane)pane;
            }
        }

        [SetUp]
        public void SetUpTerminalSettingsRepository()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            this.serviceRegistry.AddSingleton(new TerminalSettingsRepository(
                hkcu.CreateSubKey(TestKeyPath)));
            this.serviceRegistry.AddSingleton(new SshSettingsRepository(
                hkcu.CreateSubKey(TestKeyPath)));
        }

        //---------------------------------------------------------------------
        // Connect
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPortNotListening_ThenErrorIsShownAndWindowIsClosed()
        {
            using (var key = new RsaSshKey(new RSACng()))
            {
                SessionAbortedEvent deliveredEvent = null;
                this.eventService.BindHandler<SessionAbortedEvent>(e => deliveredEvent = e);

                var broker = new SshTerminalSessionBroker(
                    this.serviceProvider);
                await broker.ConnectAsync(
                        new InstanceLocator("project-1", "zone-1", "instance-1"),
                        UnboundEndpoint,
                        AuthorizedKey.ForMetadata(key, "test", true, null),
                        null,
                        TimeSpan.FromSeconds(10))
                    .ConfigureAwait(true);

                Assert.IsNotNull(deliveredEvent, "Event fired");
                Assert.IsInstanceOf(typeof(SocketException), this.ExceptionShown);
                Assert.AreEqual(
                    SocketError.ConnectionRefused,
                    ((SocketException)this.ExceptionShown).SocketErrorCode);
            }
        }

        [Test]
        public async Task WhenWrongPort_ThenErrorIsShownAndWindowIsClosed()
        {
            using (var key = new RsaSshKey(new RSACng()))
            {
                SessionAbortedEvent deliveredEvent = null;
                this.eventService.BindHandler<SessionAbortedEvent>(e => deliveredEvent = e);

                var broker = new SshTerminalSessionBroker(
                    this.serviceProvider);
                await broker.ConnectAsync(
                        new InstanceLocator("project-1", "zone-1", "instance-1"),
                        NonSshEndpoint,
                        AuthorizedKey.ForMetadata(key, "test", true, null),
                        null,
                        TimeSpan.FromSeconds(10))
                    .ConfigureAwait(true);

                Assert.IsNotNull(deliveredEvent, "Event fired");
                Assert.IsInstanceOf(typeof(SocketException), this.ExceptionShown);
                Assert.AreEqual(
                    SocketError.ConnectionRefused,
                    ((SocketException)this.ExceptionShown).SocketErrorCode);
            }
        }

        [Test]
        public async Task WhenKeyUnknown_ThenErrorIsShownAndWindowIsClosed(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instanceLocator = await instanceLocatorTask;

            using (var key = new RsaSshKey(new RSACng()))
            {
                SessionAbortedEvent deliveredEvent = null;
                this.eventService.BindHandler<SessionAbortedEvent>(e => deliveredEvent = e);

                var broker = new SshTerminalSessionBroker(
                    this.serviceProvider);
                await broker.ConnectAsync(
                        instanceLocator,
                        new IPEndPoint(await PublicAddressFromLocator(instanceLocator).ConfigureAwait(true), 22),
                        AuthorizedKey.ForMetadata(key, "test", true, null),
                        null,
                        TimeSpan.FromSeconds(10))
                    .ConfigureAwait(true);

                Assert.IsNotNull(deliveredEvent, "Event fired");
                Assert.IsInstanceOf(typeof(SshNativeException), this.ExceptionShown);
                Assert.AreEqual(
                    LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                    ((SshNativeException)this.ExceptionShown).ErrorCode);
            }
        }

        [Test]
        public async Task WhenAuthenticationSucceeds_ThenConnectionSuceededEventEventIsFired(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            SessionStartedEvent connectedEvent = null;
            this.eventService.BindHandler<SessionStartedEvent>(e => connectedEvent = e);

            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential)
                .ConfigureAwait(true))
            {
                // Close the pane (not the window).
                pane.Close();

                Assert.IsNotNull(connectedEvent, "ConnectionSuceededEvent event fired");
            }
        }

        //---------------------------------------------------------------------
        // Disonnect
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSendingExit_ThenDisconnectedEventIsFiredAndWindowIsClosed(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential)
                .ConfigureAwait(true))
            {
                await Task.Delay(50).ConfigureAwait(true);

                // Send command and wait for event
                AssertRaisesEvent<SessionEndedEvent>(
                    () => pane.SendAsync("exit\n").Wait());
            }
        }

        [Test]
        public async Task WhenSendingCtrlD_ThenDisconnectedEventIsFiredAndWindowIsClosed(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential)
                .ConfigureAwait(true))
            {
                await Task.Delay(50).ConfigureAwait(true);

                // Send keystroke and wait for event
                AssertRaisesEvent<SessionEndedEvent>(
                    () => pane.Terminal.SimulateKey(Keys.D | Keys.Control));
            }
        }

        [Test]
        public async Task WhenClosingPane_ThenDisposeDoesNotHang(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential)
                .ConfigureAwait(true))
            {
                pane.Terminal.SimulateKey(Keys.A);
                pane.Terminal.SimulateKey(Keys.B);
                pane.Terminal.SimulateKey(Keys.C);

                pane.Close();
            }
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
                    await credential)
                .ConfigureAwait(true))
            {
                // Copy command with a line continuation.
                Clipboard.SetText("whoami \\\r\n--help;exit\n\n");
                pane.Terminal.PasteClipboard();

                AwaitEvent<SessionEndedEvent>();
                var buffer = pane.Terminal.GetBuffer();

                StringAssert.Contains(
                    "Usage: whoami",
                    buffer);
            }
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
                    await credential)
                .ConfigureAwait(true))
            {
                // Measure initial window.
                await pane.SendAsync("echo 1: $COLUMNS x $LINES;exit\n")
                    .ConfigureAwait(true);

                var expectedInitialSize = $"1: {pane.Terminal.Columns} x {pane.Terminal.Rows}";

                AwaitEvent<SessionEndedEvent>();
                var buffer = pane.Terminal.GetBuffer();

                StringAssert.Contains(
                    expectedInitialSize,
                    buffer);
            }
        }

        [Test]
        public async Task WhenConnected_ThenPseudoterminalHasRightEncoding(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential,
                    new CultureInfo("en-AU"))
                .ConfigureAwait(true))
            {
                await pane.SendAsync("locale;sleep 1;exit\n")
                    .ConfigureAwait(true);

                AwaitEvent<SessionEndedEvent>();
                var buffer = pane.Terminal.GetBuffer();

                StringAssert.Contains(
                    "LC_ALL=en_AU.UTF-8",
                    buffer);
                StringAssert.Contains(
                    "LC_CTYPE=\"en_AU.UTF-8\"",
                    buffer);
            }

            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        [Test]
        public async Task WhenSendingBackspace_ThenLastCharacterIsRemoved(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential)
                .ConfigureAwait(true))
            {
                pane.Terminal.SimulateKey(Keys.A);
                pane.Terminal.SimulateKey(Keys.B);
                pane.Terminal.SimulateKey(Keys.C);
                pane.Terminal.SimulateKey(Keys.Back);

                await Task.Delay(50).ConfigureAwait(true); // Do not let the Ctrl+C abort the echo.
                pane.Terminal.SimulateKey(Keys.C | Keys.Control);

                await Task.Delay(50).ConfigureAwait(true);
                AssertRaisesEvent<SessionEndedEvent>(
                    () => pane.Terminal.SimulateKey(Keys.D | Keys.Control));

                StringAssert.Contains("ab^C", pane.Terminal.GetBuffer().Trim());
            }
        }

        [Test]
        public async Task WhenUsingHomeAndEnd_ThenCursorJumpsToPosition(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential)
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

                await Task.Delay(50);
                AssertRaisesEvent<SessionEndedEvent>(
                    () => pane.Terminal.SimulateKey(Keys.D | Keys.Control));

                StringAssert.Contains("echo xbcz", pane.Terminal.GetBuffer().Trim());
            }
        }

        [Test]
        public async Task WhenUsingCtrlAAndCtrlE_ThenCursorJumpsToPosition(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential)
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

                await Task.Delay(50);
                AssertRaisesEvent<SessionEndedEvent>(
                    () => pane.Terminal.SimulateKey(Keys.D | Keys.Control));

                StringAssert.Contains("echo xbcz", pane.Terminal.GetBuffer().Trim());
            }
        }

        [Test]
        public async Task WhenUsingAlt_ThenInputIsNotInterpretedAsKeySequence(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential)
                .ConfigureAwait(true))
            {
                pane.Terminal.SimulateKey(Keys.A);
                pane.Terminal.SimulateKey(Keys.C | Keys.Control | Keys.Alt);
                pane.Terminal.SimulateKey(Keys.B);
                pane.Terminal.SimulateKey(Keys.Enter);

                await Task.Delay(50).ConfigureAwait(true);
                AssertRaisesEvent<SessionEndedEvent>(
                    () => pane.Terminal.SimulateKey(Keys.D | Keys.Control));

                StringAssert.Contains("ab", pane.Terminal.GetBuffer().Trim());
            }
        }

        [Test]
        public async Task WhenTerminalSettingsChange_ThenSettingsAreReapplied(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            // Disable Ctrl+C/V.
            var settingsRepository = this.serviceProvider.GetService<TerminalSettingsRepository>();
            var settings = settingsRepository.GetSettings();
            settings.IsCopyPasteUsingCtrlCAndCtrlVEnabled.BoolValue = false;
            settingsRepository.SetSettings(settings);

            using (var pane = await ConnectSshTerminalPane(
                    await instanceLocatorTask,
                    await credential)
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
        }
    }
}
