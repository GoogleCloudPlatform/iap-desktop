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
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestResourceManagerAdapter : FixtureBase
    {
        [Test]
        public async Task WhenUserInRole_ThenIsGrantedPermissionReturnsTrue(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            using (var adapter = new ResourceManagerAdapter(await credential))
            {
                var result = await adapter.IsGrantedPermission(
                    TestProject.ProjectId,
                    Permissions.ComputeInstancesGet);

                Assert.IsTrue(result);
            }
        }

        [Test]
        public async Task WhenUserNotInRole_ThenIsGrantedPermissionReturnsFalse(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            using (var adapter = new ResourceManagerAdapter(await credential))
            {
                var result = await adapter.IsGrantedPermission(
                    TestProject.ProjectId,
                    "compute.disks.create");

                Assert.IsFalse(result);
            }
        }

        [Test]
        public async Task WhenProjectIdExists_ThenQueryProjectsByIdReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            using (var adapter = new ResourceManagerAdapter(await credential))
            {
                var project = await adapter.QueryProjectsById(
                    TestProject.ProjectId,
                    CancellationToken.None);

                Assert.IsNotNull(project);
                Assert.AreEqual(1, project.Count());
                Assert.AreEqual(TestProject.ProjectId, project.First().ProjectId);
            }
        }

        [Test]
        public async Task WhenProjectIdExists_ThenQueryProjectsByPrefixReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            using (var adapter = new ResourceManagerAdapter(await credential))
            {
                // Remove last character from project ID.
                var prefix = TestProject.ProjectId.Substring(0, TestProject.ProjectId.Length - 1);

                var project = await adapter.QueryProjectsByPrefix(
                    prefix,
                    CancellationToken.None);

                Assert.IsNotNull(project);
                Assert.IsTrue(project.Any());
                CollectionAssert.Contains(
                    project.Select(p => p.ProjectId),
                    TestProject.ProjectId);
            }
        }
    }
}
