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
using Google.Apis.Compute.v1;
using Google.Apis.Services;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    public class TestPscAndMtlsAwareHttpClientFactory
    {
        private class SampleClient : ApiClientBase
        {
            public IClientService Service { get; }

            public SampleClient(
                IServiceEndpoint endpoint,
                IAuthorization authorization,
                UserAgent userAgent)
                : base(endpoint, authorization, userAgent)
            {
                this.Service = new ComputeService(this.Initializer);
            }
        }

        private const string SampleEndpoint = "https://sample.googleapis.com/";

        private Mock<IAuthorization> CreateAuthorization(DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);
            enrollment.SetupGet(e => e.Certificate).Returns(new X509Certificate2());

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
        public void WhenPscDisabled_ThenProxyIsEnabled()
        {
            var endpoint = new ServiceEndpoint<SampleClient>(
                ServiceRoute.Public,
                SampleEndpoint);
            var directions = endpoint.GetDirections(DeviceEnrollmentState.NotEnrolled);

            var factory = new PscAndMtlsAwareHttpClientFactory(
                directions,
                CreateAuthorization(DeviceEnrollmentState.NotEnrolled).Object,
                TestProject.UserAgent);

            var handler = (HttpClientHandler)factory
                .CreateHttpClient(new Google.Apis.Http.CreateHttpClientArgs())
                .GetInnerHandler();

            Assert.IsTrue(handler.UseProxy);
        }

        [Test]
        public void WhenPscEnabled_ThenProxyIsBypassed()
        {
            var endpoint = new ServiceEndpoint<SampleClient>(
                new ServiceRoute("psc-endpoint"),
                SampleEndpoint);
            var directions = endpoint.GetDirections(DeviceEnrollmentState.NotEnrolled);

            var factory = new PscAndMtlsAwareHttpClientFactory(
                directions,
                CreateAuthorization(DeviceEnrollmentState.NotEnrolled).Object,
                TestProject.UserAgent);

            var handler = (HttpClientHandler)factory
                .CreateHttpClient(new Google.Apis.Http.CreateHttpClientArgs())
                .GetInnerHandler();

            Assert.IsFalse(handler.UseProxy);
        }

        //---------------------------------------------------------------------
        // mTLS.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotEnrolled_ThenClientDoesNotUseCertificate()
        {
            var endpoint = new ServiceEndpoint<SampleClient>(
                ServiceRoute.Public,
                SampleEndpoint);
            var directions = endpoint.GetDirections(DeviceEnrollmentState.NotEnrolled);

            var factory = new PscAndMtlsAwareHttpClientFactory(
                directions,
                CreateAuthorization(DeviceEnrollmentState.Enrolled).Object,
                TestProject.UserAgent);

            var handler = (HttpClientHandler)factory
                .CreateHttpClient(new Google.Apis.Http.CreateHttpClientArgs())
                .GetInnerHandler();

            CollectionAssert.IsEmpty(handler.GetClientCertificates());
        }

        [Test]
        public void WhenEnrolled_ThenClientUsesCertificate()
        {
            var endpoint = new ServiceEndpoint<SampleClient>(
                ServiceRoute.Public,
                SampleEndpoint);
            var directions = endpoint.GetDirections(DeviceEnrollmentState.Enrolled);

            var factory = new PscAndMtlsAwareHttpClientFactory(
                directions,
                CreateAuthorization(DeviceEnrollmentState.Enrolled).Object,
                TestProject.UserAgent);

            var handler = (HttpClientHandler)factory
                .CreateHttpClient(new Google.Apis.Http.CreateHttpClientArgs())
                .GetInnerHandler();

            CollectionAssert.IsNotEmpty(handler.GetClientCertificates());
        }
    }
}
