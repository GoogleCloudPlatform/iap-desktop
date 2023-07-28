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
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using Google.Solutions.Apis.Locator;
using System;
using Moq;
using Google.Solutions.Apis.Auth;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Linq;
using Google.Solutions.Iap.Net;
using System.Net.WebSockets;
using Google.Solutions.Apis.Client;

namespace Google.Solutions.Iap.Test
{
    [TestFixture]
    [UsesCloudResources]
    public class TestIapClient
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // PSC.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPscEnabled_ThenProbeSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> vm,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var address = await Dns
                .GetHostAddressesAsync(IapClient.CreateEndpoint().CanonicalUri.Host)
                .ConfigureAwait(false);

            //
            // Use IP address as pseudo-PSC endpoint.
            //
            var endpoint = IapClient.CreateEndpoint(
                new PrivateServiceConnectDirections(address.FirstOrDefault().ToString()));

            var client = new IapClient(
                endpoint,
                (await credential).ToAuthorization(),
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
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.Setup(e => e.State).Returns(state);

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);

            var client = new IapClient(
                IapClient.CreateEndpoint(),
                authorization.Object,
                TestProject.UserAgent);

            var target = client.GetTarget(
                SampleLocator,
                22,
                IapClient.DefaultNetworkInterface);

            Assert.IsFalse(target.IsMutualTlsEnabled);
            Assert.IsNull(target.ClientCertificate);
        }

        //---------------------------------------------------------------------
        // mTLS.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMtlsEnabled_ThenTargetUsesCertificate()
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.Setup(e => e.State).Returns(DeviceEnrollmentState.Enrolled);
            enrollment.Setup(e => e.Certificate).Returns(new X509Certificate2());

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);

            var client = new IapClient(
                IapClient.CreateEndpoint(),
                authorization.Object,
                TestProject.UserAgent);

            var target = client.GetTarget(
                SampleLocator,
                22,
                IapClient.DefaultNetworkInterface);
            
            Assert.IsTrue(target.IsMutualTlsEnabled);
            Assert.IsNotNull(target.ClientCertificate);
        }
    }
}
