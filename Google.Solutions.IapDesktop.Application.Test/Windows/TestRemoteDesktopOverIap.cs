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

using Google.Solutions.Compute.Test.Env;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Registry;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Windows.RemoteDesktop;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("IAP")]
    public class TestRemoteDesktopOverIap : WindowTestFixtureBase
    {
        [Test]
        public async Task WhenCredentialsInvalid_ThenErrorIsShownAndWindowIsClosed(
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            using (var tunnel = RdpTunnel.Create(testInstance.InstanceReference))
            {
                var rdpService = new RemoteDesktopService(this.serviceProvider);
                var session = rdpService.Connect(
                    testInstance.InstanceReference,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    new VmInstanceSettings()
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
        //    [WindowsInstance] InstanceRequest testInstance)
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
            [WindowsInstance(MachineType = "n1-standard-2")] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            using (var tunnel = RdpTunnel.Create(testInstance.InstanceReference))
            using (var gceAdapter = new ComputeEngineAdapter(this.serviceProvider.GetService<IAuthorizationService>()))
            {
                var credentials = await gceAdapter.ResetWindowsUserAsync(
                    testInstance.InstanceReference,
                    "test",
                    CancellationToken.None);

                var rdpService = new RemoteDesktopService(this.serviceProvider);
                var session = rdpService.Connect(
                    testInstance.InstanceReference,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    new VmInstanceSettings()
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
            InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            using (var tunnel = RdpTunnel.Create(testInstance.InstanceReference))
            using (var gceAdapter = new ComputeEngineAdapter(this.serviceProvider.GetService<IAuthorizationService>()))
            {
                var credentials = await gceAdapter.ResetWindowsUserAsync(
                       testInstance.InstanceReference,
                       "test",
                       CancellationToken.None);

                var rdpService = new RemoteDesktopService(this.serviceProvider);
                var session = (RemoteDesktopPane)rdpService.Connect(
                    testInstance.InstanceReference,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    new VmInstanceSettings()
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
