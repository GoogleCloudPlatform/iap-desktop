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
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

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
        public void WhenPscEnabled_ThenGetTargetThrowsException()
        {
            var endpoint = IapClient.CreateEndpoint();

            //
            // Use IP address as pseudo-PSC endpoint.
            //
            endpoint.PscHostOverride = "psc.example.org";

            var client = new IapClient(
                endpoint,
                new Mock<IAuthorization>().Object,
                TestProject.UserAgent);

            Assert.Throws<ArgumentException>(
                () => client.GetTarget(
                    SampleLocator, 
                    22, 
                    IapClient.DefaultNetworkInterface));
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
