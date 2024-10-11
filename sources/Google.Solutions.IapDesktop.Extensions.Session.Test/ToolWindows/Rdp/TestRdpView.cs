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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Terminal.Controls;
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
    [RequiresRdp]
    public class TestRdpView : WindowTestFixtureBase
    {
        private static async Task<RdpCredential> GenerateRdpCredentialAsync(
            InstanceLocator instance)
        {
            var username = "test" + Guid.NewGuid().ToString().Substring(0, 4);
            var credentialAdapter = new WindowsCredentialGenerator(
                new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(),
                    TestProject.AdminAuthorization,
                    TestProject.UserAgent));

            var credential = await credentialAdapter
                .CreateWindowsCredentialsAsync(
                    instance,
                    username,
                    UserFlags.AddToAdministrators,
                    TimeSpan.FromSeconds(60),
                    CancellationToken.None)
                .ConfigureAwait(true);

            return new RdpCredential(
                credential.UserName,
                credential.Domain,
                credential.SecurePassword);
        }

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
            registry.AddTransient<RdpView>();
            registry.AddTransient<RdpViewModel>();
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
        public async Task Connect_WhenPortNotListening_ThenErrorIsShownAndWindowIsClosed(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var unboundEndpoint = new IPEndPoint(IPAddress.Loopback, 1);
            var transport = CreateTransportForEndpoint(unboundEndpoint);

            var serviceProvider = CreateServiceProvider(await auth);
            var broker = new SessionFactory(
                serviceProvider.GetService<IMainWindow>(),
                serviceProvider.GetService<ISessionBroker>(),
                serviceProvider.GetService<IToolWindowHost>(),
                serviceProvider.GetService<IJobService>());
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
            Assert.AreEqual(516, ((RdpDisconnectedException)this.ExceptionShown!).DisconnectReason);
        }

        //---------------------------------------------------------------------
        // Connect via IAP
        //---------------------------------------------------------------------

        [Test]
        public async Task Connect_WhenCredentialsInvalid_ThenErrorIsShownAndWindowIsClosed(
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
                    UserAuthenticationBehavior = RdpAutomaticLogin.LegacyAbortOnFailure
                };

                var broker = new SessionFactory(
                    serviceProvider.GetService<IMainWindow>(),
                    serviceProvider.GetService<ISessionBroker>(),
                    serviceProvider.GetService<IToolWindowHost>(),
                    serviceProvider.GetService<IJobService>());

                await AssertRaisesEventAsync<SessionAbortedEvent>(
                    () => broker.ConnectRdpSession(
                        instance,
                        tunnel,
                        rdpParameters,
                        rdpCredential))
                    .ConfigureAwait(true);

                Assert.IsNotNull(this.ExceptionShown);
                Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
                Assert.AreEqual(2055, ((RdpDisconnectedException)this.ExceptionShown!).DisconnectReason);
            }
        }

        [Test]
        public async Task Connect_WhenWindowChangedToFloating_ThenConnectionSurvives(
            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var serviceProvider = CreateServiceProvider(await auth);
            var instance = await testInstance;
            var rdpCredential = await
                GenerateRdpCredentialAsync(instance)
                .ConfigureAwait(true);

            using (var tunnel = IapTransport.ForRdp(
                instance,
                await auth))
            {
                var rdpParameters = new RdpParameters();

                var broker = new SessionFactory(
                    serviceProvider.GetService<IMainWindow>(),
                    serviceProvider.GetService<ISessionBroker>(),
                    serviceProvider.GetService<IToolWindowHost>(),
                    serviceProvider.GetService<IJobService>());

                RdpView? session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(
                    () => session = (RdpView)broker.ConnectRdpSession(
                        instance,
                        tunnel,
                        rdpParameters,
                        rdpCredential))
                    .ConfigureAwait(true);

                Assert.IsNotNull(session);
                Assert.IsNull(this.ExceptionShown);

                // Float.
                session!.FloatAt(new Rectangle(0, 0, 800, 600));
                await Task.Delay(TimeSpan.FromSeconds(2))
                    .ConfigureAwait(true);

                // Dock again.
                session.DockTo(session.DockPanel, DockStyle.Fill);
                await Task.Delay(TimeSpan.FromSeconds(2))
                    .ConfigureAwait(true);

                session.Close();
            }
        }
    }
}
