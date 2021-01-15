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
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Test.Views;
using Google.Solutions.IapDesktop.Extensions.Ssh.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Ssh.Views.Terminal;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CS4014 // call is not awaited

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Test.Views.Terminal
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
                    .ExecuteAsync();
                return instance.PublicAddress();
            }
        }

        //---------------------------------------------------------------------
        // Connect
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPortNotListening_ThenErrorIsShownAndWindowIsClosed()
        {
            using (var key = new RsaSshKey(new RSACng()))
            {
                ConnectionFailedEvent deliveredEvent = null;
                this.eventService.BindHandler<ConnectionFailedEvent>(e => deliveredEvent = e);

                var broker = new SshTerminalConnectionBroker(
                    this.serviceProvider);
                await broker.ConnectAsync(
                        new InstanceLocator("project-1", "zone-1", "instance-1"),
                        "test",
                        UnboundEndpoint,
                        key)
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
                ConnectionFailedEvent deliveredEvent = null;
                this.eventService.BindHandler<ConnectionFailedEvent>(e => deliveredEvent = e);

                var broker = new SshTerminalConnectionBroker(
                    this.serviceProvider);
                await broker.ConnectAsync(
                        new InstanceLocator("project-1", "zone-1", "instance-1"),
                        "test",
                        NonSshEndpoint,
                        key)
                    .ConfigureAwait(true);

                Assert.IsNotNull(deliveredEvent, "Event fired");
                Assert.IsInstanceOf(typeof(SocketException), this.ExceptionShown);
                Assert.AreEqual(
                    SocketError.ConnectionRefused, 
                    ((SocketException)this.ExceptionShown).SocketErrorCode);
            }
        }

        [Test]
        public async Task WhenUsernameIsNotPosixCompliant_ThenErrorIsShownAndWindowIsClosed(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instanceLocator = await instanceLocatorTask;

            using (var key = new RsaSshKey(new RSACng()))
            {
                ConnectionFailedEvent deliveredEvent = null;
                this.eventService.BindHandler<ConnectionFailedEvent>(e => deliveredEvent = e);

                var broker = new SshTerminalConnectionBroker(
                    this.serviceProvider);
                await broker.ConnectAsync(
                        instanceLocator,
                        "not POSIX compli@nt",
                        new IPEndPoint(await PublicAddressFromLocator(instanceLocator), 22),
                        key)
                    .ConfigureAwait(true);

                Assert.IsNotNull(deliveredEvent, "Event fired");
                Assert.IsInstanceOf(typeof(SshNativeException), this.ExceptionShown);
                Assert.AreEqual(
                    LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                    ((SshNativeException)this.ExceptionShown).ErrorCode);
            }
        }

        [Test]
        public async Task WhenKeyUnknown_ThenErrorIsShownAndWindowIsClosed(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var instanceLocator = await instanceLocatorTask;

            using (var key = new RsaSshKey(new RSACng()))
            {
                ConnectionFailedEvent deliveredEvent = null;
                this.eventService.BindHandler<ConnectionFailedEvent>(e => deliveredEvent = e);

                var broker = new SshTerminalConnectionBroker(
                    this.serviceProvider);
                await broker.ConnectAsync(
                        instanceLocator,
                        "test",
                        new IPEndPoint(await PublicAddressFromLocator(instanceLocator), 22),
                        key)
                    .ConfigureAwait(true);

                Assert.IsNotNull(deliveredEvent, "Event fired");
                Assert.IsInstanceOf(typeof(SshNativeException), this.ExceptionShown);
                Assert.AreEqual(
                    LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                    ((SshNativeException)this.ExceptionShown).ErrorCode);
            }
        }

        [Test]
        public async Task WhenAuthenticationSucceeds_ThenConnectedEventIsFired(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var instanceLocator = await instanceLocatorTask;

            using (var key = new RsaSshKey(new RSACng()))
            using (var keyAdapter = new ComputeEngineKeysAdapter(
                new ComputeEngineAdapter(await credential)))
            {
                await keyAdapter.PushPublicKeyAsync(
                        instanceLocator,
                        "test",
                        key,
                        CancellationToken.None)
                    .ConfigureAwait(true);

                // Connect and wait for event
                ConnectionSuceededEvent connectedEvent = null;
                this.eventService.BindHandler<ConnectionSuceededEvent>(e => connectedEvent = e);

                var broker = new SshTerminalConnectionBroker(
                    this.serviceProvider);
                using (var pane = await broker.ConnectAsync(
                        instanceLocator,
                        "test",
                        new IPEndPoint(await PublicAddressFromLocator(instanceLocator), 22),
                        key)
                    .ConfigureAwait(true))
                {
                    Assert.IsNotNull(connectedEvent, "ConnectionSuceededEvent event fired");

                    // Close and wait for event
                    ConnectionClosedEvent closedEvent = null;
                    this.eventService.BindHandler<ConnectionClosedEvent>(e => closedEvent = e);

                    pane.Close();

                    Assert.IsNotNull(closedEvent, "ConnectionClosedEvent event fired");
                }
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
            var instanceLocator = await instanceLocatorTask;

            using (var key = new RsaSshKey(new RSACng()))
            using (var keyAdapter = new ComputeEngineKeysAdapter(
                new ComputeEngineAdapter(await credential)))
            {
                await keyAdapter.PushPublicKeyAsync(
                    instanceLocator,
                    "test",
                    key,
                    CancellationToken.None);

                // Connect and wait for event
                ConnectionSuceededEvent connectedEvent = null;
                this.eventService.BindHandler<ConnectionSuceededEvent>(e => connectedEvent = e);

                var broker = new SshTerminalConnectionBroker(
                    this.serviceProvider);
                using (var pane = (SshTerminalPane)await broker.ConnectAsync(
                        instanceLocator,
                        "test",
                        new IPEndPoint(await PublicAddressFromLocator(instanceLocator), 22),
                        key)
                    .ConfigureAwait(true))
                {
                    Assert.IsNotNull(connectedEvent, "ConnectionSuceededEvent event fired");

                    // Send command and wait for event
                    await pane.SendAsync("exit\n");

                    AwaitEvent<ConnectionClosedEvent>();
                }
            }
        }

        [Test]
        public void WhenSocketForceClosed_ThenErrorIsShownAndWindowIsClosed()
        {
            Assert.Inconclusive();
        }

        //---------------------------------------------------------------------
        // Terminal input
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenPseudoterminalHasRightSize(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var instanceLocator = await instanceLocatorTask;

            using (var key = new RsaSshKey(new RSACng()))
            using (var keyAdapter = new ComputeEngineKeysAdapter(
                new ComputeEngineAdapter(await credential)))
            {
                await keyAdapter.PushPublicKeyAsync(
                    instanceLocator,
                    "test",
                    key,
                    CancellationToken.None);

                // Connect and wait for event
                ConnectionSuceededEvent connectedEvent = null;
                this.eventService.BindHandler<ConnectionSuceededEvent>(e => connectedEvent = e);

                var broker = new SshTerminalConnectionBroker(
                    this.serviceProvider);
                using (var pane = (SshTerminalPane)await broker.ConnectAsync(
                        instanceLocator,
                        "test",
                        new IPEndPoint(await PublicAddressFromLocator(instanceLocator), 22),
                        key)
                    .ConfigureAwait(true))
                {
                    Assert.IsNotNull(connectedEvent, "ConnectionSuceededEvent event fired");

                    PumpWindowMessages();

                    // Measure initial window.
                    await pane.SendAsync("echo 1: $COLUMNS x $LINES\n");

                    var expectedInitialSize = $"1: {pane.Terminal.Columns} x {pane.Terminal.Rows}";

                    // Resize window and measure again.
                    var window = ((Form)mainForm);
                    window.Size = new Size(window.Size.Width + 100, window.Size.Height + 100);
                    PumpWindowMessages();

                    await pane.SendAsync("echo 2: $COLUMNS x $LINES;exit\n");

                    var expectedFinalSize = $"2: {pane.Terminal.Columns} x {pane.Terminal.Rows}";

                    AwaitEvent<ConnectionClosedEvent>();
                    var buffer = pane.Terminal.GetBuffer();

                    StringAssert.Contains(
                        expectedInitialSize,
                        buffer);
                    StringAssert.Contains(
                        expectedFinalSize,
                        buffer);
                }
            }
        }
        
        [Test]
        public async Task WhenConnected_ThenPseudoterminalHasRightEncoding(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var instanceLocator = await instanceLocatorTask;

            using (var key = new RsaSshKey(new RSACng()))
            using (var keyAdapter = new ComputeEngineKeysAdapter(
                new ComputeEngineAdapter(await credential)))
            {
                await keyAdapter.PushPublicKeyAsync(
                    instanceLocator,
                    "test",
                    key,
                    CancellationToken.None);

                // Connect and wait for event
                ConnectionSuceededEvent connectedEvent = null;
                this.eventService.BindHandler<ConnectionSuceededEvent>(e => connectedEvent = e);

                CultureInfo.CurrentUICulture = new CultureInfo("en-AU");

                var broker = new SshTerminalConnectionBroker(
                    this.serviceProvider);
                using (var pane = (SshTerminalPane)await broker.ConnectAsync(
                        instanceLocator,
                        "test",
                        new IPEndPoint(await PublicAddressFromLocator(instanceLocator), 22),
                        key)
                    .ConfigureAwait(true))
                {
                    Assert.IsNotNull(connectedEvent, "ConnectionSuceededEvent event fired");

                    PumpWindowMessages();

                    await pane.SendAsync("locale;sleep 1;exit\n");

                    AwaitEvent<ConnectionClosedEvent>();
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
        }

        [Test]
        public async Task WhenSendingCtrlD_ThenDisconnectedEventIsFiredAndWindowIsClosed(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var instanceLocator = await instanceLocatorTask;

            using (var key = new RsaSshKey(new RSACng()))
            using (var keyAdapter = new ComputeEngineKeysAdapter(
                new ComputeEngineAdapter(await credential)))
            {
                await keyAdapter.PushPublicKeyAsync(
                    instanceLocator,
                    "test",
                    key,
                    CancellationToken.None);

                // Connect and wait for event
                ConnectionSuceededEvent connectedEvent = null;
                this.eventService.BindHandler<ConnectionSuceededEvent>(e => connectedEvent = e);

                var broker = new SshTerminalConnectionBroker(
                    this.serviceProvider);
                using (var pane = (SshTerminalPane)await broker.ConnectAsync(
                        instanceLocator,
                        "test",
                        new IPEndPoint(await PublicAddressFromLocator(instanceLocator), 22),
                        key)
                    .ConfigureAwait(true))
                {
                    Assert.IsNotNull(connectedEvent, "ConnectionSuceededEvent event fired");

                    PumpWindowMessages();

                    // Send keystroke and wait for event
                    pane.Terminal.SendKey(Keys.D, true, false);

                    AwaitEvent<ConnectionClosedEvent>();
                }
            }
        }


        [Test]
        public async Task WhenSendingBackspace_ThenLastCharacterIsRemoved(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var instanceLocator = await instanceLocatorTask;

            using (var key = new RsaSshKey(new RSACng()))
            using (var keyAdapter = new ComputeEngineKeysAdapter(
                new ComputeEngineAdapter(await credential)))
            {
                await keyAdapter.PushPublicKeyAsync(
                    instanceLocator,
                    "test",
                    key,
                    CancellationToken.None);

                // Connect and wait for event
                ConnectionSuceededEvent connectedEvent = null;
                this.eventService.BindHandler<ConnectionSuceededEvent>(e => connectedEvent = e);

                var broker = new SshTerminalConnectionBroker(
                    this.serviceProvider);
                using (var pane = (SshTerminalPane)await broker.ConnectAsync(
                        instanceLocator,
                        "test",
                        new IPEndPoint(await PublicAddressFromLocator(instanceLocator), 22),
                        key)
                    .ConfigureAwait(true))
                {
                    Assert.IsNotNull(connectedEvent, "ConnectionSuceededEvent event fired");

                    PumpWindowMessages();

                    pane.Terminal.SendKey(Keys.A, false, false);
                    pane.Terminal.SendKey(Keys.B, false, false);
                    pane.Terminal.SendKey(Keys.C, false, false);
                    pane.Terminal.SendKey(Keys.Back, false, false);

                    pane.Terminal.SendKey(Keys.C, true, false);
                    pane.Terminal.SendKey(Keys.D, true, false);

                    AwaitEvent<ConnectionClosedEvent>();
                    StringAssert.Contains("ab^C", pane.Terminal.GetBuffer().Trim());
                }
            }
        }

        // TODO: more tests
        // - back
        // - enter 
        // - pgup/dn
        // - (see translation table)

    }
}
