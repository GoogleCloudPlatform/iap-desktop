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
using Google.Solutions.Common.Util;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    public class TestApiClientBase
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

        /// <summary>
        /// Check if device certificate authentication is enabled.
        /// </summary>
        private static bool IsDeviceCertificateAuthenticationEnabled(IClientService service)
        {
            Precondition.ExpectNotNull(service, nameof(service));

            var dcaEnabled = IsDcaEnabledForHandler(service.HttpClient.MessageHandler);
            Debug.Assert(dcaEnabled == service.BaseUri.Contains(".mtls."));

            return dcaEnabled;

            bool IsDcaEnabledForHandler(HttpMessageHandler handler)
            {
                if (handler is DelegatingHandler delegatingHandler)
                {
                    return IsDcaEnabledForHandler(delegatingHandler.InnerHandler);
                }
                else if (handler is WebRequestHandler httpHandler)
                {
                    return httpHandler.ClientCertificates.Count > 0;
                }
                else
                {
                    return false;
                }
            }
        }

        private const string SampleEndpoint = "https://sample.googleapis.com/";

        private static Mock<IAuthorization> CreateAuthorization(
            DeviceEnrollmentState state,
            X509Certificate2? certificate)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);
            enrollment.SetupGet(e => e.Certificate).Returns(certificate);

            var session = new Mock<IOidcSession>();
            session.SetupGet(s => s.ApiCredential).Returns(new Mock<ICredential>().Object);

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);
            authorization.SetupGet(a => a.Session).Returns(session.Object);

            return authorization;
        }

        //---------------------------------------------------------------------
        // Initializer.
        //---------------------------------------------------------------------

        [Test]
        public void Initializer_WhenNotEnrolled_ThenClientUsesTls(
            [Values(
                DeviceEnrollmentState.NotEnrolled,
                DeviceEnrollmentState.Disabled)] DeviceEnrollmentState state)
        {
            var authorization = CreateAuthorization(state, null);

            var endpoint = new ServiceEndpoint<SampleClient>(
                ServiceRoute.Public,
                SampleEndpoint);

            var client = new SampleClient(
                endpoint,
                authorization.Object,
                TestProject.UserAgent);

            Assert.That(client.Initializer.BaseUri, Is.EqualTo(SampleEndpoint));
            Assert.That(IsDeviceCertificateAuthenticationEnabled(client.Service), Is.False);
        }

        [Test]
        public void Initializer_WhenEnrolled_ThenCreateServiceInitializerUsesTlsUsesMtls()
        {
            var endpoint = new ServiceEndpoint<SampleClient>(
                ServiceRoute.Public,
                SampleEndpoint);

            var authorization = CreateAuthorization(
                DeviceEnrollmentState.Enrolled,
                new X509Certificate2());

            var client = new SampleClient(
                endpoint,
                authorization.Object,
                TestProject.UserAgent);

            Assert.That(client.Initializer.BaseUri, Is.EqualTo("https://sample.mtls.googleapis.com/"));
            Assert.IsTrue(IsDeviceCertificateAuthenticationEnabled(client.Service));
        }

        [Test]
        public void Initializer_WhenPscOverrideFound_ThenCreateServiceInitializerUsesPsc()
        {
            var authorization = CreateAuthorization(DeviceEnrollmentState.Disabled, null);

            var endpoint = new ServiceEndpoint<SampleClient>(
                new ServiceRoute("crm.googleapis.com"),
                SampleEndpoint);

            var client = new SampleClient(
                endpoint,
                authorization.Object,
                TestProject.UserAgent);

            Assert.That(client.Initializer.BaseUri, Is.EqualTo("https://crm.googleapis.com/"));
            Assert.That(IsDeviceCertificateAuthenticationEnabled(client.Service), Is.False);
        }
    }
}
