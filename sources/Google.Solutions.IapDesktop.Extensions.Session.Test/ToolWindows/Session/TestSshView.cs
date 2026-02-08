//
// Copyright 2024 Google LLC
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
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Views;
using Moq;
using NUnit.Framework;
using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSshView : WindowTestFixtureBase
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project", "zone", "instance");

        private IServiceProvider CreateServiceProvider(IAuthorization authorization)
        {
            var registry = new ServiceRegistry(this.ServiceRegistry);
            registry.AddTransient<SshView>();
            registry.AddTransient<SshViewModel>();
            registry.AddMock<IBindingContext>();
            registry.AddMock<IToolWindowTheme>();
            registry.AddMock<IInputDialog>();
            registry.AddTransient<IToolWindowHost, ToolWindowHost>();
            registry.AddSingleton(authorization);
            registry.AddSingleton<SessionFactory>();

            var settingsRepository = registry.AddMock<ITerminalSettingsRepository>();
            settingsRepository
                .Setup(r => r.GetSettings())
                .Returns(TerminalSettings.Default);

            return registry;
        }

        private static ITransport CreateTransportForEndpoint(IPEndPoint endpoint)
        {
            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Protocol).Returns(RdpProtocol.Protocol);
            transport.SetupGet(t => t.Endpoint).Returns(endpoint);
            return transport.Object;
        }

        private static async Task<ISshCredential> CrateSshCredential(
            InstanceLocator instance,
            IAuthorization authorization,
            SshKeyType keyType)
        {
            var keyAdapter = new PlatformCredentialFactory(
                authorization,
                new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(),
                    authorization,
                    TestProject.UserAgent),
                new ResourceManagerClient(
                    ResourceManagerClient.CreateEndpoint(),
                    authorization,
                    TestProject.UserAgent),
                new Mock<IOsLoginProfile>().Object);

            return await keyAdapter
                .CreateCredentialAsync(
                    instance,
                    AsymmetricKeySigner.CreateEphemeral(keyType),
                    TimeSpan.FromMinutes(10),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None)
                .ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Invalid server.
        //---------------------------------------------------------------------

        [Test]
        public async Task Connect_WhenPortNotListening(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var unboundEndpoint = new IPEndPoint(IPAddress.Loopback, 1);
            var transport = CreateTransportForEndpoint(unboundEndpoint);

            var serviceProvider = CreateServiceProvider(await auth);
            var factory = serviceProvider.GetService<SessionFactory>();

            await AssertRaisesEventAsync<SessionAbortedEvent>(
                () => factory.ConnectSshSession(
                    SampleLocator,
                    transport,
                    new SshParameters()
                    {
                        ConnectionTimeout = TimeSpan.FromSeconds(5)
                    },
                    new StaticPasswordCredential("user", string.Empty)))
                .ConfigureAwait(true);

            Assert.IsInstanceOf(typeof(SocketException), this.ExceptionShown);
        }

        //---------------------------------------------------------------------
        // Connect via IAP.
        //---------------------------------------------------------------------

        [Test]
        public async Task Connect_WhenCredentialsInvalid(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var instance = await testInstance;

            using (var tunnel = IapTransport.CreateSshTransport(
                instance,
                await auth))
            {
                var serviceProvider = CreateServiceProvider(await auth);
                var factory = serviceProvider.GetService<SessionFactory>();

                await AssertRaisesEventAsync<SessionAbortedEvent>(
                    () => factory.ConnectSshSession(
                        instance,
                        tunnel,
                        new SshParameters(),
                        new StaticPasswordCredential("user", string.Empty)))
                    .ConfigureAwait(true);

                Assert.That(this.ExceptionShown, Is.Not.Null);
                Assert.IsInstanceOf(typeof(UnsupportedAuthenticationMethodException), this.ExceptionShown);
            }
        }

        [Test]
        public async Task Connect_WhenWindowChangedToFloating(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Roles = new[] {
                PredefinedRole.IapTunnelUser,
                PredefinedRole.ComputeInstanceAdminV1
            })] ResourceTask<IAuthorization> auth)
        {
            var instance = await testInstance;

            using (var tunnel = IapTransport.CreateSshTransport(
                instance,
                await auth))
            {
                var serviceProvider = CreateServiceProvider(await auth);
                var factory = serviceProvider.GetService<SessionFactory>();

                var credential = await CrateSshCredential(
                    instance,
                    await auth,
                    SshKeyType.Rsa3072);

                SshView? session = null;
                await AssertRaisesEventAsync<SessionStartedEvent>(
                    () => session = (SshView)factory.ConnectSshSession(
                        instance,
                        tunnel,
                        new SshParameters(),
                        credential))
                    .ConfigureAwait(true);

                Assert.That(session, Is.Not.Null);
                Assert.That(this.ExceptionShown, Is.Null);

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
