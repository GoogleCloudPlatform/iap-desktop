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

using Google.Apis.Http;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using System.Diagnostics;
using System.Net.Http;

namespace Google.Solutions.Apis.Client
{
    /// <summary>
    /// Client factory that enables client certificate authenticateion
    /// if the device is enrolled.
    /// </summary>
    internal class MtlsAwareHttpClientFactory : HttpClientFactory
    {
        private readonly ServiceEndpointDetails endpointDetails;
        private readonly IDeviceEnrollment deviceEnrollment;

        public MtlsAwareHttpClientFactory(
            ServiceEndpointDetails endpointDetails,
            IDeviceEnrollment deviceEnrollment)
        {
            this.endpointDetails = endpointDetails.ExpectNotNull(nameof(endpointDetails));
            this.deviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
        }

        protected override HttpClientHandler CreateClientHandler()
        {
            var handler = base.CreateClientHandler();

            if (this.endpointDetails.UseClientCertificate &&
                HttpClientHandlerExtensions.CanUseClientCertificates)
            {
                Debug.Assert(deviceEnrollment.State == DeviceEnrollmentState.Enrolled);
                Debug.Assert(this.deviceEnrollment.Certificate != null);

                ApiTraceSources.Default.TraceInformation("Enabling MTLS");

                var added = handler.TryAddClientCertificate(this.deviceEnrollment.Certificate);
                Debug.Assert(added);
            }
                
            return handler;
        }
    }
}
