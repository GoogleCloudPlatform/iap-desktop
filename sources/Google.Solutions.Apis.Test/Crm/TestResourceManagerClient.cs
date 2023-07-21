﻿//
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
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Crm
{
    [TestFixture]
    [UsesCloudResources]
    public class TestResourceManagerClient
    {
        //---------------------------------------------------------------------
        // PSC.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPscEnabled_ThenRequestSucceeds(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var address = await Dns
                .GetHostAddressesAsync(ResourceManagerClient.CreateEndpoint().CanonicalUri.Host)
                .ConfigureAwait(false);

            //
            // Use IP address as pseudo-PSC endpoint.
            //
            var endpoint = ResourceManagerClient.CreateEndpoint(
                new PrivateServiceConnectDirections(address.FirstOrDefault().ToString()));

            var client = new ResourceManagerClient(
                endpoint,
                await credential.ToAuthorization(),
                TestProject.UserAgent);

            var project = await client.GetProjectAsync(
                    TestProject.ProjectId,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(project);
            Assert.AreEqual(TestProject.ProjectId, project.Name);
        }

        [Test]
        public async Task WhenUserInRole_ThenIsGrantedPermissionReturnsTrue(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await credential.ToAuthorization(),
                TestProject.UserAgent);
            var result = await client.IsGrantedPermissionAsync(
                    TestProject.ProjectId,
                    Permissions.ComputeInstancesGet,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(result);
        }

        //---------------------------------------------------------------------
        // IsGrantedPermission.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserNotInRole_ThenIsGrantedPermissionReturnsFalse(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await credential.ToAuthorization(),
                TestProject.UserAgent);
            var result = await client.IsGrantedPermissionAsync(
                    TestProject.ProjectId,
                    "compute.disks.create",
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(result);
        }

        //---------------------------------------------------------------------
        // GetProject.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUserInViewerRole_ThenGetProjectReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await credential.ToAuthorization(),
                TestProject.UserAgent);
            var project = await client.GetProjectAsync(
                    TestProject.ProjectId,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(project);
            Assert.AreEqual(TestProject.ProjectId, project.ProjectId);
        }

        [Test]
        public async Task WhenUserNotInRole_ThenGetProjectThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await credential.ToAuthorization(),
                TestProject.UserAgent);
            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.GetProjectAsync(
                    TestProject.ProjectId,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenProjectIdInvalid_ThenGetProjectThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await credential.ToAuthorization(),
                TestProject.UserAgent);
            ExceptionAssert.ThrowsAggregateException<ResourceAccessDeniedException>(
                () => client.GetProjectAsync(
                    "invalid",
                    CancellationToken.None).Wait());
        }

        //---------------------------------------------------------------------
        // ListProjectsAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectIdExists_ThenQueryProjectsByIdReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await credential.ToAuthorization(),
                TestProject.UserAgent);
            var result = await client.ListProjectsAsync(
                    ProjectFilter.ByProjectId(TestProject.ProjectId),
                    null,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsTruncated);
            Assert.AreEqual(1, result.Projects.Count());
            Assert.AreEqual(TestProject.ProjectId, result.Projects.First().ProjectId);
        }

        [Test]
        public async Task WhenProjectIdExists_ThenQueryProjectsByPrefixReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await credential.ToAuthorization(),
                TestProject.UserAgent);

            // Remove last character from project ID.
            var prefix = TestProject.ProjectId.Substring(0, TestProject.ProjectId.Length - 1);

            var result = await client.ListProjectsAsync(
                    ProjectFilter.ByPrefix(prefix),
                    10,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Projects.Any());
            CollectionAssert.Contains(
                result.Projects.Select(p => p.ProjectId),
                TestProject.ProjectId);
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
