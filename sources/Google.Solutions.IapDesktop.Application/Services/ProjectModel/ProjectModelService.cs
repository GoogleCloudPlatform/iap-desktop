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
using Google.Solutions.IapDesktop.Application.Util;
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
    /// <summary>
    /// Represents the in-memory model (or workspace) of projects and
    /// instances. Data is cached, but read-only.
    /// </summary>
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
        /// Load projects without loading zones and instances.
        /// Uses cached data if available.
        /// </summary>
        Task<IProjectExplorerCloudNode> GetRootNodeAsync(
            bool forceReload,
            CancellationToken token);

        /// <summary>
        /// Load zones and instances. Uses cached data if available.
        /// </summary>
        Task<IReadOnlyCollection<IProjectExplorerZoneNode>> GetZoneNodesAsync(
            ProjectLocator project,
            bool forceReload,
            CancellationToken token);

        /// <summary>
        /// Load any node. Uses cached data if available.
        /// </summary>
        Task<IProjectExplorerNode> GetNodeAsync(
            ResourceLocator locator,
            CancellationToken token);

        /// <summary>
        /// Gets the active/selected node. The selection
        /// is kept across reloads.
        /// </summary>
        Task<IProjectExplorerNode> GetActiveNodeAsync(CancellationToken token);

        /// <summary>
        /// Gets the active/selected node. The selection
        /// is kept across reloads.
        /// </summary>
        Task SetActiveNodeAsync(
            IProjectExplorerNode node,
            CancellationToken token);

        /// <summary>
        /// Gets the active/selected node. The selection
        /// is kept across reloads.
        /// </summary>
        Task SetActiveNodeAsync(
            ResourceLocator locator,
            CancellationToken token);
    }

    public class ProjectModelService : IProjectModelService, IDisposable
    {
        private readonly IServiceProvider serviceProvider;

        private ResourceLocator activeNode;

        private readonly AsyncLock cacheLock = new AsyncLock();
        private CloudNode cachedRoot = null;
        private IDictionary<ProjectLocator, IReadOnlyCollection<IProjectExplorerZoneNode>> cachedZones =
            new Dictionary<ProjectLocator, IReadOnlyCollection<IProjectExplorerZoneNode>>();

        //---------------------------------------------------------------------
        // Data loading (uncached).
        //---------------------------------------------------------------------

        private async Task<CloudNode> LoadProjectsAsync(
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            using (var computeEngineAdapter = this.serviceProvider
                .GetService<IComputeEngineAdapter>())
            using (var resourceManagerAdapter = this.serviceProvider
                .GetService<IResourceManagerAdapter>())
            {
                var accessibleProjects = new List<ProjectNode>();
                var inaccessibleProjects = new List<ProjectLocator>();

                //
                // Load projects in parallel.
                //
                // NB. The Compute Engine project.get resource does not include the
                // project name, so we have to use the Resource Manager API instead.
                //
                var tasks = new Dictionary<ProjectLocator, Task<Google.Apis.CloudResourceManager.v1.Data.Project>>();
                foreach (var project in await this.serviceProvider
                    .GetService<IProjectRepository>()
                    .ListProjectsAsync()
                    .ConfigureAwait(false))
                {
                    tasks.Add(
                        new ProjectLocator(project.ProjectId),
                        resourceManagerAdapter.GetProjectAsync(
                            project.ProjectId,
                            token));
                }

                foreach (var task in tasks)
                {
                    //
                    // NB. Some projects might not be accessible anymore,
                    // either because they have been deleted or the user
                    // lost access.
                    //
                    try
                    {
                        var project = task.Value.Result;
                        accessibleProjects.Add(new ProjectNode(
                            task.Key,
                            project.Name));

                        ApplicationTraceSources.Default.TraceVerbose(
                            "Successfully loaded project {0}", task.Key);
                    }
                    catch (Exception e) when (e.IsReauthError())
                    {
                        // Propagate reauth errors so that the reauth logic kicks in.
                        throw e.Unwrap();
                    }
                    catch (Exception e)
                    {
                        ApplicationTraceSources.Default.TraceError(
                            "Failed to load project {0}: {1}",
                            task.Key,
                            e);

                        // 
                        // Continue with other projects.
                        //
                        inaccessibleProjects.Add(task.Key);
                    }
                }

                return new CloudNode(accessibleProjects, inaccessibleProjects);
            }
        }

        private async Task<IReadOnlyCollection<IProjectExplorerZoneNode>> LoadZones(
            ProjectLocator project,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            using (var computeEngineAdapter = this.serviceProvider
                .GetService<IComputeEngineAdapter>())
            {
                var instances = await computeEngineAdapter
                    .ListInstancesAsync(project.ProjectId, token)
                    .ConfigureAwait(false);

                var zoneLocators = instances
                    .EnsureNotNull()
                    .Select(i => ZoneLocator.FromString(i.Zone))
                    .ToHashSet();

                var zones = new List<ZoneNode>();
                foreach (var zoneLocator in zoneLocators.OrderBy(z => z.Name))
                {
                    var instancesInZone = instances
                        .Where(i => ZoneLocator.FromString(i.Zone) == zoneLocator)
                        .Where(i => i.Disks != null && i.Disks.Any())
                        .OrderBy(i => i.Name)
                        .Select(i => new InstanceNode(
                            i.Id.Value,
                            new InstanceLocator(
                                zoneLocator.ProjectId,
                                zoneLocator.Name,
                                i.Name),
                            i.IsWindowsInstance()
                                ? OperatingSystems.Windows
                                : OperatingSystems.Linux,
                            i.Status == "RUNNING"))
                        .ToList();

                    zones.Add(new ZoneNode(
                        zoneLocator,
                        instancesInZone));
                }

                return zones;
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
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(project))
            {
                this.serviceProvider
                    .GetService<IProjectRepository>()
                    .AddProject(project);

                await this.serviceProvider
                    .GetService<IEventService>()
                    .FireAsync(new ProjectAddedEvent(project.ProjectId))
                    .ConfigureAwait(false);
            }
        }

        public async Task RemoveProjectAsync(ProjectLocator project)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(project))
            {
                this.serviceProvider
                    .GetService<IProjectRepository>()
                    .RemoveProject(project);

                //
                // Purge from cache.
                //
                using (await this.cacheLock.AcquireAsync(CancellationToken.None)
                    .ConfigureAwait(false))
                {
                    this.cachedZones.Remove(project);
                }

                await this.serviceProvider
                    .GetService<IEventService>()
                    .FireAsync(new ProjectDeletedEvent(project.ProjectId))
                    .ConfigureAwait(false);
            }
        }

        public async Task<IProjectExplorerCloudNode> GetRootNodeAsync(
            bool forceReload,
            CancellationToken token)
        {
            using (await this.cacheLock.AcquireAsync(token).ConfigureAwait(false))
            { 
                if (this.cachedRoot == null || forceReload)
                {
                    //
                    // Load from backend and cache.
                    //
                    this.cachedRoot = await LoadProjectsAsync(token)
                        .ConfigureAwait(false);
                }
            }

            Debug.Assert(this.cachedRoot != null);

            return this.cachedRoot;
        }

        public async Task<IReadOnlyCollection<IProjectExplorerZoneNode>> GetZoneNodesAsync(
            ProjectLocator project,
            bool forceReload,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(project, forceReload))
            {
                IReadOnlyCollection<IProjectExplorerZoneNode> zones = null;
                using (await this.cacheLock.AcquireAsync(token).ConfigureAwait(false))
                {
                    if (!this.cachedZones.TryGetValue(
                        project,
                        out zones) || forceReload)
                    {
                        //
                        // Load from backend and cache.
                        //
                        zones = await LoadZones(project, token)
                            .ConfigureAwait(false);
                        this.cachedZones[project] = zones;
                    }
                }

                Debug.Assert(zones != null);

                return zones;
            }
        }

        public async Task<IProjectExplorerNode> GetNodeAsync(
            ResourceLocator locator,
            CancellationToken token)
        {

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(locator))
            {
                if (locator is ProjectLocator projectLocator)
                {
                    var root = await GetRootNodeAsync(false, token)
                        .ConfigureAwait(false);

                    return root.Projects.FirstOrDefault(p => p.Project == projectLocator);
                }
                else if (locator is ZoneLocator zoneLocator)
                {
                    var zones = await GetZoneNodesAsync(
                            new ProjectLocator(zoneLocator.ProjectId),
                            false,
                            token)
                        .ConfigureAwait(false);
                    return zones.FirstOrDefault(z => z.Zone == zoneLocator);
                }
                else if (locator is InstanceLocator instanceLocator)
                {
                    var zones = await GetZoneNodesAsync(
                            new ProjectLocator(instanceLocator.ProjectId),
                            false,
                            token)
                        .ConfigureAwait(false);
                    return zones
                        .SelectMany(z => z.Instances)
                        .FirstOrDefault(i => i.Instance == instanceLocator);
                }
                else
                {
                    throw new ArgumentException("Unrecognized locator " + locator);
                }
            }
        }

        public async Task<IProjectExplorerNode> GetActiveNodeAsync(CancellationToken token)
        {
            //
            // Look up the node. It's possible that no node has been 
            // selected or that the node is not there anymore because 
            // it was removed by a reload - then default to the root.
            //
            if (this.activeNode != null)
            {
                var node = await GetNodeAsync(this.activeNode, token)
                    .ConfigureAwait(false);
                if (node != null)
                {
                    return node;
                }
            }

            return await GetRootNodeAsync(false, token)
                .ConfigureAwait(false);
        }

        public Task SetActiveNodeAsync(
            IProjectExplorerNode node,
            CancellationToken token)
        {
            if (node is IProjectExplorerInstanceNode instanceNode)
            {
                return SetActiveNodeAsync(instanceNode.Instance, token);
            }
            else if (node is IProjectExplorerZoneNode zoneNode)
            {
                return SetActiveNodeAsync(zoneNode.Zone, token);
            }
            else if (node is IProjectExplorerProjectNode projectNode)
            {
                return SetActiveNodeAsync(projectNode.Project, token);
            }
            else
            {
                return SetActiveNodeAsync((ResourceLocator)null, token);
            }
        }

        public async Task SetActiveNodeAsync(
            ResourceLocator locator,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(locator))
            {
                IProjectExplorerNode node;
                if (locator != null)
                {
                    node = await GetNodeAsync(locator, token)
                        .ConfigureAwait(false);

                    if (node != null)
                    {
                        //
                        // Node found -> set as active and fire event.
                        //
                        this.activeNode = locator;

                        // TODO: rename event
                        await this.serviceProvider
                            .GetService<IEventService>()
                            .FireAsync(new ProjectExplorerNodeSelectedEvent(node))
                            .ConfigureAwait(true);

                        return;
                    }
                }

                //
                // Locator was null or pointed to nonexisting node ->
                // set root as active.
                //
                this.activeNode = null;
                if (this.cachedRoot != null)
                {
                    await this.serviceProvider
                        .GetService<IEventService>()
                        .FireAsync(new ProjectExplorerNodeSelectedEvent(this.cachedRoot))
                        .ConfigureAwait(true);
                }
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            this.cacheLock.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
