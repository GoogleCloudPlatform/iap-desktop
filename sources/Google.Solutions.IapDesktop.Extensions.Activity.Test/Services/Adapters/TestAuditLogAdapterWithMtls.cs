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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Testing.Common.Integration;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using NUnit.Framework;
using System.Threading.Tasks;
using Google.Solutions.Testing.Application.Mocks;
using Google.Solutions.Testing.Application.Test;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.Adapters
{
    [TestFixture]
    [UsesCloudResources]
    public class TestAuditLogAdapterWithMtls : ApplicationFixtureBase
    {
        [Test]
        public async Task WhenNoEnrollmentProvided_ThenDeviceCertiticateAuthenticationIsOff(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var adapter = new AuditLogAdapter(await credential);
            Assert.IsFalse(adapter.IsDeviceCertiticateAuthenticationEnabled);
        }

        [Test]
        public void WhenEnrollmentProvided_ThenDeviceCertiticateAuthenticationIsOn()
        {
            var adapter = new AuditLogAdapter(AuthorizationSourceMocks.ForSecureConnectUser());
            Assert.IsTrue(adapter.IsDeviceCertiticateAuthenticationEnabled);
        }
    }
}