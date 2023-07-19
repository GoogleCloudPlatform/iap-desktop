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
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Iap
{
    /// <summary>
    /// API client for IAP TCP-forwarding.
    /// </summary>
    public interface IIapClient : IClient
    {
        /// <summary>
        /// Returns the IAP endpoint for a VM instance.
        /// </summary>
        IapInstanceEndpoint GetTarget(
            InstanceLocator vmInstance,
            ushort port,
            string nic);
    }

    public class IapClient : IIapClient
    {
        private readonly IAuthorization authorization;
        private readonly UserAgent userAgent;

        public IapClient(
            ServiceEndpoint<IapClient> endpoint,
            IAuthorization authorization,
            UserAgent userAgent)
        {
            this.Endpoint = endpoint.ExpectNotNull(nameof(endpoint));
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));
        }

        public static ServiceEndpoint<IapClient> CreateEndpoint()
        {
            return new ServiceEndpoint<IapClient>(
                new Uri("wss://tunnel.cloudproxy.app/v4/"),
                new Uri("wss://mtls.tunnel.cloudproxy.app/v4/"));
        }

        //---------------------------------------------------------------------
        // IClient.
        //---------------------------------------------------------------------

        public IServiceEndpoint Endpoint { get; }

        //---------------------------------------------------------------------
        // IIapClient.
        //---------------------------------------------------------------------

        public IapInstanceEndpoint GetTarget(
            InstanceLocator instance, 
            ushort port, 
            string nic)
        {
            instance.ExpectNotNull(nameof(instance));

            var baseUri = this.Endpoint.GetEffectiveUri(
                this.authorization.DeviceEnrollment?.State ?? DeviceEnrollmentState.NotEnrolled,
                out var endpointType);

            X509Certificate2 clientCertificate = null;
            if (endpointType == ServiceEndpointType.MutualTls &&
                this.authorization.DeviceEnrollment.State == DeviceEnrollmentState.Enrolled &&
                this.authorization.DeviceEnrollment.Certificate != null)
            {
                //
                // Device is enrolled and we have a device certificate -> enable DCA.
                //
                clientCertificate = this.authorization.DeviceEnrollment.Certificate;
            }

            if (clientCertificate != null)
            {
                IapTraceSources.Default.TraceInformation(
                    "Using client certificate (valid till {0})", clientCertificate.NotAfter);
            }

            return new IapInstanceEndpoint(
                baseUri,
                this.authorization.Credential,
                instance,
                port,
                nic,
                this.userAgent,
                clientCertificate);
        }
    }
}
