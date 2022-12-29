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
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Download;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Views;
using Google.Solutions.Testing.Common.Integration;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.SshTerminal
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSshTerminalSessionBroker : WindowTestFixtureBase
    {
        [SetUp]
        public void SetUpServices()
        {
            this.ServiceRegistry.AddMock<IConfirmationDialog>();
            this.ServiceRegistry.AddMock<IOperationProgressDialog>();
            this.ServiceRegistry.AddMock<IDownloadFileDialog>();
            this.ServiceRegistry.AddMock<IQuarantineAdapter>();

            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            this.ServiceRegistry.AddSingleton(new TerminalSettingsRepository(
                hkcu.CreateSubKey(TestKeyPath)));
        }

        //---------------------------------------------------------------------
        // TryActivate
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenTryActivateReturnsFalse()
        {
            var sampleLocator = new InstanceLocator("project", "zone", "instance");
            var broker = new SshTerminalSessionBroker(this.ServiceProvider);
            Assert.IsFalse(broker.TryActivate(sampleLocator));
        }

        [Test]
        public async Task WhenConnected_ThenTryActivateReturnsTrue(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;
            var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072);

            var gceAdapter = new ComputeEngineAdapter(
                this.ServiceProvider.GetService<IAuthorizationSource>());
            var keyAdapter = new KeyAuthorizationService(
                this.ServiceProvider.GetService<IAuthorizationSource>(),
                new ComputeEngineAdapter(await credential),
                new ResourceManagerAdapter(await credential),
                new Mock<IOsLoginService>().Object);
            
            var authorizedKey = await keyAdapter.AuthorizeKeyAsync(
                    locator,
                    key,
                    TimeSpan.FromMinutes(10),
                    null,
                    KeyAuthorizationMethods.InstanceMetadata,
                    CancellationToken.None)
                .ConfigureAwait(true);

            var instance = await gceAdapter.GetInstanceAsync(
                    locator,
                    CancellationToken.None)
                .ConfigureAwait(true);

            // Connect
            var broker = new SshTerminalSessionBroker(this.ServiceProvider);

            ISshTerminalSession session = null;
            await AssertRaisesEventAsync<SessionStartedEvent>(
                async () => session = await broker.ConnectAsync(
                    locator,
                    new IPEndPoint(instance.PublicAddress(), 22),
                    authorizedKey,
                    null,
                    TimeSpan.FromSeconds(10))
                .ConfigureAwait(true))
                .ConfigureAwait(true);

            Assert.IsNull(this.ExceptionShown);

            Assert.AreSame(session, SshTerminalPane.TryGetActivePane(this.MainForm));
            Assert.AreSame(session, SshTerminalPane.TryGetExistingPane(this.MainForm, locator));
            Assert.IsTrue(broker.IsConnected(locator));
            Assert.IsTrue(broker.TryActivate(locator));

            await AssertRaisesEventAsync<SessionEndedEvent>(
                    () => session.Close())
                .ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // IsConnected.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenIsConnectedIsFalse()
        {
            var sampleLocator = new InstanceLocator("project", "zone", "instance");
            var broker = new SshTerminalSessionBroker(this.ServiceProvider);
            Assert.IsFalse(broker.IsConnected(sampleLocator));
        }

        //---------------------------------------------------------------------
        // ActiveSession.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenActiveSessionReturnsNull()
        {
            var broker = new SshTerminalSessionBroker(this.ServiceProvider);
            Assert.IsNull(broker.ActiveSshTerminalSession);
        }
    }
}
