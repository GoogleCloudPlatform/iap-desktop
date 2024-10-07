//
// Copyright 2024 Google LLC
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

using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Linq;
using Google.Solutions.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Testing.Apis;
using Google.Solutions.IapDesktop.Core.EntityModel;
using System.ComponentModel;
using Google.Solutions.IapDesktop.Core.ResourceModel;

using CrmProject = Google.Apis.CloudResourceManager.v1.Data.Project;
using CrmOrganization = Google.Apis.CloudResourceManager.v1.Data.Organization;

namespace Google.Solutions.IapDesktop.Core.Test.ResourceModel
{
    [TestFixture]
    public class TestProjectWorkspace
    {
        private static readonly ProjectLocator SampleProject = new ProjectLocator("project-1");

        //----------------------------------------------------------------------
        // Search organizations.
        //----------------------------------------------------------------------

        [Test]
        public async Task SearchOrganizations_WhenWorkspaceEmpty()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            var workspace = new ProjectWorkspace(
                settings.Object,
                new Mock<IResourceManagerClient>().Object);

            var organizations = await workspace
                .SearchAsync(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false);
            CollectionAssert.IsEmpty(organizations);
        }

        [Test]
        public async Task SearchOrganizations_WhenCalledTwice_ThenUsesCache()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            settings
                .SetupGet(s => s.Projects)
                .Returns(Enumerables.Scalar(SampleProject));

            var resourceMamager = new Mock<IResourceManagerClient>();
            resourceMamager
                .Setup(r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrmProject());

            var workspace = new ProjectWorkspace(
                settings.Object,
                resourceMamager.Object);

            // Search twice.
            await workspace
                .SearchAsync(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false);
            await workspace
                .SearchAsync(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false);

