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
    public class TestRemoteDesktopSessionBroker : WindowTestFixtureBase
    {
        // Use a larger machine type as all this RDP'ing consumes a fair
        // amount of memory.
        private const string MachineTypeForRdp = "n1-highmem-2";

        //---------------------------------------------------------------------
        // TryActivate
        //---------------------------------------------------------------------


        [Test]
        public void WhenNotConnected_ThenTryActivateReturnsFalse()
        {
            var sampleLocator = new InstanceLocator("project", "zone", "instance");
            var broker = new RemoteDesktopSessionBroker(this.serviceProvider);
            Assert.IsFalse(broker.TryActivate(sampleLocator));
        }

        [Test]
        public async Task WhenConnected_ThenGetActivePaneReturnsPane(
            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = IapTunnel.ForRdp(
                locator,
                await credential))
            using (var credentialAdapter = new WindowsCredentialAdapter(
                new ComputeEngineAdapter(this.serviceProvider.GetService<IAuthorizationAdapter>())))
            {
                var credentials = await credentialAdapter.CreateWindowsCredentialsAsync(
                        locator,
                        CreateRandomUsername(),
                        UserFlags.AddToAdministrators,
                        TimeSpan.FromSeconds(60),
                        CancellationToken.None)
                    .ConfigureAwait(true);

                var settings = InstanceConnectionSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.RdpUsername.StringValue = credentials.UserName;
                settings.RdpPassword.Value = credentials.SecurePassword;

                // Connect
                var broker = new RemoteDesktopSessionBroker(this.serviceProvider);
                IRemoteDesktopSession session = null;
                AssertRaisesEvent<SessionStartedEvent>(
                    () => session = (RemoteDesktopPane)broker.Connect(
                        locator,
                        "localhost",
                        (ushort)tunnel.LocalPort,
                        settings));

                Assert.IsNull(this.ExceptionShown);

                Assert.AreSame(session, RemoteDesktopPane.TryGetActivePane(this.mainForm));
                Assert.AreSame(session, RemoteDesktopPane.TryGetExistingPane(this.mainForm, locator));
                Assert.IsTrue(broker.IsConnected(locator));
                Assert.IsTrue(broker.TryActivate(locator));

                AssertRaisesEvent<SessionEndedEvent>(
                    () => session.Close());
            }
        }

        //---------------------------------------------------------------------
        // IsConnected.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenIsConnectedIsFalse()
        {
            var sampleLocator = new InstanceLocator("project", "zone", "instance");
            var broker = new RemoteDesktopSessionBroker(this.serviceProvider);
            Assert.IsFalse(broker.IsConnected(sampleLocator));
        }

        //---------------------------------------------------------------------
        // ActiveSession.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenActiveSessionReturnsNull()
        {
            var broker = new RemoteDesktopSessionBroker(this.serviceProvider);
            Assert.IsNull(broker.ActiveSession);
        }
    }
}
