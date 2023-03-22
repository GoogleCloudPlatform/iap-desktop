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
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Session;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Views;
using Google.Solutions.Testing.Common.Integration;
using NUnit.Framework;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.RemoteDesktop
{
    [TestFixture]
    [UsesCloudResources]
    public class TestRemoteDesktopView : WindowTestFixtureBase
    {
        // Use a larger machine type as all this RDP'ing consumes a fair
        // amount of memory.
        private const string MachineTypeForRdp = "n1-highmem-2";

        private readonly InstanceLocator SampleLocator =
            new InstanceLocator("project", "zone", "instance");

        private IServiceProvider CreateServiceProvider(ICredential credential = null)
        {
            var registry = new ServiceRegistry(this.ServiceRegistry);
            registry.AddTransient<RemoteDesktopView>();
            registry.AddTransient<RemoteDesktopViewModel>();
            registry.AddMock<IThemeService>();
            registry.AddMock<IBindingContext>();
            registry.AddSingleton(CreateAuthorizationMock(credential).Object);
            return registry;
        }

        //---------------------------------------------------------------------
        // Invalid server
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenServerInvalid_ThenErrorIsShownAndWindowIsClosed()
        {
            var serviceProvider = CreateServiceProvider();
            var settings = InstanceConnectionSettings.CreateNew(this.SampleLocator);

            var broker = new InstanceSessionBroker(serviceProvider);

            await AssertRaisesEventAsync<SessionAbortedEvent>(() => broker.Connect(
                    new RdpConnectionTemplate(
                        this.SampleLocator,
                        true,
                        "invalid.corp",
                        3389,
                        settings)))
                .ConfigureAwait(true);

            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(260, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }

        [Test]
        public async Task WhenPortNotListening_ThenErrorIsShownAndWindowIsClosed()
        {
            var serviceProvider = CreateServiceProvider();
            var settings = InstanceConnectionSettings.CreateNew(this.SampleLocator);
            settings.RdpConnectionTimeout.IntValue = 5;

            var broker = new InstanceSessionBroker(serviceProvider);

            await AssertRaisesEventAsync<SessionAbortedEvent>(() => broker.Connect(
                    new RdpConnectionTemplate(
                        this.SampleLocator,
                        true,
                        "localhost",
                        1,
                        settings)))
                .ConfigureAwait(true);

            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(516, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }

        [Test]
        [Ignore("")]
        public async Task WhenWrongPort_ThenErrorIsShownAndWindowIsClosed()
        {
            var serviceProvider = CreateServiceProvider();
            var settings = InstanceConnectionSettings.CreateNew(this.SampleLocator);

            var broker = new InstanceSessionBroker(serviceProvider);

            await AssertRaisesEventAsync<SessionAbortedEvent>(() => broker.Connect(
                    new RdpConnectionTemplate(
                        this.SampleLocator,
                        true,
                        "localhost",
                        135,    // That one will be listening, but it is RPC, not RDP.
                        settings)))
                .ConfigureAwait(true);

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
            var serviceProvider = CreateServiceProvider(await credential);
            var locator = await testInstance;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                await credential))
            {
                var settings = InstanceConnectionSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.RdpUsername.StringValue = "wrong";
                settings.RdpPassword.Value = SecureStringExtensions.FromClearText("wrong");
                settings.RdpAuthenticationLevel.EnumValue = RdpAuthenticationLevel.NoServerAuthentication;
                settings.RdpUserAuthenticationBehavior.EnumValue = RdpUserAuthenticationBehavior.AbortOnFailure;
                settings.RdpDesktopSize.EnumValue = RdpDesktopSize.ClientSize;

                var broker = new InstanceSessionBroker(serviceProvider);

                await AssertRaisesEventAsync<SessionAbortedEvent>(() => broker.Connect(
                        new RdpConnectionTemplate(
                            locator,
                            true,
                            "localhost",
                            (ushort)tunnel.LocalPort,
                            settings)))
                    .ConfigureAwait(true);

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
            var serviceProvider = CreateServiceProvider(await credential);
            var locator = await testInstance;
            var windowsCredentials = await GenerateWindowsCredentials(locator).ConfigureAwait(true);

            // To avoid excessive combinations, combine some settings.
            var redirectPrinter = (RdpRedirectPrinter)(int)redirectClipboard;
            var redirectSmartCard = (RdpRedirectSmartCard)(int)redirectClipboard;
            var redirectPort = (RdpRedirectPort)(int)redirectClipboard;
            var redirectDrive = (RdpRedirectDrive)(int)redirectClipboard;
            var redirectDevice = (RdpRedirectDevice)(int)redirectClipboard;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                await credential))
            {
                var settings = InstanceConnectionSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.RdpUsername.StringValue = windowsCredentials.UserName;
                settings.RdpPassword.Value = windowsCredentials.SecurePassword;
                settings.RdpConnectionBar.EnumValue = connectionBarState;
                settings.RdpDesktopSize.EnumValue = desktopSize;
                settings.RdpAudioMode.EnumValue = audioMode;
                settings.RdpRedirectClipboard.EnumValue = redirectClipboard;
                settings.RdpAuthenticationLevel.EnumValue = RdpAuthenticationLevel.NoServerAuthentication;
                settings.RdpBitmapPersistence.EnumValue = RdpBitmapPersistence.Disabled;
                settings.RdpRedirectClipboard.EnumValue = redirectClipboard;
                settings.RdpRedirectPrinter.EnumValue = redirectPrinter;
                settings.RdpRedirectSmartCard.EnumValue = redirectSmartCard;
                settings.RdpRedirectPort.EnumValue = redirectPort;
                settings.RdpRedirectDrive.EnumValue = redirectDrive;
                settings.RdpRedirectDevice.EnumValue = redirectDevice;

                var broker = new InstanceSessionBroker(serviceProvider);

                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(() =>
                    {
                        session = broker.Connect(new RdpConnectionTemplate(
                            locator,
                            true,
                            "localhost",
                            (ushort)tunnel.LocalPort,
                            settings));
                    })
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                await AssertRaisesEventAsync<SessionEndedEvent>(() => session.Close())
                    .ConfigureAwait(true);
            }
        }

        [Test]
        public async Task WhenWindowChangedToFloating_ThenConnectionSurvives(
            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var serviceProvider = CreateServiceProvider(await credential);
            var locator = await testInstance;
            var windowsCredentials = await GenerateWindowsCredentials(locator).ConfigureAwait(true);

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                await credential))
            {
                var settings = InstanceConnectionSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.RdpUsername.StringValue = windowsCredentials.UserName;
                settings.RdpPassword.Value = windowsCredentials.SecurePassword;

                var broker = new InstanceSessionBroker(serviceProvider);

                RemoteDesktopView session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(() =>
                    {
                        session = (RemoteDesktopView)broker.Connect(
                            new RdpConnectionTemplate(
                                locator,
                                true,
                                "localhost",
                                (ushort)tunnel.LocalPort,
                                settings));
                    })
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                // Float.
                session.FloatAt(new Rectangle(0, 0, 800, 600));
                await Task.Delay(TimeSpan.FromSeconds(2))
                    .ConfigureAwait(true);

                // Dock again.
                session.DockTo(session.DockPanel, DockStyle.Fill);
                await Task.Delay(TimeSpan.FromSeconds(2))
                    .ConfigureAwait(true);

                session.Close();
            }
        }

        [Test, Ignore("Unreliable in CI")]
        public async Task WhenSigningOutPerSendKeys_ThenWindowIsClosed(
            [WindowsInstance(ImageFamily = WindowsInstanceAttribute.WindowsServer2019)]
            ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var serviceProvider = CreateServiceProvider(await credential);
            var locator = await testInstance;
            var windowsCredentials = await GenerateWindowsCredentials(locator).ConfigureAwait(true);

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                await credential))
            {
                var settings = InstanceConnectionSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.RdpUsername.StringValue = windowsCredentials.UserName;
                settings.RdpPassword.Value = windowsCredentials.SecurePassword;
                settings.RdpAuthenticationLevel.EnumValue = RdpAuthenticationLevel.NoServerAuthentication;
                settings.RdpBitmapPersistence.EnumValue = RdpBitmapPersistence.Disabled;
                settings.RdpDesktopSize.EnumValue = RdpDesktopSize.ClientSize;

                var broker = new InstanceSessionBroker(serviceProvider);

                RemoteDesktopView session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(() =>
                    {
                        session = (RemoteDesktopView)broker.Connect(
                            new RdpConnectionTemplate(
                                locator,
                                true,
                                "localhost",
                                (ushort)tunnel.LocalPort,
                                settings));
                    })
                    .ConfigureAwait(true);

                Thread.Sleep(5000);
                session.ShowSecurityScreen();
                Thread.Sleep(1000);

                await AssertRaisesEventAsync<SessionEndedEvent>(() =>
                    {
                        session.SendKeys(Keys.Menu, Keys.S); // Sign out.
                    })
                    .ConfigureAwait(true);

                Assert.IsNull(this.ExceptionShown);
            }
        }
    }
}
