﻿//
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
using Google.Apis.Services;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Services.Authorization;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public class AuthorizedClientInitializer : BaseClientService.Initializer
    {
        public AuthorizedClientInitializer(
            IAuthorization authorization,
            string mtlsBaseUrl)
            : this(authorization.Credential, authorization.DeviceEnrollment, mtlsBaseUrl)
        { }

        public AuthorizedClientInitializer(
            ICredential credential,
            IDeviceEnrollment deviceEnrollment,
            string mtlsBaseUrl)
        {
            Precondition.ExpectNotNull(credential, nameof(credential));
            Precondition.ExpectNotNull(deviceEnrollment, nameof(deviceEnrollment));
            Precondition.ExpectNotNull(mtlsBaseUrl, nameof(mtlsBaseUrl));

            this.HttpClientInitializer = credential;
            this.ApplicationName = Install.UserAgent.ToApplicationName();

            if (deviceEnrollment.State == DeviceEnrollmentState.Enrolled &&
                deviceEnrollment.Certificate != null)
            {
                //
                // Device is enrolled and we have a device certificate -> enable DCA.
                //
                ClientServiceMtlsExtensions.EnableDeviceCertificateAuthentication(
                    this,
                    mtlsBaseUrl,
                    deviceEnrollment.Certificate);
            }
        }
    }
}
