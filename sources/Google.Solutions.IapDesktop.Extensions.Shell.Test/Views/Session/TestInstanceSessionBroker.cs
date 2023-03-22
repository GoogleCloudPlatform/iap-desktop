//
// Copyright 2023 Google LLC
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
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Session;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Views;
using Google.Solutions.Testing.Common.Integration;
using Google.Solutions.Testing.Common.Mocks;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Session
{
    [TestFixture]
    [UsesCloudResourcesAttribute]
    public class TestInstanceSessionBroker : WindowTestFixtureBase
    {
        // Use a larger machine type as all this RDP'ing consumes a fair
        // amount of memory.
        private const string MachineTypeForRdp = "n1-highmem-2";

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
        // IsConnected.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenIsConnectedIsFalse()
        {
            var serviceProvider = CreateServiceProvider();
            var sampleLocator = new InstanceLocator("project", "zone", "instance");
            var broker = new InstanceSessionBroker(serviceProvider);
            Assert.IsFalse(broker.IsConnected(sampleLocator));
        }

        //---------------------------------------------------------------------
        // ActiveSession.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenActiveSessionReturnsNull()
        {
            var serviceProvider = CreateServiceProvider();
            var broker = new InstanceSessionBroker(serviceProvider);
            Assert.IsNull(broker.ActiveSession);
        }

        //---------------------------------------------------------------------
        // TryActivate
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenTryActivateReturnsFalse()
        {
            var serviceProvider = CreateServiceProvider();
            var sampleLocator = new InstanceLocator("project", "zone", "instance");
            var broker = new InstanceSessionBroker(serviceProvider);
            Assert.IsFalse(broker.TryActivate(sampleLocator, out var _));

        }

        [Test]
        public async Task WhenRdpSessionExists_ThenTryActivateSucceeds(
            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var serviceProvider = CreateServiceProvider(await credential);
            var locator = await testInstance;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                await credential))
            {
                var credentials = await GenerateWindowsCredentials(locator)
                    .ConfigureAwait(true);

                var settings = InstanceConnectionSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.RdpUsername.StringValue = credentials.UserName;
                settings.RdpPassword.Value = credentials.SecurePassword;

                var template = new RdpConnectionTemplate(
                    locator,
                    true,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    settings);

                // Connect
                var broker = new InstanceSessionBroker(serviceProvider);
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(
                        () => session = (RemoteDesktopView)broker.Connect(template))
                    .ConfigureAwait(true);

                Assert.IsNull(this.ExceptionShown);

                Assert.AreSame(session, RemoteDesktopView.TryGetActivePane(this.MainWindow));
                Assert.AreSame(session, RemoteDesktopView.TryGetExistingPane(this.MainWindow, locator));
                Assert.IsTrue(broker.IsConnected(locator));
                Assert.IsTrue(broker.TryActivate(locator, out var _));

                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => session.Close())
                    .ConfigureAwait(true);
            }
        }

        [Test]
        public async Task WhenSshSessionExists_ThenGetActivePaneReturnsPane(
            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var serviceProvider = CreateServiceProvider(await credential);
            var locator = await testInstance;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                await credential))
            {
                var credentials = await GenerateWindowsCredentials(locator)
                    .ConfigureAwait(true);

                var settings = InstanceConnectionSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.RdpUsername.StringValue = credentials.UserName;
                settings.RdpPassword.Value = credentials.SecurePassword;

                var template = new RdpConnectionTemplate(
                    locator,
                    true,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    settings);

                // Connect
                var broker = new InstanceSessionBroker(serviceProvider);
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(
                        () => session = (RemoteDesktopView)broker.Connect(template))
                    .ConfigureAwait(true);

                Assert.IsNull(this.ExceptionShown);

                Assert.AreSame(session, RemoteDesktopView.TryGetActivePane(this.MainWindow));
                Assert.AreSame(session, RemoteDesktopView.TryGetExistingPane(this.MainWindow, locator));
                Assert.IsTrue(broker.IsConnected(locator));
                Assert.IsTrue(broker.TryActivate(locator, out var _));

                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => session.Close())
                    .ConfigureAwait(true);
            }
        }
    }
}
