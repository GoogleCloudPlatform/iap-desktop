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
using Google.Solutions.Common.Test;
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
    public class TestResourceManagerAdapter : ApplicationFixtureBase
    {
        [Test]
        public async Task WhenUserInRole_ThenIsGrantedPermissionReturnsTrue(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            using (var adapter = new ResourceManagerAdapter(await credential))
            {
                var result = await adapter.IsGrantedPermissionAsync(
                    TestProject.ProjectId,
                    Permissions.ComputeInstancesGet,
                    CancellationToken.None);

                Assert.IsTrue(result);
            }
        }

        //---------------------------------------------------------------------
        // IsGrantedPermission.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenIsGrantedPermissionReturnsFalse(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            using (var adapter = new ResourceManagerAdapter(await credential))
            {
                var result = await adapter.IsGrantedPermissionAsync(
                    TestProject.ProjectId,
                    "compute.disks.create",
                    CancellationToken.None);

                Assert.IsFalse(result);
            }
        }

        //---------------------------------------------------------------------
        // GetProject.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInViewerRole_ThenGetProjectReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            using (var adapter = new ResourceManagerAdapter(await credential))
            {
                var project = await adapter.GetProjectAsync(
                    TestProject.ProjectId,
                    CancellationToken.None);

                Assert.IsNotNull(project);
                Assert.AreEqual(TestProject.ProjectId, project.ProjectId);
            }
        }

        [Test]
        public async Task WhenUserNotInRole_ThenGetProjectThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            using (var adapter = new ResourceManagerAdapter(await credential))
            {
                AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                    () => adapter.GetProjectAsync(
                        TestProject.ProjectId,
                        CancellationToken.None).Wait());
            }
        }

        [Test]
        public async Task WhenProjectIdInvalid_ThenGetProjectThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            using (var adapter = new ResourceManagerAdapter(await credential))
            {
                AssertEx.ThrowsAggregateException<ResourceAccessDeniedException>(
                    () => adapter.GetProjectAsync(
                        "invalid",
                        CancellationToken.None).Wait());
            }
        }

        //---------------------------------------------------------------------
        // ListProjectsAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectIdExists_ThenQueryProjectsByIdReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            using (var adapter = new ResourceManagerAdapter(await credential))
            {
                var result = await adapter.ListProjectsAsync(
                    ProjectFilter.ByProjectId(TestProject.ProjectId),
                    null,
                    CancellationToken.None);

                Assert.IsNotNull(result);
                Assert.IsFalse(result.IsTruncated);
                Assert.AreEqual(1, result.Projects.Count());
                Assert.AreEqual(TestProject.ProjectId, result.Projects.First().ProjectId);
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

                var result = await adapter.ListProjectsAsync(
                    ProjectFilter.ByPrefix(prefix),
                    10,
                    CancellationToken.None);

                Assert.IsNotNull(result);
                Assert.IsTrue(result.Projects.Any());
                CollectionAssert.Contains(
                    result.Projects.Select(p => p.ProjectId),
                    TestProject.ProjectId);
            }
        }

        //---------------------------------------------------------------------
        // ProjectFilter.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTermContainsSpecialCharacters_ThenByProjectIdIgnoresThem()
        {
            Assert.AreEqual(
                "id:\"foo-bar\"",
                ProjectFilter.ByProjectId("foo:'\"-bar").ToString());
        }

        [Test]
        public void WhenTermContainsSpecialCharacters_ThenByPrefixIgnoresThem()
        {
            Assert.AreEqual(
                "name:\"foo-bar*\" OR id:\"foo-bar*\"",
                ProjectFilter.ByPrefix("foo:'\"-bar").ToString());
        }
    }
}
