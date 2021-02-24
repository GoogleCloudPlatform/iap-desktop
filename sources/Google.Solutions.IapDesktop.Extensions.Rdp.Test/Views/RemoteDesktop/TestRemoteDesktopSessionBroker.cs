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
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.RemoteDesktop;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Views.RemoteDesktop
{
    [TestFixture]
    public class TestRemoteDesktopSessionBroker : WindowTestFixtureBase
    {
        // Use a larger machine type as all this RDP'ing consumes a fair
        // amount of memory.
        private const string MachineTypeForRdp = "n1-highmem-2";

        private readonly InstanceLocator SampleLocator =
            new InstanceLocator("project", "zone", "instance");

        //---------------------------------------------------------------------
        // TryActivate
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenActiveSessionIsNullAndTryActivateReturnsFails()
        {
            var broker = new RemoteDesktopSessionBroker(this.serviceProvider);

            Assert.IsFalse(broker.IsConnected(SampleLocator));
            Assert.IsFalse(broker.TryActivate(SampleLocator));
        }

        [Test]
        public async Task WhenConnected_ThenActiveSessionIsSetAndTryActivateReturnsTrue(
            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var locator = await testInstance;

            using (var tunnel = RdpTunnel.Create(
                locator,
                await credential))
            using (var gceAdapter = new ComputeEngineAdapter(this.serviceProvider.GetService<IAuthorizationAdapter>()))
            {
                var credentials = await gceAdapter.ResetWindowsUserAsync(
                    locator,
                    CreateRandomUsername(),
                    TimeSpan.FromSeconds(60),
                    CancellationToken.None);

                var settings = VmInstanceConnectionSettings.CreateNew(
                    locator.ProjectId,
                    locator.Name);
                settings.Username.StringValue = credentials.UserName;
                settings.Password.Value = credentials.SecurePassword;

                // Connect
                var broker = new RemoteDesktopSessionBroker(this.serviceProvider);

                IRemoteDesktopSession session = null;
                AssertRaisesEvent<SessionStartedEvent>(
                    () => session = broker.Connect(
                        locator,
                        "localhost",
                        (ushort)tunnel.LocalPort,
                        settings));

                Assert.IsNull(this.ExceptionShown);

                // Verify session is connected.
                Assert.IsTrue(broker.IsConnected(locator));
                Assert.AreSame(session, broker.ActiveSession);
                Assert.IsTrue(broker.TryActivate(locator));

                // Verify dummy session is not connected.
                Assert.IsFalse(broker.TryActivate(SampleLocator));
                Assert.IsFalse(broker.IsConnected(SampleLocator));

                AssertRaisesEvent<SessionEndedEvent>(
                    () => session.Close());
            }
        }

        //---------------------------------------------------------------------
        // ActiveSession
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenActiveSessionReturnsNull()
        {
            var broker = new RemoteDesktopSessionBroker(this.serviceProvider);

            Assert.IsNull(broker.ActiveSession);
        }
    }
}