            // Check that cache loaded once.
            resourceMamager.Verify(
                r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task SearchOrganizations_WhenSettingsChange_ThenInvalidatesCache()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            settings
                .SetupGet(s => s.Projects)
                .Returns(Enumerables.Scalar(SampleProject));

            var resourceMamager = new Mock<IResourceManagerClient>();
            resourceMamager
                .Setup(r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrmProject());

            var workspace = new ProjectWorkspace(
                settings.Object,
                resourceMamager.Object);

            // Search to load cache.
            await workspace
                .SearchAsync(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false);

            // Invalidate.
            settings.Raise(
                s => s.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectWorkspaceSettings.Projects)));

            // Search again.
            await workspace
                .SearchAsync(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false);
            await workspace
                .SearchAsync(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false);

            // Check that cache loaded once.
            resourceMamager.Verify(
                r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task SearchOrganizations_WhenProjectAncestryUnknown()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            settings
                .SetupGet(s => s.Projects)
                .Returns(Enumerables.Scalar(SampleProject));

            // Fail calls to load ancestry.
            var resourceMamager = new Mock<IResourceManagerClient>();
            resourceMamager
                .Setup(r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrmProject());

            var workspace = new ProjectWorkspace(
                settings.Object,
                resourceMamager.Object);

            var organizations = await workspace
                .SearchAsync(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false);
            CollectionAssert.IsNotEmpty(organizations);
            Assert.AreEqual(
                Organization.Default.Locator,
                organizations.First().Locator);
        }

        [Test]
        public async Task SearchOrganizations_WhenProjectAncestryCached()
        {
            var org = new OrganizationLocator(1);

            var settings = new Mock<IProjectWorkspaceSettings>();
            settings
                .SetupGet(s => s.Projects)
                .Returns(Enumerables.Scalar(SampleProject));
            settings
                .Setup(s => s.TryGetCachedAncestry(SampleProject, out org))
                .Returns(true);

            var resourceMamager = new Mock<IResourceManagerClient>();
            resourceMamager
                .Setup(r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrmProject());
            resourceMamager
                .Setup(r => r.GetOrganizationAsync(org, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrmOrganization()
                {
                    DisplayName = "One"
                });

            var workspace = new ProjectWorkspace(
                settings.Object,
                resourceMamager.Object);

            var organizations = await workspace
                .SearchAsync(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false);
            CollectionAssert.IsNotEmpty(organizations);
            Assert.AreEqual(
                org,
                organizations.First().Locator);
            Assert.AreEqual(
                "One",
                organizations.First().DisplayName);

            resourceMamager
                .Verify(r => r.FindOrganizationAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task SearchOrganizations_WhenProjectAncestryInaccessible()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            settings
                .SetupGet(s => s.Projects)
                .Returns(Enumerables.Scalar(SampleProject));

            var resourceMamager = new Mock<IResourceManagerClient>();
            resourceMamager
                .Setup(r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrmProject());
            resourceMamager
                .Setup(r => r.FindOrganizationAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync((OrganizationLocator?)null);

            var workspace = new ProjectWorkspace(
                settings.Object,
                resourceMamager.Object);

            var organizations = await workspace
                .SearchAsync(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false);
            CollectionAssert.IsNotEmpty(organizations);
            Assert.AreEqual(
                Organization.Default.Locator,
                organizations.First().Locator);
        }

        [Test]
        public async Task SearchOrganizations_WhenProjectAncestryCachedButOrganizationInaccessible()
        {
            var org = new OrganizationLocator(1);

            var settings = new Mock<IProjectWorkspaceSettings>();
            settings
                .SetupGet(s => s.Projects)
                .Returns(Enumerables.Scalar(SampleProject));
            settings
                .Setup(s => s.TryGetCachedAncestry(SampleProject, out org))
                .Returns(true);

            var resourceMamager = new Mock<IResourceManagerClient>();
            resourceMamager
                .Setup(r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrmProject());
            resourceMamager
                .Setup(r => r.GetOrganizationAsync(org, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("mock", new Exception()));

            var workspace = new ProjectWorkspace(
                settings.Object,
                resourceMamager.Object);

            var organizations = await workspace
                .SearchAsync(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false);
            CollectionAssert.IsNotEmpty(organizations);
            Assert.AreEqual(
                Organization.Default.Locator,
                organizations.First().Locator);
        }

        //----------------------------------------------------------------------
        // Query organizations.
        //----------------------------------------------------------------------

        [Test]
        public async Task QueryOrganization_WhenNotFound()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            var workspace = new ProjectWorkspace(
                settings.Object,
                new Mock<IResourceManagerClient>().Object);

            var aspect = await workspace
                .QueryAspectAsync(Organization.Default.Locator, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(aspect);
        }

        //----------------------------------------------------------------------
        // Expand organizations.
        //----------------------------------------------------------------------

        [Test]
        public async Task ExpandOrganization_WhenProjectInaccessible()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            settings
                .SetupGet(s => s.Projects)
                .Returns(Enumerables.Scalar(SampleProject));

            var resourceMamager = new Mock<IResourceManagerClient>();
            resourceMamager
                .Setup(r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("mock", new Exception()));

            var workspace = new ProjectWorkspace(
                settings.Object,
                resourceMamager.Object);

            var projects = await workspace
                .ExpandAsync(Organization.Default.Locator, CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(projects);
            Assert.IsFalse(projects.First().IsAccessible);
            Assert.AreEqual(SampleProject, projects.First().Locator);
            Assert.AreEqual(SampleProject.Name, projects.First().DisplayName);
        }

        [Test]
        public void ExpandOrganization_WhenReauthTriggered()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            settings
                .SetupGet(s => s.Projects)
                .Returns(Enumerables.Scalar(SampleProject));

            // Trigger reauth.
            var resourceMamager = new Mock<IResourceManagerClient>();
            resourceMamager
                .Setup(r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TokenResponseException(new TokenErrorResponse()
                {
                    Error = "invalid_grant"
                }));

            var workspace = new ProjectWorkspace(
                settings.Object,
                resourceMamager.Object);

            ExceptionAssert.ThrowsAggregateException<TokenResponseException>(
                () => workspace
                    .ExpandAsync(Organization.Default.Locator, CancellationToken.None)
                    .Wait());
        }

        [Test]
        public async Task ExpandOrganization_WhenAncestryInaccessible()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            settings
                .SetupGet(s => s.Projects)
                .Returns(Enumerables.Scalar(SampleProject));

            var resourceMamager = new Mock<IResourceManagerClient>();
            resourceMamager
                .Setup(r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrmProject()
                {
                    Name = "Project 1"
                });

            var workspace = new ProjectWorkspace(
                settings.Object,
                resourceMamager.Object);

            var projects = await workspace
                .ExpandAsync(Organization.Default.Locator, CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(projects);
            Assert.IsTrue(projects.First().IsAccessible);
            Assert.AreEqual(SampleProject, projects.First().Locator);
            Assert.AreEqual("Project 1", projects.First().DisplayName);
            Assert.AreEqual(Organization.Default.Locator, projects.First().Organization);
        }

        [Test]
        public async Task ExpandOrganization()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            settings
                .SetupGet(s => s.Projects)
                .Returns(Enumerables.Scalar(SampleProject));

            var org = new OrganizationLocator(1);

            var resourceMamager = new Mock<IResourceManagerClient>();
            resourceMamager
                .Setup(r => r.GetProjectAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrmProject()
                {
                    Name = "Project 1"
                });
            resourceMamager
                .Setup(r => r.FindOrganizationAsync(SampleProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync(org);
            resourceMamager
                .Setup(r => r.GetOrganizationAsync(org, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrmOrganization()
                {
                    DisplayName = "One"
                });

            var workspace = new ProjectWorkspace(
                settings.Object,
                resourceMamager.Object);

            var projects = await workspace
                .ExpandAsync(org, CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(projects);
            Assert.IsTrue(projects.First().IsAccessible);
            Assert.AreEqual(SampleProject, projects.First().Locator);
            Assert.AreEqual("Project 1", projects.First().DisplayName);
            Assert.AreEqual(org, projects.First().Organization);
        }

        //----------------------------------------------------------------------
        // Query projects.
        //----------------------------------------------------------------------

        [Test]
        public async Task QueryProject_WhenNotFound()
        {
            var settings = new Mock<IProjectWorkspaceSettings>();
            var workspace = new ProjectWorkspace(
                settings.Object,
                new Mock<IResourceManagerClient>().Object);

            var aspect = await workspace
                .QueryAspectAsync(SampleProject, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(aspect);
        }
    }
}
