//
// Copyright 2021 Google LLC
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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.ProjectModel
{
    public interface IProjectModelService
    {
        /// <summary>
        /// Add a project so that it will be considered when
        /// the model is next (force-) reloaded.
        /// </summary>
        Task AddProjectAsync(ProjectLocator project);

        /// <summary>
        /// Remove project so that it will not be considered when
        /// the model is next (force-) reloaded.
        /// </summary>
        Task RemoveProjectAsync(ProjectLocator project);

        /// <summary>
        /// Get model, either from cache or from backend.
        /// </summary>
        Task<IProjectExplorerCloudNode> GetModelAsync(
            bool forceReload,
            CancellationToken token);

        /// <summary>
        /// Looks up a node by locator in the cached model.
        /// </summary>
        IProjectExplorerNode TryFindNode(ResourceLocator locator);

        /// <summary>
        /// Gets the active/selected node. The selection
        /// is kept across reloads.
        /// </summary>
        IProjectExplorerNode ActiveNode { get; }

        /// <summary>
        /// Gets the active/selected node. The selection
        /// is kept across reloads.
        /// </summary>
        Task SetActiveNodeAsync(IProjectExplorerNode node);
    }

    public class ProjectModelService : IProjectModelService
    {
        private readonly IServiceProvider serviceProvider;

        private ResourceLocator activeNode;
        private CloudNode cachedModel = null;

        private async Task<CloudNode> LoadModelAsync(
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            using (var computeEngineAdapter = this.serviceProvider
                .GetService<IComputeEngineAdapter>())
            {
                var accessibleProjects = new List<ProjectNode>();
                var inaccessibleProjects = new List<ProjectLocator>();

                foreach (var project in await this.serviceProvider
                        .GetService<IProjectRepository>()
                        .ListProjectsAsync()
                        .ConfigureAwait(false))
                {
                    //
                    // NB. Some projects might not be accessible anymore,
                    // either because they have been deleted or the user
                    // lost access.
                    //

                    try
                    {
                        var projectDetails = await computeEngineAdapter.GetProjectAsync(
                                project.ProjectId,
                                token)
                            .ConfigureAwait(false);
                        var instances = await computeEngineAdapter
                            .ListInstancesAsync(project.ProjectId, token)
                            .ConfigureAwait(false);

                        accessibleProjects.Add(ProjectNode.FromProject(
                            projectDetails,
                            instances));

                        ApplicationTraceSources.Default.TraceVerbose(
                            "Successfully loaded project {0} with {1} instances", 
                            projectDetails.Name, 
                            instances.Count());
                    }
                    catch (Exception e) when (e.IsReauthError())
                    {
                        // Propagate reauth errors so that the reauth logic kicks in.
                        throw;
                    }
                    catch (Exception e)
                    {
                        ApplicationTraceSources.Default.TraceError(
                            "Failed to load project {0}: {1}",
                            project.ProjectId,
                            e);

                        // 
                        // Continue with other projects.
                        //
                        inaccessibleProjects.Add(new ProjectLocator(project.ProjectId));
                    }
                }

                return new CloudNode(accessibleProjects, inaccessibleProjects);
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ProjectModelService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        //---------------------------------------------------------------------
        // IProjectModelService.
        //---------------------------------------------------------------------

        public async Task AddProjectAsync(ProjectLocator project)
        {
            await this.serviceProvider
                .GetService<IProjectRepository>()
                .AddProjectAsync(project.ProjectId)
                .ConfigureAwait(false);

            await this.serviceProvider
                .GetService<IEventService>()
                .FireAsync(new ProjectAddedEvent(project.ProjectId))
                .ConfigureAwait(false);
        }

        public async Task RemoveProjectAsync(ProjectLocator project)
        {
            await this.serviceProvider
                .GetService<IProjectRepository>()
                .DeleteProjectAsync(project.ProjectId)
                .ConfigureAwait(false);

            await this.serviceProvider
                .GetService<IEventService>()
                .FireAsync(new ProjectDeletedEvent(project.ProjectId))
                .ConfigureAwait(false);
        }

        public async Task<IProjectExplorerCloudNode> GetModelAsync(
            bool forceReload,
            CancellationToken token)
        {
            if (this.cachedModel == null || forceReload)
            {
                //
                // NB. If called concurrently, we might be triggering multiple
                // loads, but that's ok since the operation is read-only.
                //

                this.cachedModel = await LoadModelAsync(token)
                    .ConfigureAwait(false);
            }

            Debug.Assert(this.cachedModel != null);

            return this.cachedModel;
        }

        public IProjectExplorerNode ActiveNode
        {
            get
            {
                //
                // Look up the node. It's possible that no node has been 
                // selected or that the node is not there anymore because 
                // it was removed by a reload - then default to the root.
                //
                if (this.activeNode == null)
                {
                    return this.cachedModel;
                }
                else
                {
                    return TryFindNode(this.activeNode) ?? this.cachedModel;
                }
            }
        }

        public async Task SetActiveNodeAsync(IProjectExplorerNode node)
        {
            // TODO: Use polymorphism instead.
            if (node is IProjectExplorerInstanceNode instanceNode)
            {
                this.activeNode = instanceNode.Instance;
            }
            else if (node is IProjectExplorerZoneNode zoneNode)
            {
                this.activeNode = zoneNode.Zone;
            }
            else if (node is IProjectExplorerProjectNode projectNode)
            {
                this.activeNode = projectNode.Project;
            }
            else
            {
                this.activeNode = null;
            }

            if (this.activeNode != null)
            {
                await this.serviceProvider
                    .GetService<IEventService>()
                    .FireAsync(new ProjectExplorerNodeSelectedEvent(node))
                    .ConfigureAwait(true);
            }
        }

        public IProjectExplorerNode TryFindNode(ResourceLocator locator)
        {
            var model = this.cachedModel;
            if (model == null)
            {
                return null;
            }
            else if (locator is ProjectLocator projectLocator)
            {
                return model.Projects
                    .FirstOrDefault(p => p.Project == projectLocator);
            }
            else if (locator is ZoneLocator zoneLocator)
            {
                return model.Projects
                    .SelectMany(p => p.Zones)
                    .FirstOrDefault(z => z.Zone == zoneLocator);
            }
            else if (locator is InstanceLocator instanceLocator)
            {
                return model.Projects
                    .SelectMany(p => p.Zones)
                    .SelectMany(z => z.Instances)
                    .FirstOrDefault(i => i.Instance == instanceLocator);
            }
            else
            {
                throw new ArgumentException("Unrecognized locator " + locator);
            }
        }
    }
}
