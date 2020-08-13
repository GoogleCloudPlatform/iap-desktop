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

using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Application.Util;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Locator;

namespace Google.Solutions.IapDesktop.Application.Test.Views
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("IAP")]
    public class TestRemoteDesktopOverIap : WindowTestFixtureBase
    {
        [Test]
        public async Task WhenCredentialsInvalid_ThenErrorIsShownAndWindowIsClosed(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            {
                var rdpService = new RemoteDesktopService(this.serviceProvider);
                var session = rdpService.Connect(
                    locator,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    new VmInstanceConnectionSettings()
                    {
                        Username = "wrong",
                        Password = SecureStringExtensions.FromClearText("wrong"),
                        AuthenticationLevel = RdpAuthenticationLevel.NoServerAuthentication,
                        UserAuthenticationBehavior = RdpUserAuthenticationBehavior.AbortOnFailure,
                        DesktopSize = RdpDesktopSize.ClientSize
                    });

                AwaitEvent<RemoteDesktopConnectionFailedEvent>();
                Assert.IsNotNull(this.ExceptionShown);
                Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
                Assert.AreEqual(2055, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
            }
        }

        //
        // There's no reliable way to dismiss the warning/error, so these tests seem 
        // challenging to implement.
        //
        //[Test]
        //public void WhenAttemptServerAuthentication_ThenWarningIsShown()
        //{
        //}

        //[Test]
        //public void WhenRequireServerAuthentication_ThenConnectionFails(
        //    [WindowsInstance] ResourceTask<InstanceLocator> testInstance)
        //{
        //}

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

            // Use a slightly larger machine type as all this RDP'ing consumes a fair
            // amount of memory.
            [WindowsInstance(MachineType = "n1-standard-2")] ResourceTask<InstanceLocator> testInstance,
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
                    CancellationToken.None);

                var rdpService = new RemoteDesktopService(this.serviceProvider);
                var session = rdpService.Connect(
                    locator,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    new VmInstanceConnectionSettings()
                    {
                        Username = credentials.UserName,
                        Password = credentials.SecurePassword,
                        ConnectionBar = connectionBarState,
                        DesktopSize = desktopSize,
                        AudioMode = audioMode,
                        RedirectClipboard = redirectClipboard,
                        AuthenticationLevel = RdpAuthenticationLevel.NoServerAuthentication,
                        BitmapPersistence = RdpBitmapPersistence.Disabled
                    });

                AwaitEvent<RemoteDesktopConnectionSuceededEvent>();
                Assert.IsNull(this.ExceptionShown);


                RemoteDesktopWindowClosedEvent expectedEvent = null;

                this.serviceProvider.GetService<IEventService>()
                    .BindHandler<RemoteDesktopWindowClosedEvent>(e =>
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
                       CancellationToken.None);

                var rdpService = new RemoteDesktopService(this.serviceProvider);
                var session = (RemoteDesktopPane)rdpService.Connect(
                    locator,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    new VmInstanceConnectionSettings()
                    {
                        Username = credentials.UserName,
                        Password = credentials.SecurePassword,
                        AuthenticationLevel = RdpAuthenticationLevel.NoServerAuthentication,
                        BitmapPersistence = RdpBitmapPersistence.Disabled,
                        DesktopSize = RdpDesktopSize.ClientSize
                    });

                AwaitEvent<RemoteDesktopConnectionSuceededEvent>();

                Thread.Sleep(5000);
                session.ShowSecurityScreen();
                Thread.Sleep(1000);
                session.SendKeys(Keys.Menu, Keys.S); // Sign out.

                AwaitEvent<RemoteDesktopWindowClosedEvent>();
                Assert.IsNull(this.ExceptionShown);
            }
        }
    }
}
