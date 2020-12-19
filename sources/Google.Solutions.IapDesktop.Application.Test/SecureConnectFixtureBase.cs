
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Moq;

namespace Google.Solutions.IapDesktop.Application.Test
{
    public class SecureConnectFixtureBase : ApplicationFixtureBase
    {
        protected static IAuthorizationAdapter CreateAuthorizationAdapterForSecureConnectUser()
        {
            var authz = new Mock<IAuthorization>();
            authz.SetupGet(a => a.Credential).Returns(TestProject.GetSecureConnectCredential());

            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State)
                .Returns(DeviceEnrollmentState.Enrolled);
            enrollment.SetupGet(e => e.Certificate)
                .Returns(TestProject.GetDeviceCertificate());

            var adapter = new Mock<IAuthorizationAdapter>();
            adapter.SetupGet(a => a.Authorization).Returns(authz.Object);
            adapter.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);

            return adapter.Object;
        }
    }
}
