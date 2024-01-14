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
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Views;
using Moq;
using NUnit.Framework;
using System;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Rdp
{
    [TestFixture]
    [UsesCloudResources]
    [RdpTest]
    public class TestRemoteDesktopView : WindowTestFixtureBase
    {
        //
        // Use a larger machine type as all this RDP'ing consumes a fair
        // amount of memory.
        //
        private const string MachineTypeForRdp = "n1-highmem-2";

        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project", "zone", "instance");

        private IServiceProvider CreateServiceProvider(IAuthorization authorization)
        {
            var registry = new ServiceRegistry(this.ServiceRegistry);
            registry.AddTransient<RdpDesktopView>();
            registry.AddTransient<RdpViewModel>();
            registry.AddMock<IThemeService>();
            registry.AddMock<IBindingContext>();
            registry.AddTransient<IToolWindowHost, ToolWindowHost>();
            registry.AddSingleton(authorization);
            return registry;
        }

        private static ITransport CreateTransportForEndpoint(IPEndPoint endpoint)
        {
            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Protocol).Returns(RdpProtocol.Protocol);
            transport.SetupGet(t => t.Endpoint).Returns(endpoint);
            return transport.Object;
        }

        //---------------------------------------------------------------------
        // Invalid server
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPortNotListening_ThenErrorIsShownAndWindowIsClosed(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var unboundEndpoint = new IPEndPoint(IPAddress.Loopback, 1);
            var transport = CreateTransportForEndpoint(unboundEndpoint);

            var serviceProvider = CreateServiceProvider(await auth);
            var broker = new InstanceSessionBroker(serviceProvider);
            await AssertRaisesEventAsync<SessionAbortedEvent>(
                () => broker.ConnectRdpSession(
                    SampleLocator,
                    transport,
                    new RdpParameters()
                    {
                        ConnectionTimeout = TimeSpan.FromSeconds(5)
                    },
                    RdpCredential.Empty))
                .ConfigureAwait(true);

            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(516, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }

        [Test]
        [Ignore("")]
        public async Task WhenWrongPort_ThenErrorIsShownAndWindowIsClosed(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            // That one will be listening, but it is RPC, not RDP.
            var wrongEndpoint = new IPEndPoint(IPAddress.Loopback, 135);
            var transport = CreateTransportForEndpoint(wrongEndpoint);

            var serviceProvider = CreateServiceProvider(await auth);
            var broker = new InstanceSessionBroker(serviceProvider);

            await AssertRaisesEventAsync<SessionAbortedEvent>(
                () => broker.ConnectRdpSession(
                    SampleLocator,
                    transport,
                    new RdpParameters(),
                    RdpCredential.Empty))
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
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var serviceProvider = CreateServiceProvider(await auth);
            var instance = await testInstance;

            using (var tunnel = IapTransport.ForRdp(
                instance,
                await auth))
            {
                var rdpCredential = new RdpCredential(
                    "wrong",
                    null,
                    SecureStringExtensions.FromClearText("wrong"));
                var rdpParameters = new RdpParameters()
                {
                    AuthenticationLevel = RdpAuthenticationLevel.NoServerAuthentication,
                    UserAuthenticationBehavior = RdpUserAuthenticationBehavior.AbortOnFailure,
                    DesktopSize = RdpDesktopSize.ClientSize
                };

                var broker = new InstanceSessionBroker(serviceProvider);

                await AssertRaisesEventAsync<SessionAbortedEvent>(
                    () => broker.ConnectRdpSession(
                        instance,
                        tunnel,
                        rdpParameters,
                        rdpCredential))
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

            [Values(RdpAudioMode.DoNotPlay, RdpAudioMode.PlayLocally, RdpAudioMode.PlayOnServer)]
            RdpAudioMode audioMode,

            [Values(RdpRedirectClipboard.Disabled, RdpRedirectClipboard.Enabled)]
            RdpRedirectClipboard redirectClipboard,

            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var serviceProvider = CreateServiceProvider(await auth);
            var instance = await testInstance;
            var windowsCredentials = await GenerateWindowsCredentialsAsync(instance).ConfigureAwait(true);

            // To avoid excessive combinations, combine some settings.
            var redirectPrinter = (RdpRedirectPrinter)(int)redirectClipboard;
            var redirectSmartCard = (RdpRedirectSmartCard)(int)redirectClipboard;
            var redirectPort = (RdpRedirectPort)(int)redirectClipboard;
            var redirectDrive = (RdpRedirectDrive)(int)redirectClipboard;
            var redirectDevice = (RdpRedirectDevice)(int)redirectClipboard;

            using (var tunnel = IapTransport.ForRdp(
                instance,
                await auth))
            {
                var rdpCredential = new RdpCredential(
                    windowsCredentials.UserName,
                    windowsCredentials.Domain,
                    windowsCredentials.SecurePassword);
                var rdpParameters = new RdpParameters()
                {
                    ConnectionBar = connectionBarState,
                    AudioMode = audioMode,
                    RedirectClipboard = redirectClipboard,
                    AuthenticationLevel = RdpAuthenticationLevel.NoServerAuthentication,
                    BitmapPersistence = RdpBitmapPersistence.Disabled,
                    RedirectPrinter = redirectPrinter,
                    RedirectSmartCard = redirectSmartCard,
                    RedirectPort = redirectPort,
                    RedirectDrive = redirectDrive,
                    RedirectDevice = redirectDevice,
                };

                var broker = new InstanceSessionBroker(serviceProvider);

                IRdpSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(
                    () => session = broker.ConnectRdpSession(
                        instance,
                        tunnel,
                        rdpParameters,
                        rdpCredential))
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
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var serviceProvider = CreateServiceProvider(await auth);
            var instance = await testInstance;
            var windowsCredentials = await GenerateWindowsCredentialsAsync(instance).ConfigureAwait(true);

            using (var tunnel = IapTransport.ForRdp(
                instance,
                await auth))
            {
                var rdpCredential = new RdpCredential(
                    windowsCredentials.UserName,
                    windowsCredentials.Domain,
                    windowsCredentials.SecurePassword);
                var rdpPparameters = new RdpParameters();

                var broker = new InstanceSessionBroker(serviceProvider);

                RdpDesktopView session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(
                    () => session = (RdpDesktopView)broker.ConnectRdpSession(
                        instance,
                        tunnel,
                        rdpPparameters,
                        rdpCredential))
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
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var serviceProvider = CreateServiceProvider(await auth);
            var instance = await testInstance;
            var windowsCredentials = await GenerateWindowsCredentialsAsync(instance).ConfigureAwait(true);

            using (var tunnel = IapTransport.ForRdp(
                instance,
                await auth))
            {
                var rdpCredential = new RdpCredential(
                    windowsCredentials.UserName,
                    windowsCredentials.Domain,
                    windowsCredentials.SecurePassword);
                var rdpParameters = new RdpParameters()
                {
                    AuthenticationLevel = RdpAuthenticationLevel.NoServerAuthentication,
                    BitmapPersistence = RdpBitmapPersistence.Disabled,
                    DesktopSize = RdpDesktopSize.ClientSize
                };

                var broker = new InstanceSessionBroker(serviceProvider);

                RdpDesktopView session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(
                    () => session = (RdpDesktopView)broker.ConnectRdpSession(
                        instance,
                        tunnel,
                        rdpParameters,
                        rdpCredential))
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
