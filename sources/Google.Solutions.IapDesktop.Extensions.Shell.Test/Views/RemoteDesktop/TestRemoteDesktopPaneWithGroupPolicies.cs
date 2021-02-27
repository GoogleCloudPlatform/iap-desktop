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
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.RemoteDesktop
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("IAP")]
    public class TestRemoteDesktopWithServerSideGroupPolicies : WindowTestFixtureBase
    {
        private async Task<IRemoteDesktopSession> Connect(
            RdpTunnel tunnel,
            InstanceLocator vmInstanceReference)
        {
            using (var gceAdapter = new ComputeEngineAdapter(this.serviceProvider.GetService<IAuthorizationAdapter>()))
            {
                var credentials = await gceAdapter.ResetWindowsUserAsync(
                    vmInstanceReference,
                    CreateRandomUsername(),
                    TimeSpan.FromSeconds(60),
                    CancellationToken.None);

                var settings = InstanceConnectionSettings.CreateNew(vmInstanceReference);
                settings.RdpUsername.Value = credentials.UserName;
                settings.RdpPassword.Value = credentials.SecurePassword;
                settings.RdpAuthenticationLevel.Value = RdpAuthenticationLevel.NoServerAuthentication;
                settings.RdpBitmapPersistence.Value = RdpBitmapPersistence.Disabled;
                settings.RdpDesktopSize.Value = RdpDesktopSize.ClientSize;

                var rdpService = new RemoteDesktopSessionBroker(this.serviceProvider);
                return rdpService.Connect(
                    vmInstanceReference,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    settings);
            }
        }

        [Test]
        public async Task WhenAllowUsersToConnectRemotelyByUsingRdsIsOff_ThenErrorIsShownAndWindowIsClosed(
            [WindowsInstance(InitializeScript = @"
                # Disable Policy
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDenyTSConnections /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            {
                var session = await Connect(tunnel, locator);

                AwaitEvent<SessionAbortedEvent>(TimeSpan.FromSeconds(90));
                Assert.IsNotNull(this.ExceptionShown);
                Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
                Assert.AreEqual(264, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
            }
        }

        [Test, Ignore("Unreliable in CI")]
        public async Task WhenSetClientConnectionEncryptionLevelSetToLow_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v MinEncryptionLevel /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            {
                var session = await Connect(tunnel, locator);

                AwaitEvent<SessionStartedEvent>();
                Assert.IsNull(this.ExceptionShown);

                SessionEndedEvent expectedEvent = null;

                this.serviceProvider.GetService<IEventService>()
                    .BindHandler<SessionEndedEvent>(e =>
                    {
                        expectedEvent = e;
                    });

                Delay(TimeSpan.FromSeconds(5));
                session.Close();

                Assert.IsNotNull(expectedEvent);
            }
        }

        [Test, Ignore("Unreliable in CI")]
        public async Task WhenSetClientConnectionEncryptionLevelSetToHigh_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v MinEncryptionLevel /d 3 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            {
                var session = await Connect(tunnel, locator);

                AwaitEvent<SessionStartedEvent>();
                Assert.IsNull(this.ExceptionShown);

                SessionEndedEvent expectedEvent = null;

                this.serviceProvider.GetService<IEventService>()
                    .BindHandler<SessionEndedEvent>(e =>
                    {
                        expectedEvent = e;
                    });

                Delay(TimeSpan.FromSeconds(5));
                session.Close();

                Assert.IsNotNull(expectedEvent);
            }
        }

        [Test]
        public async Task WhenRequireUseOfSpecificSecurityLayerForRdpConnectionsSetToRdp_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v SecurityLayer /d 0 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            {
                var session = await Connect(tunnel, locator);

                AwaitEvent<SessionStartedEvent>();
                Assert.IsNull(this.ExceptionShown);

                SessionEndedEvent expectedEvent = null;

                this.serviceProvider.GetService<IEventService>()
                    .BindHandler<SessionEndedEvent>(e =>
                    {
                        expectedEvent = e;
                    });

                Delay(TimeSpan.FromSeconds(5));
                session.Close();

                Assert.IsNotNull(expectedEvent);
            }
        }

        [Test]
        public async Task WhenRequireUseOfSpecificSecurityLayerForRdpConnectionsSetToNegotiate_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v SecurityLayer /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            {
                var session = await Connect(tunnel, locator);

                AwaitEvent<SessionStartedEvent>();
                Assert.IsNull(this.ExceptionShown);

                SessionEndedEvent expectedEvent = null;

                this.serviceProvider.GetService<IEventService>()
                    .BindHandler<SessionEndedEvent>(e =>
                    {
                        expectedEvent = e;
                    });

                Delay(TimeSpan.FromSeconds(5));
                session.Close();

                Assert.IsNotNull(expectedEvent);
            }
        }

        [Test]
        public async Task WhenRequireUseOfSpecificSecurityLayerForRdpConnectionsSetToSsl_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v SecurityLayer /d 2 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            {
                var session = await Connect(tunnel, locator);

                AwaitEvent<SessionStartedEvent>();
                Assert.IsNull(this.ExceptionShown);

                SessionEndedEvent expectedEvent = null;

                this.serviceProvider.GetService<IEventService>()
                    .BindHandler<SessionEndedEvent>(e =>
                    {
                        expectedEvent = e;
                    });

                Delay(TimeSpan.FromSeconds(5));
                session.Close();

                Assert.IsNotNull(expectedEvent);
            }
        }

        [Test]
        public async Task WhenRequireUserAuthenticationForRemoteConnectionsByNlaDisabled_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v UserAuthentication /d 0 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            {
                var session = await Connect(tunnel, locator);

                AwaitEvent<SessionStartedEvent>();
                Assert.IsNull(this.ExceptionShown);

                SessionEndedEvent expectedEvent = null;

                this.serviceProvider.GetService<IEventService>()
                    .BindHandler<SessionEndedEvent>(e =>
                    {
                        expectedEvent = e;
                    });

                Delay(TimeSpan.FromSeconds(5));
                session.Close();

                Assert.IsNotNull(expectedEvent);
            }
        }

        [Test]
        public async Task WhenRequireUserAuthenticationForRemoteConnectionsByNlaEnabled_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v UserAuthentication /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            {
                var session = await Connect(tunnel, locator);

                AwaitEvent<SessionStartedEvent>();
                Assert.IsNull(this.ExceptionShown);

                SessionEndedEvent expectedEvent = null;

                this.serviceProvider.GetService<IEventService>()
                    .BindHandler<SessionEndedEvent>(e =>
                    {
                        expectedEvent = e;
                    });

                Delay(TimeSpan.FromSeconds(5));
                session.Close();

                Assert.IsNotNull(expectedEvent);
            }
        }
    }
}
