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
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Net;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Test
{
    [TestFixture]
    [UsesCloudResources]
    public class TestIapClient
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static Mock<IAuthorization> CreateAuthorization(DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);
            enrollment.Setup(e => e.Certificate).Returns(new X509Certificate2());

            var session = new Mock<IOidcSession>();
            session.SetupGet(s => s.ApiCredential).Returns(new Mock<ICredential>().Object);

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);
            authorization.SetupGet(a => a.Session).Returns(session.Object);

            return authorization;
        }

        //---------------------------------------------------------------------
        // PSC.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPscEnabled_ThenProbeSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> vm,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var address = await Dns
                .GetHostAddressesAsync(IapClient.CreateEndpoint().CanonicalUri.Host)
                .ConfigureAwait(false);

            //
            // Use IP address as pseudo-PSC endpoint.
            //
            var endpoint = IapClient.CreateEndpoint(
                new ServiceRoute(address.FirstOrDefault().ToString()));

            var client = new IapClient(
                endpoint,
                await auth,
                TestProject.UserAgent);

            WebSocket.RegisterPrefixes();
            SystemPatch.SetUsernameAsHostHeaderForWssRequests.Install();

            await client.GetTarget(
                    await vm,
                    22,
                    IapClient.DefaultNetworkInterface)
                .ProbeAsync(TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);

            SystemPatch.SetUsernameAsHostHeaderForWssRequests.Uninstall();
        }

        //---------------------------------------------------------------------
        // TLS.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMtlsDisabled_ThenTargetDoesNotUseCertificate(
            [Values(DeviceEnrollmentState.Disabled, DeviceEnrollmentState.NotEnrolled)]
            DeviceEnrollmentState state)
        {
            var authorization = CreateAuthorization(state);

            var client = new IapClient(
                IapClient.CreateEndpoint(),
                authorization.Object,
                TestProject.UserAgent);

            var target = client.GetTarget(
                SampleLocator,
                22,
                IapClient.DefaultNetworkInterface);

            Assert.That(target.IsMutualTlsEnabled, Is.False);
            Assert.IsNull(target.ClientCertificate);
        }

        //---------------------------------------------------------------------
        // mTLS.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMtlsEnabled_ThenTargetUsesCertificate()
        {
            var authorization = CreateAuthorization(DeviceEnrollmentState.Enrolled);

            var client = new IapClient(
                IapClient.CreateEndpoint(),
                authorization.Object,
                TestProject.UserAgent);

            var target = client.GetTarget(
                SampleLocator,
                22,
                IapClient.DefaultNetworkInterface);

            Assert.That(target.IsMutualTlsEnabled, Is.True);
            Assert.That(target.ClientCertificate, Is.Not.Null);
        }
    }
}
