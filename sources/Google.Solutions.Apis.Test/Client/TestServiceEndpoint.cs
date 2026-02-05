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
using NUnit.Framework;
using System;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    public class TestServiceEndpoint
    {
        private class SampleAdapter : IClient
        {
            public SampleAdapter(IServiceEndpoint endpoint)
            {
                this.Endpoint = endpoint;
            }

            public IServiceEndpoint Endpoint { get; }
        }

        //---------------------------------------------------------------------
        // GetDirections - TLS.
        //---------------------------------------------------------------------

        [Test]
        public void GetDirections_WhenMtlsDisabled_ThenReturnsTlsEndpoint(
            [Values(DeviceEnrollmentState.Disabled, DeviceEnrollmentState.NotEnrolled)]
            DeviceEnrollmentState state)
        {
            var endpoint = new ServiceEndpoint<SampleAdapter>(
                ServiceRoute.Public,
                new Uri("https://sample.Googleapis.COM/compute"));

            var details = endpoint.GetDirections(state);

            Assert.That(details.BaseUri, Is.EqualTo(new Uri("https://sample.googleapis.com/compute")));
            Assert.That(details.Type, Is.EqualTo(ServiceEndpointType.Tls));
            Assert.That(details.Host, Is.EqualTo("sample.googleapis.com"));
            Assert.That(details.UseClientCertificate, Is.False);
        }

        //---------------------------------------------------------------------
        // GetDirections - mTLS.
        //---------------------------------------------------------------------

        [Test]
        public void GetDirections__WhenMtlsEnabled_ThenReturnsMtlsEndpoint()
        {
            var endpoint = new ServiceEndpoint<SampleAdapter>(
                ServiceRoute.Public,
                "https://sample.Googleapis.COM/compute");

            var details = endpoint.GetDirections(DeviceEnrollmentState.Enrolled);

            Assert.That(details.BaseUri, Is.EqualTo(new Uri("https://sample.mtls.googleapis.com/compute")));
            Assert.That(details.Type, Is.EqualTo(ServiceEndpointType.MutualTls));
            Assert.That(details.Host, Is.EqualTo("sample.mtls.googleapis.com"));
            Assert.That(details.UseClientCertificate, Is.True);
        }

        [Test]
        public void GetDirections_WhenMtlsEnabledButMtlsEndpointisNull_ThenReturnsTlsEndpoint()
        {
            var endpoint = new ServiceEndpoint<SampleAdapter>(
                ServiceRoute.Public,
                new Uri("https://sample.Googleapis.COM/compute"),
                null);

            var details = endpoint.GetDirections(DeviceEnrollmentState.Enrolled);

            Assert.That(details.BaseUri, Is.EqualTo(new Uri("https://sample.Googleapis.COM/compute")));
            Assert.That(details.Type, Is.EqualTo(ServiceEndpointType.Tls));
            Assert.That(details.Host, Is.EqualTo("sample.googleapis.com"));
            Assert.That(details.UseClientCertificate, Is.False);
        }

        //---------------------------------------------------------------------
        // GetDirections - PSC.
        //---------------------------------------------------------------------

        [Test]
        public void GetDirections_WhenPscEnabled_ThenReturnsPscEndpoint(
            [Values(DeviceEnrollmentState.Disabled, DeviceEnrollmentState.Enrolled)]
            DeviceEnrollmentState state)
        {
            var endpoint = new ServiceEndpoint<SampleAdapter>(
                new ServiceRoute("sample.p.Googleapis.COM"),
                new Uri("https://sample.Googleapis.COM/compute"));

            var details = endpoint.GetDirections(state);

            Assert.That(details.BaseUri, Is.EqualTo(new Uri("https://sample.p.Googleapis.COM/compute")));
            Assert.That(details.Type, Is.EqualTo(ServiceEndpointType.PrivateServiceConnect));
            Assert.That(details.Host, Is.EqualTo("sample.googleapis.com"));
            Assert.That(details.UseClientCertificate, Is.False);
        }

        //---------------------------------------------------------------------
        // ToString
        //---------------------------------------------------------------------

        [Test]
        public void ToString_ContainsUri()
        {
            var endpoint = new ServiceEndpoint<SampleAdapter>(
                ServiceRoute.Public,
                new Uri("https://sample.googleapis.com/compute"));

            Assert.That(endpoint.ToString(), Does.Contain("https://sample.googleapis.com/compute"));
        }
    }
}
