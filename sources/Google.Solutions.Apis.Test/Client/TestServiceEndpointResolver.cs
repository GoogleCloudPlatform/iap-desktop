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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Moq;
using NUnit.Framework;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    public class TestServiceEndpointResolver
    {
        private static Mock<IAuthorization> CreateAuthorization(
            DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);

            if (state == DeviceEnrollmentState.Enrolled)
            {
                enrollment.SetupGet(e => e.Certificate).Returns(new X509Certificate2());
            }

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);

            return authorization;
        }

        //---------------------------------------------------------------------
        // ResolveEndpoint - mTLS.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMtlsDisabled_ThenSelectEndpointReturnsTlsEndpoint(
            [Values(DeviceEnrollmentState.Disabled, DeviceEnrollmentState.NotEnrolled)]
            DeviceEnrollmentState state)
        {
            var resovler = new ServiceEndpointResolver();
            var template = new ServiceDescription(
                new Uri("https://Compute.Googleapis.COM/compute"));

            var authorization = CreateAuthorization(state);
            var endpoint = resovler.ResolveEndpoint(authorization.Object, template);

            Assert.AreEqual(new Uri("https://compute.googleapis.com/compute"), endpoint.Uri);
            Assert.AreEqual(EndpointType.Tls, endpoint.Type);
        }

        [Test]
        public void WhenMtlsEnabled_ThenSelectEndpointReturnsMlsEndpoint()
        {
            var resovler = new ServiceEndpointResolver();
            var template = new ServiceDescription(
                new Uri("https://Compute.Googleapis.COM/compute"));

            var authorization = CreateAuthorization(DeviceEnrollmentState.Enrolled);
            var endpoint = resovler.ResolveEndpoint(authorization.Object, template);

            Assert.AreEqual(new Uri("https://compute.mtls.googleapis.com/compute"), endpoint.Uri);
            Assert.AreEqual(EndpointType.MutualTls, endpoint.Type);
        }

        //---------------------------------------------------------------------
        // ResolveEndpoint - PSC.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPscEndpointFound_ThenSelectEndpointReturnsPscEndpoint(
            [Values(DeviceEnrollmentState.Disabled, DeviceEnrollmentState.Enrolled)]
            DeviceEnrollmentState state)
        {
            var resovler = new ServiceEndpointResolver();
            resovler.AddPrivateServiceEndpoint(
                "Compute.Googleapis.COM",
                "Compute.p.Googleapis.COM");

            var template = new ServiceDescription(
                new Uri("https://Compute.Googleapis.COM/compute"));

            var authorization = CreateAuthorization(state);
            var endpoint = resovler.ResolveEndpoint(authorization.Object, template);

            Assert.AreEqual(new Uri("https://Compute.p.Googleapis.COM/compute"), endpoint.Uri);
            Assert.AreEqual(EndpointType.PrivateServiceConnect, endpoint.Type);
        }
    }
}
