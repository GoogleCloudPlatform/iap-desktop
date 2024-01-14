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
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Views;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Rdp
{
    [TestFixture]
    [UsesCloudResources]
    [RdpTest]
    public class TestRemoteDesktopViewWithServerSideGroupPolicies : WindowTestFixtureBase
    {
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

        private async Task<RdpCredential> GenerateRdpCredentialAsync(
            InstanceLocator instanceLocator)
        {
            var windowsCredentials = await GenerateWindowsCredentialsAsync(instanceLocator)
                .ConfigureAwait(true);

            return new RdpCredential(
                windowsCredentials.UserName,
                windowsCredentials.Domain,
                windowsCredentials.SecurePassword);
        }

        private RdpParameters CreateSessionParameters()
        {
            return new RdpParameters()
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

        private async Task<IRdpSession> ConnectAsync(
            IapTransport tunnel,
            InstanceLocator instance,
            IAuthorization authorization)
        {
            var serviceProvider = CreateServiceProvider(authorization);
            var broker = new InstanceSessionBroker(serviceProvider);
            var rdpCredential = await GenerateRdpCredentialAsync(instance)
                .ConfigureAwait(true);

            return broker.ConnectRdpSession(
                instance,
                tunnel,
                CreateSessionParameters(),
                rdpCredential);
        }

        [Test]
        public async Task WhenNlaDisabledAndServerRequiresNla_ThenErrorIsShownAndWindowIsClosed(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var serviceProvider = CreateServiceProvider(await auth);
            var instance = await testInstance;

            using (var tunnel = IapTransport.ForRdp(
                instance,
                await auth))
            {
                var rdpCredential = await GenerateRdpCredentialAsync(instance).ConfigureAwait(true);
                var rdpParameters = CreateSessionParameters();
                rdpParameters.NetworkLevelAuthentication = RdpNetworkLevelAuthentication.Disabled;

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
                Assert.AreEqual(2825, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
            }
        }

        [Test]
        public async Task WhenClientConnectionEncryptionLevelSetToLow_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v MinEncryptionLevel /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            var instance = await testInstance;
            var auth = await authTask;

            using (var tunnel = IapTransport.ForRdp(
                instance,
                auth))
            {
                IRdpSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, instance, auth).ConfigureAwait(true);
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
        public async Task WhenClientConnectionEncryptionLevelSetToHigh_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v MinEncryptionLevel /d 3 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            var locator = await testInstance;
            var auth = await authTask;

            using (var tunnel = IapTransport.ForRdp(
                locator,
                auth))
            {
                IRdpSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, auth).ConfigureAwait(true);
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
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            var locator = await testInstance;
            var auth = await authTask;

            using (var tunnel = IapTransport.ForRdp(
                locator,
                auth))
            {
                IRdpSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, auth).ConfigureAwait(true);
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
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            var locator = await testInstance;
            var auth = await authTask;

            using (var tunnel = IapTransport.ForRdp(
                locator,
                auth))
            {
                IRdpSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, auth).ConfigureAwait(true);
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
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            var locator = await testInstance;
            var auth = await authTask;

            using (var tunnel = IapTransport.ForRdp(
                locator,
                auth))
            {
                IRdpSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, auth).ConfigureAwait(true);
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
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            var locator = await testInstance;
            var auth = await authTask;

            using (var tunnel = IapTransport.ForRdp(
                locator,
                auth))
            {
                IRdpSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, auth).ConfigureAwait(true);
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
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            var locator = await testInstance;
            var auth = await authTask;

            using (var tunnel = IapTransport.ForRdp(
                locator,
                auth))
            {
                IRdpSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, auth).ConfigureAwait(true);
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
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            var locator = await testInstance;
            var auth = await authTask;

            using (var tunnel = IapTransport.ForRdp(
                locator,
                auth))
            {
                IRdpSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(async () =>
                    {
                        session = await ConnectAsync(tunnel, locator, auth).ConfigureAwait(true);
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
