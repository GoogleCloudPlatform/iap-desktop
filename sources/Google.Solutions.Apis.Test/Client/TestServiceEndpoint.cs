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
        private class SampleAdapter : IServiceClient
        {
            public SampleAdapter(IServiceEndpoint endpoint)
            {
                this.Endpoint = endpoint;
            }

            public IServiceEndpoint Endpoint { get; }
        }

        //---------------------------------------------------------------------
        // GetEffectiveUri - mTLS.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMtlsDisabled_ThenSelectEndpointReturnsTlsEndpoint(
            [Values(DeviceEnrollmentState.Disabled, DeviceEnrollmentState.NotEnrolled)]
            DeviceEnrollmentState state)
        {
            var endpoint = new ServiceEndpoint<SampleAdapter>(
                new Uri("https://sample.Googleapis.COM/compute"));

            var effectiveUri = endpoint.GetEffectiveUri(
                state, 
                out var endpointType);

            Assert.AreEqual(new Uri("https://sample.googleapis.com/compute"), effectiveUri);
            Assert.AreEqual(ServiceEndpointType.Tls, endpointType);
        }

        [Test]
        public void WhenMtlsEnabled_ThenSelectEndpointReturnsMlsEndpoint()
        {
            var endpoint = new ServiceEndpoint<SampleAdapter>(
                new Uri("https://sample.Googleapis.COM/compute"));

            var effectiveUri = endpoint.GetEffectiveUri(
                DeviceEnrollmentState.Enrolled, 
                out var endpointType);

            Assert.AreEqual(new Uri("https://sample.mtls.googleapis.com/compute"), effectiveUri);
            Assert.AreEqual(ServiceEndpointType.MutualTls, endpointType);
        }

        //---------------------------------------------------------------------
        // GetEffectiveUri - PSC.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPscEndpointFound_ThenSelectEndpointReturnsPscEndpoint(
            [Values(DeviceEnrollmentState.Disabled, DeviceEnrollmentState.Enrolled)]
            DeviceEnrollmentState state)
        {
            var endpoint = new ServiceEndpoint<SampleAdapter>(
                new Uri("https://sample.Googleapis.COM/compute"))
            {
                PscEndpointOverride = "sample.p.Googleapis.COM"
            };

            var effectiveUri = endpoint.GetEffectiveUri(
                state,
                out var endpointType);

            Assert.AreEqual(new Uri("https://sample.p.Googleapis.COM/compute"), effectiveUri);
            Assert.AreEqual(ServiceEndpointType.PrivateServiceConnect, endpointType);
        }

        //---------------------------------------------------------------------
        // ToString
        //---------------------------------------------------------------------

        [Test]
        public void ToStringContainsUri()
        {
            var endpoint = new ServiceEndpoint<SampleAdapter>(
                new Uri("https://sample.googleapis.com/compute"));

            StringAssert.Contains("https://sample.googleapis.com/compute", endpoint.ToString());
        }
    }
}
