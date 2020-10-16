//
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
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface IAuthorizationAdapter
    {
        IAuthorization Authorization { get; }
        IDeviceEnrollment DeviceEnrollment { get; }

        Task ReauthorizeAsync(CancellationToken token);
    }

    public interface IDeviceEnrollment
    {
        DeviceEnrollmentState State { get; }
        X509Certificate2 Certificate { get; }
        Task RefreshAsync(string userId);
    }

    public enum DeviceEnrollmentState
    {
        NotInstalled,
        NotEnrolled,
        EnrolledWithoutCertificate,
        Enrolled
    }
}
