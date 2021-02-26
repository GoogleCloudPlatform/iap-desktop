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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Test.Views;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.RemoteDesktop;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Views.RemoteDesktop
{
    [TestFixture]
    public class TestRemoteDesktopPane : WindowTestFixtureBase
    {
        // Use a larger machine type as all this RDP'ing consumes a fair
        // amount of memory.
        private const string MachineTypeForRdp = "n1-highmem-2";

        private readonly InstanceLocator SampleLocator =
            new InstanceLocator("project", "zone", "instance");

        //---------------------------------------------------------------------
        // Invalid server
        //---------------------------------------------------------------------

        [Test]
        public void WhenServerInvalid_ThenErrorIsShownAndWindowIsClosed()
        {
            var settings = RdpInstanceSettings.CreateNew(this.SampleLocator);

            var rdpService = new RemoteDesktopSessionBroker(this.serviceProvider);
            rdpService.Connect(
                this.SampleLocator,
                "invalid.corp",
                3389,
                settings);

            AwaitEvent<SessionAbortedEvent>();
            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(260, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }

        [Test]
        public void WhenPortNotListening_ThenErrorIsShownAndWindowIsClosed()
        {
            var settings = RdpInstanceSettings.CreateNew(this.SampleLocator);
            settings.ConnectionTimeout.IntValue = 5;

            var rdpService = new RemoteDesktopSessionBroker(this.serviceProvider);
            rdpService.Connect(
                this.SampleLocator,
                "localhost",
                1,
                settings);

            AwaitEvent<SessionAbortedEvent>();
            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(516, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }

        [Test]
        [Ignore("")]
        public void WhenWrongPort_ThenErrorIsShownAndWindowIsClosed()
        {
            var settings = RdpInstanceSettings.CreateNew(this.SampleLocator);

            var rdpService = new RemoteDesktopSessionBroker(this.serviceProvider);
            rdpService.Connect(
                this.SampleLocator,
                "localhost",
                135,    // That one will be listening, but it is RPC, not RDP.
                settings);

            AwaitEvent<SessionAbortedEvent>();
            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(2308, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }

        //---------------------------------------------------------------------
        // Connect via IAP
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenCredentialsInvalid_ThenErrorIsShownAndWindowIsClosed(
            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            {
                var settings = RdpInstanceSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.Username.StringValue = "wrong";
                settings.Password.Value = SecureStringExtensions.FromClearText("wrong");
                settings.AuthenticationLevel.EnumValue = RdpAuthenticationLevel.NoServerAuthentication;
                settings.UserAuthenticationBehavior.EnumValue = RdpUserAuthenticationBehavior.AbortOnFailure;
                settings.DesktopSize.EnumValue = RdpDesktopSize.ClientSize;

                var rdpService = new RemoteDesktopSessionBroker(this.serviceProvider);
                var session = rdpService.Connect(
                    locator,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    settings);

                AwaitEvent<SessionAbortedEvent>();
                Assert.IsNotNull(this.ExceptionShown);
                Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
                Assert.AreEqual(2055, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
            }
        }


        [Test]
        public async Task WhenCredentialsValid_ThenConnectingSucceeds(
            [Values(RdpConnectionBarState.AutoHide, RdpConnectionBarState.Off, RdpConnectionBarState.Pinned)]
            RdpConnectionBarState connectionBarState,

            [Values(RdpDesktopSize.ClientSize, RdpDesktopSize.ScreenSize)]
            RdpDesktopSize desktopSize,

            [Values(RdpAudioMode.DoNotPlay, RdpAudioMode.PlayLocally, RdpAudioMode.PlayOnServer)]
            RdpAudioMode audioMode,

            [Values(RdpRedirectClipboard.Disabled, RdpRedirectClipboard.Enabled)]
            RdpRedirectClipboard redirectClipboard,

            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            using (var gceAdapter = new ComputeEngineAdapter(this.serviceProvider.GetService<IAuthorizationAdapter>()))
            {
                var credentials = await gceAdapter.ResetWindowsUserAsync(
                    locator,
                    CreateRandomUsername(),
                    TimeSpan.FromSeconds(60),
                    CancellationToken.None);

                var settings = RdpInstanceSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.Username.StringValue = credentials.UserName;
                settings.Password.Value = credentials.SecurePassword;
                settings.ConnectionBar.EnumValue = connectionBarState;
                settings.DesktopSize.EnumValue = desktopSize;
                settings.AudioMode.EnumValue = audioMode;
                settings.RedirectClipboard.EnumValue = redirectClipboard;
                settings.AuthenticationLevel.EnumValue = RdpAuthenticationLevel.NoServerAuthentication;
                settings.BitmapPersistence.EnumValue = RdpBitmapPersistence.Disabled;

                var rdpService = new RemoteDesktopSessionBroker(this.serviceProvider);
                var session = rdpService.Connect(
                    locator,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    settings);

                AwaitEvent<SessionStartedEvent>();
                Assert.IsNull(this.ExceptionShown);


                SessionEndedEvent expectedEvent = null;

                this.serviceProvider.GetService<IEventService>()
                    .BindHandler<SessionEndedEvent>(e =>
                    {
                        expectedEvent = e;
                    });
                session.Close();

                Assert.IsNotNull(expectedEvent);
            }
        }


        [Test, Ignore("Unreliable in CI")]
        public async Task WhenSigningOutPerSendKeys_ThenWindowIsClosed(
            [WindowsInstance(ImageFamily = WindowsInstanceAttribute.WindowsServer2019)]
            ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            using (var gceAdapter = new ComputeEngineAdapter(this.serviceProvider.GetService<IAuthorizationAdapter>()))
            {
                var credentials = await gceAdapter.ResetWindowsUserAsync(
                       locator,
                       CreateRandomUsername(),
                       TimeSpan.FromSeconds(60),
                       CancellationToken.None);

                var settings = RdpInstanceSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.Username.StringValue = credentials.UserName;
                settings.Password.Value = credentials.SecurePassword;
                settings.AuthenticationLevel.EnumValue = RdpAuthenticationLevel.NoServerAuthentication;
                settings.BitmapPersistence.EnumValue = RdpBitmapPersistence.Disabled;
                settings.DesktopSize.EnumValue = RdpDesktopSize.ClientSize;

                var rdpService = new RemoteDesktopSessionBroker(this.serviceProvider);
                var session = (RemoteDesktopPane)rdpService.Connect(
                    locator,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    settings);

                AwaitEvent<SessionStartedEvent>();

                Thread.Sleep(5000);
                session.ShowSecurityScreen();
                Thread.Sleep(1000);
                session.SendKeys(Keys.Menu, Keys.S); // Sign out.

                AwaitEvent<SessionEndedEvent>();
                Assert.IsNull(this.ExceptionShown);
            }
        }

        //---------------------------------------------------------------------
        // TryGetExistingPane, TryGetActivePane
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenTryGetExistingPaneReturnsNull()
        {
            var broker = new RemoteDesktopSessionBroker(this.serviceProvider);

            Assert.IsNull(RemoteDesktopPane.TryGetExistingPane(this.mainForm, SampleLocator));
        }

        [Test]
        public void WhenNotConnected_ThenTryGetActivePaneReturnsNull()
        {
            Assert.IsNull(RemoteDesktopPane.TryGetActivePane(this.mainForm));
        }

        [Test]
        public async Task WhenConnected_ThenGetActivePaneReturnsPane(
            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            using (var gceAdapter = new ComputeEngineAdapter(this.serviceProvider.GetService<IAuthorizationAdapter>()))
            {
                var credentials = await gceAdapter.ResetWindowsUserAsync(
                    locator,
                    CreateRandomUsername(),
                    TimeSpan.FromSeconds(60),
                    CancellationToken.None);

                var settings = RdpInstanceSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.Username.StringValue = credentials.UserName;
                settings.Password.Value = credentials.SecurePassword;

                // Connect
                var rdpService = new RemoteDesktopSessionBroker(this.serviceProvider);
                IRemoteDesktopSession session = null;
                AssertRaisesEvent<SessionStartedEvent>(
                    () => session = (RemoteDesktopPane)rdpService.Connect(
                        locator,
                        "localhost",
                        (ushort)tunnel.LocalPort,
                        settings));

                Assert.IsNull(this.ExceptionShown);

                Assert.AreSame(session, RemoteDesktopPane.TryGetActivePane(this.mainForm));
                Assert.AreSame(session, RemoteDesktopPane.TryGetExistingPane(this.mainForm, locator));

                AssertRaisesEvent<SessionEndedEvent>(
                    () => session.Close());
            }
        }
    }
}
