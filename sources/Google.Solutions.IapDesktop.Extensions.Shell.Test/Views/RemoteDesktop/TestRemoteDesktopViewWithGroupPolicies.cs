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
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
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
using System.Net;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.RemoteDesktop
{
    [TestFixture]
    [UsesCloudResources]
    public class TestRemoteDesktopViewWithServerSideGroupPolicies : WindowTestFixtureBase
    {
        private IServiceProvider CreateServiceProvider(ICredential credential)
        {
            var registry = new ServiceRegistry(this.ServiceRegistry);
            registry.AddTransient<RemoteDesktopView>();
            registry.AddTransient<RemoteDesktopViewModel>();
            registry.AddMock<IThemeService>();
            registry.AddMock<IBindingContext>();
            registry.AddSingleton(CreateAuthorizationMock(credential).Object);
            return registry;
        }

        private async Task<RdpSessionParameters> CreateSessionParametersAsync(
            InstanceLocator instanceLocator)
        {
            var windowsCredentials = await GenerateWindowsCredentials(instanceLocator)
                .ConfigureAwait(true);

            return new RdpSessionParameters(
                new RdpCredentials(
                    windowsCredentials.UserName,
                    windowsCredentials.Domain,
                    windowsCredentials.SecurePassword))
            {
                AuthenticationLevel = RdpAuthenticationLevel.NoServerAuthentication,
                BitmapPersistence = RdpBitmapPersistence.Disabled,
                DesktopSize = RdpDesktopSize.ClientSize,
                RedirectClipboard = RdpRedirectClipboard.Enabled,
                RedirectPrinter = RdpRedirectPrinter.Enabled,
                RedirectPort = RdpRedirectPort.Enabled,
                RedirectSmartCard = RdpRedirectSmartCard.Enabled,
                RedirectDrive = RdpRedirectDrive.Enabled,
                RedirectDevice = RdpRedirectDevice.Enabled,
            };
        }

        private async Task<IRemoteDesktopSession> ConnectAsync(
            IapTunnel tunnel,
            InstanceLocator instanceLocator,
            ICredential credential)
        {
            var serviceProvider = CreateServiceProvider(credential);
            var broker = new InstanceSessionBroker(serviceProvider);
            var parameters = await CreateSessionParametersAsync(instanceLocator)
                .ConfigureAwait(true);

            return broker.Connect(
                new ConnectionTemplate<RdpSessionParameters>(
                    new TransportParameters(
                        TransportParameters.TransportType.IapTunnel,
                        instanceLocator,
                        new IPEndPoint(IPAddress.Loopback, tunnel.LocalPort)),
                    parameters));
        }

        [Test]
        public async Task WhenAllowUsersToConnectRemotelyByUsingRdsIsOff_ThenErrorIsShownAndWindowIsClosed(
            [WindowsInstance(InitializeScript = @"
                # Disable Policy
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDenyTSConnections /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credentialTask)
        {
            var locator = await testInstance;
            var credential = await credentialTask;

            using (var tunnel = IapTunnel.ForRdp(locator, credential))
            {
                await AssertRaisesEventAsync<SessionAbortedEvent>(
                    () => ConnectAsync(tunnel, locator, credential))
                    .ConfigureAwait(true);

                Assert.IsNotNull(this.ExceptionShown);
                Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
                Assert.AreEqual(264, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
            }
        }

        [Test]
        public async Task WhenNlaDisabledAndServerRequiresNla_ThenErrorIsShownAndWindowIsClosed(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var serviceProvider = CreateServiceProvider(await credential);
            var locator = await testInstance;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                await credential))
            {
                var parameters = await CreateSessionParametersAsync(locator).ConfigureAwait(true);
                parameters.NetworkLevelAuthentication = RdpNetworkLevelAuthentication.Disabled;

                var broker = new InstanceSessionBroker(serviceProvider);

                await AssertRaisesEventAsync<SessionAbortedEvent>(
                    () => broker.Connect(
                        new ConnectionTemplate<RdpSessionParameters>(
                            new TransportParameters(
                                TransportParameters.TransportType.IapTunnel,
                                locator,
                                new IPEndPoint(IPAddress.Loopback, tunnel.LocalPort)),
                            parameters)))
                    .ConfigureAwait(true);

                Assert.IsNotNull(this.ExceptionShown);
                Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
                Assert.AreEqual(2825, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
            }
        }

        [Test]
        public async Task WhenNlaDisabledAndServerDoesNotRequireNla_ThenServerAuthWarningIsDisplayed(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v UserAuthentication /d 0 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var serviceProvider = CreateServiceProvider(await credential);
            var locator = await testInstance;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                await credential))
            {
                var parameters = await CreateSessionParametersAsync(locator).ConfigureAwait(true);
                parameters.NetworkLevelAuthentication = RdpNetworkLevelAuthentication.Disabled;

                var broker = new InstanceSessionBroker(serviceProvider);
                var session = broker.Connect(
                    new ConnectionTemplate<RdpSessionParameters>(
                        new TransportParameters(
                            TransportParameters.TransportType.IapTunnel,
                            locator,
                            new IPEndPoint(IPAddress.Loopback, tunnel.LocalPort)),
                        parameters));

                bool serverAuthWarningIsDisplayed = false;
                ((RemoteDesktopView)session).AuthenticationWarningDisplayed += (sender, args) =>
                {
                    serverAuthWarningIsDisplayed = true;
                    MainWindow.Close();
                };

                var deadline = DateTime.Now.AddSeconds(45);
                while (!serverAuthWarningIsDisplayed && DateTime.Now < deadline)
                {
                    try
                    {
                        PumpWindowMessages();
                    }
                    catch (AccessViolationException) { }
                }

                Assert.IsTrue(serverAuthWarningIsDisplayed);
            }
        }

        [Test, Ignore("Unreliable in CI")]
        public async Task WhenSetClientConnectionEncryptionLevelSetToLow_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v MinEncryptionLevel /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credentialTask)
        {
            var locator = await testInstance;
            var credential = await credentialTask;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                credential))
            {
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, credential).ConfigureAwait(true);
                    })
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                Delay(TimeSpan.FromSeconds(5));

                await AssertRaisesEventAsync<SessionEndedEvent>(() => session.Close())
                    .ConfigureAwait(true);
            }
        }

        [Test, Ignore("Unreliable in CI")]
        public async Task WhenSetClientConnectionEncryptionLevelSetToHigh_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v MinEncryptionLevel /d 3 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credentialTask)
        {
            var locator = await testInstance;
            var credential = await credentialTask;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                credential))
            {
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, credential).ConfigureAwait(true);
                    })
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                Delay(TimeSpan.FromSeconds(5));

                await AssertRaisesEventAsync<SessionEndedEvent>(() => session.Close())
                    .ConfigureAwait(true);
            }
        }

        [Test]
        public async Task WhenRequireUseOfSpecificSecurityLayerForRdpConnectionsSetToRdp_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v SecurityLayer /d 0 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credentialTask)
        {
            var locator = await testInstance;
            var credential = await credentialTask;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                credential))
            {
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, credential).ConfigureAwait(true);
                    })
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                Delay(TimeSpan.FromSeconds(5));

                await AssertRaisesEventAsync<SessionEndedEvent>(() => session.Close())
                    .ConfigureAwait(true);
            }
        }

        [Test]
        public async Task WhenRequireUseOfSpecificSecurityLayerForRdpConnectionsSetToNegotiate_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v SecurityLayer /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credentialTask)
        {
            var locator = await testInstance;
            var credential = await credentialTask;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                credential))
            {
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, credential).ConfigureAwait(true);
                    })
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                Delay(TimeSpan.FromSeconds(5));

                await AssertRaisesEventAsync<SessionEndedEvent>(() => session.Close())
                    .ConfigureAwait(true);
            }
        }

        [Test]
        public async Task WhenRequireUseOfSpecificSecurityLayerForRdpConnectionsSetToSsl_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v SecurityLayer /d 2 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credentialTask)
        {
            var locator = await testInstance;
            var credential = await credentialTask;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                credential))
            {
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, credential).ConfigureAwait(true);
                    })
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                Delay(TimeSpan.FromSeconds(5));

                await AssertRaisesEventAsync<SessionEndedEvent>(() => session.Close())
                    .ConfigureAwait(true);
            }
        }

        [Test]
        public async Task WhenRequireUserAuthenticationForRemoteConnectionsByNlaDisabled_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v UserAuthentication /d 0 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credentialTask)
        {
            var locator = await testInstance;
            var credential = await credentialTask;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                credential))
            {
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, credential).ConfigureAwait(true);
                    })
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                Delay(TimeSpan.FromSeconds(5));

                await AssertRaisesEventAsync<SessionEndedEvent>(() => session.Close())
                    .ConfigureAwait(true);
            }
        }

        [Test]
        public async Task WhenRequireUserAuthenticationForRemoteConnectionsByNlaEnabled_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v UserAuthentication /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credentialTask)
        {
            var locator = await testInstance;
            var credential = await credentialTask;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                credential))
            {
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, credential).ConfigureAwait(true);
                    })
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                Delay(TimeSpan.FromSeconds(5));

                await AssertRaisesEventAsync<SessionEndedEvent>(() => session.Close())
                    .ConfigureAwait(true);
            }
        }

        [Test]
        public async Task WhenLocalResourceRedirectionDisabled_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisableClip /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisableLPT /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisableCcm /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisableCdm /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fEnableSmartCard /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisablePNPRedir /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisableCpm /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credentialTask)
        {
            var locator = await testInstance;
            var credential = await credentialTask;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                credential))
            {
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, credential).ConfigureAwait(true);
                    })
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                Delay(TimeSpan.FromSeconds(5));

                await AssertRaisesEventAsync<SessionEndedEvent>(() => session.Close())
                    .ConfigureAwait(true);
            }
        }
    }
}
