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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
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
        public async Task GetProject_WhenPscEnabled_ThenRequestSucceeds(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var address = await Dns
                .GetHostAddressesAsync(ResourceManagerClient.CreateEndpoint().CanonicalUri.Host)
                .ConfigureAwait(false);

            //
            // Use IP address as pseudo-PSC endpoint.
            //
            var endpoint = ResourceManagerClient.CreateEndpoint(
                new ServiceRoute(address.First().ToString()));

            var client = new ResourceManagerClient(
                endpoint,
                await auth,
                TestProject.UserAgent);

            var project = await client
                .GetProjectAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(project);
            Assert.That(project.Name, Is.EqualTo(TestProject.ProjectId));
        }

        //---------------------------------------------------------------------
        // IsAccessGranted.
        //---------------------------------------------------------------------

        [Test]
        public async Task IsAccessGranted_WhenUserHasPermission_ThenIsAccessGrantedReturnsTrue(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);
            var result = await client
                .IsAccessGrantedAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    new[] { Permissions.ComputeInstancesGet },
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsAccessGranted_WhenUserLacksOnePermission_ThenIsAccessGrantedReturnsFalse(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);
            var result = await client
                .IsAccessGrantedAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    new[] { "compute.disks.create", Permissions.ComputeInstancesGet },
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(result, Is.False);
        }

        //---------------------------------------------------------------------
        // GetProject.
        //---------------------------------------------------------------------

        [Test]
        public async Task GetProject_WhenUserInViewerRole_ThenGetProjectReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);
            var project = await client
                .GetProjectAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(project);
            Assert.That(project.ProjectId, Is.EqualTo(TestProject.ProjectId));
        }

        [Test]
        public async Task GetProject_WhenUserNotInRole_ThenGetProjectThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.GetProjectAsync(
                    new ProjectLocator(TestProject.ProjectId),
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GetProject_WhenProjectIdInvalid_ThenGetProjectThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.GetProjectAsync(
                    new ProjectLocator("invalid"),
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // ListProjects.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListProjects_WhenProjectIdExists_ThenQueryProjectsByIdReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);
            var result = await client.ListProjectsAsync(
                    ProjectFilter.ByProjectId(TestProject.ProjectId),
                    null,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.That(result.IsTruncated, Is.False);
            Assert.That(result.Projects.Count(), Is.EqualTo(1));
            Assert.That(result.Projects.First().ProjectId, Is.EqualTo(TestProject.ProjectId));
        }

        [Test]
        public async Task ListProjects_WhenProjectIdExists_ThenQueryProjectsByPrefixReturnsProject(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            // Remove last character from project ID.
            var prefix = TestProject.ProjectId.Substring(0, TestProject.ProjectId.Length - 1);

            var result = await client.ListProjectsAsync(
                    ProjectFilter.ByTerm(prefix),
                    10,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.That(result.Projects.Any(), Is.True);
            Assert.That(
                result.Projects.Select(p => p.ProjectId), Has.Member(TestProject.ProjectId));
        }

        //---------------------------------------------------------------------
        // FindOrganization.
        //---------------------------------------------------------------------

        [Test]
        public async Task FindOrganization_WhenProjectIdInvalid_ThenFindOrganizationThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.ServiceUsageConsumer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.FindOrganizationAsync(
                    new ProjectLocator("invalid"),
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task FindOrganization_WhenUserInRole_ThenFindOrganizationReturnsId(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);
            var org = await client
                .FindOrganizationAsync(
                    TestProject.Project,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(org);
            Assert.That(org!.Id, Is.Not.EqualTo(0));
        }

        //---------------------------------------------------------------------
        // GetOrganization.
        //---------------------------------------------------------------------

        [Test]
        public async Task GetOrganization_WhenOrganizationIdInvalid_ThenGetOrganizationThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(() => client.GetOrganizationAsync(
                    new OrganizationLocator(0),
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GetOrganization_WhenUserNotInRole_ThenGetOrganizationThrowsResourceAccessDeniedException(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var client = new ResourceManagerClient(
                ResourceManagerClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            var org = await client
                .FindOrganizationAsync(
                    TestProject.Project,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(org);

            await ExceptionAssert
                .ThrowsAsync<ResourceAccessDeniedException>(
                    () => client.GetOrganizationAsync(org!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // ProjectFilter.
        //---------------------------------------------------------------------

        [Test]
        public void ProjectFilter_WhenTermContainsSpecialCharacters_ThenByProjectIdReturnsFilter()
        {
            Assert.That(
                ProjectFilter.ByProjectId("foo:'\"-bar").ToString(), Is.EqualTo("id:\"foo-bar\""));
        }

        [Test]
        public void ProjectFilter_WhenTermContainsSpecialCharacters_ThenByTermReturnsFilter()
        {
            Assert.That(
                ProjectFilter.ByTerm("foo:'\"-bar").ToString(), Is.EqualTo("name:\"*foo-bar*\" OR id:\"*foo-bar*\""));
        }

        [Test]
        public void ProjectFilter_WhenTermEmpty_ThenByTermReturnsFilter()
        {
            Assert.That(
                ProjectFilter.ByTerm(string.Empty).ToString(), Is.EqualTo("name:\"**\" OR id:\"**\""));
        }
    }
}
