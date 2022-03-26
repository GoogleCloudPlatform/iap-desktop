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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.Ssh.Auth;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Adapter
{
    [TestFixture]
    public class TestOsLoginAdapter : ApplicationFixtureBase
    {
        private OsLoginAdapter CreateAdapter(string email)
        {
            var authz = new Mock<IAuthorization>();
            authz.SetupGet(a => a.Email).Returns(email);
            authz.SetupGet(a => a.Credential).Returns(TestProject.GetAdminCredential());

            var authzSource = new Mock<IAuthorizationSource>();
            authzSource.SetupGet(s => s.Authorization).Returns(authz.Object);

            return new OsLoginAdapter(authzSource.Object);
        }

        //---------------------------------------------------------------------
        // ImportSshPublicKeyAsync.
        //---------------------------------------------------------------------

        [Test]
        public void WhenEmailInvalid_ThenImportSshPublicKeyThrowsException()
        {
            var adapter = CreateAdapter("x@gmail.com");

            var key = new Mock<ISshKeyPair>();
            key.SetupGet(s => s.PublicKeyString).Returns("key");

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.ImportSshPublicKeyAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    key.Object,
                    TimeSpan.FromMinutes(1),
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // GetLoginProfileAsync.
        //---------------------------------------------------------------------

        [Test]
        public void WhenEmailInvalid_ThenGetLoginProfileThrowsException()
        {
            var adapter = CreateAdapter("x@gmail.com");

            var key = new Mock<ISshKeyPair>();
            key.SetupGet(s => s.PublicKeyString).Returns("key");

            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => adapter.GetLoginProfileAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None).Wait());
        }
    }
}
