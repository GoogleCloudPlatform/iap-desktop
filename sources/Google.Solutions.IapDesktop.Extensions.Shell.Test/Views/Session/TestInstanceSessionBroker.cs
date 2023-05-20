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
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.RemoteDesktop;
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
            registry.AddTransient<IToolWindowHost, ToolWindowHost>();
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
            var instance = await testInstance;

            using (var tunnel = IapTransport.ForRdp(
                instance,
                await credential))
            {
                var credentials = await GenerateWindowsCredentials(instance).ConfigureAwait(true);
                var rdpCredential = new RdpCredential(
                    credentials.UserName,
                    credentials.Domain,
                    credentials.SecurePassword);

                var rdpParameters = new RdpSessionParameters();

                // Connect
                var broker = new InstanceSessionBroker(serviceProvider);
                IRemoteDesktopSession session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(
                        () => session = (RemoteDesktopView)broker.ConnectRdpSession(
                            instance,
                            tunnel,
                            rdpParameters,
                            rdpCredential))
                    .ConfigureAwait(true);

                Assert.IsNull(this.ExceptionShown);

                Assert.AreSame(session, RemoteDesktopView.TryGetActivePane(this.MainWindow));
                Assert.AreSame(session, RemoteDesktopView.TryGetExistingPane(this.MainWindow, instance));
                Assert.IsTrue(broker.IsConnected(instance));
                Assert.IsTrue(broker.TryActivate(instance, out var _));

                await AssertRaisesEventAsync<SessionEndedEvent>(
                        () => session.Close())
                    .ConfigureAwait(true);
            }
        }
    }
}
