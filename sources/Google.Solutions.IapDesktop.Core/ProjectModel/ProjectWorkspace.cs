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

using Google.Solutions.Apis;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Threading;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ProjectModel
{
    /// <summary>
    /// Represents the in-memory model (or workspace) of projects and
    /// instances. Data is cached, but read-only.
    /// </summary>
    public interface IProjectWorkspace : IDisposable
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
        Task<IProjectModelCloudNode> GetRootNodeAsync(
            bool forceReload,
            CancellationToken token);

        /// <summary>
        /// Load zones and instances. Uses cached data if available.
        /// </summary>
        Task<IReadOnlyCollection<IProjectModelZoneNode>> GetZoneNodesAsync(
            ProjectLocator project,
            bool forceReload,
            CancellationToken token);

        /// <summary>
        /// Load any node. Uses cached data if available.
        /// </summary>
        Task<IProjectModelNode?> GetNodeAsync(
            ResourceLocator locator,
            CancellationToken token);

        /// <summary>
        /// Gets the active/selected node. The selection
        /// is kept across reloads.
        /// </summary>
        Task<IProjectModelNode> GetActiveNodeAsync(CancellationToken token);

        /// <summary>
        /// Gets the active/selected node. The selection
        /// is kept across reloads.
        /// </summary>
        Task SetActiveNodeAsync(
            IProjectModelNode node,
            CancellationToken token);

        /// <summary>
        /// Gets the active/selected node. The selection
        /// is kept across reloads.
        /// </summary>
        Task SetActiveNodeAsync(
            ResourceLocator locator,
            CancellationToken token);
    }

    public class ProjectWorkspace : IProjectWorkspace
    {
        private readonly IComputeEngineClient computeClient;
        private readonly IResourceManagerClient resourceManagerClient;
        private readonly IProjectRepository projectRepository;
        private readonly IEventQueue eventQueue;

        private ResourceLocator? activeNode;

        private readonly AsyncLock cacheLock = new AsyncLock();
        private CloudNode? cachedRoot = null;
        private readonly IDictionary<ProjectLocator, IReadOnlyCollection<IProjectModelZoneNode>> cachedZones =
            new Dictionary<ProjectLocator, IReadOnlyCollection<IProjectModelZoneNode>>();

        // For testing only.
        internal int CachedProjectsCount => this.cachedZones.Count;

        //---------------------------------------------------------------------
        // Data loading (uncached).
        //---------------------------------------------------------------------

        private async Task<CloudNode> LoadProjectsAsync(
            CancellationToken token)
        {
            using (CoreTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var accessibleProjects = new List<ProjectNode>();

                //
                // Load projects in parallel.
                //
                // NB. The Compute Engine project.get resource does not include the
                // project name, so we have to use the Resource Manager API instead.
                //
                var tasks = new Dictionary<ProjectLocator, Task<Google.Apis.CloudResourceManager.v1.Data.Project>>();
                foreach (var project in await this.projectRepository
                    .ListProjectsAsync()
                    .ConfigureAwait(false))
                {
                    tasks.Add(
                        new ProjectLocator(project.ProjectId),
                        this.resourceManagerClient
                            .GetProjectAsync(project.Project, token));
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
                        var project = await task.Value.ConfigureAwait(false);
                        Debug.Assert(project != null);

                        accessibleProjects.Add(new ProjectNode(
                            task.Key,
                            true,
                            project!.Name));

                        CoreTraceSource.Log.TraceVerbose(
                            "Successfully loaded project {0}", task.Key);
                    }
                    catch (Exception e) when (e.IsReauthError())
                    {
                        // Propagate reauth errors so that the reauth logic kicks in.
                        throw e.Unwrap();
                    }
                    catch (Exception e)
                    {
                        // 
                        // Add as inaccessible project and continue.
                        //
                        accessibleProjects.Add(new ProjectNode(
                            task.Key,
                            false,
                            task.Key.Name)); // Use ID instead of name.

                        CoreTraceSource.Log.TraceError(
                            "Failed to load project {0}: {1}",
                            task.Key,
                            e);
                    }
                }

                return new CloudNode(accessibleProjects);
            }
        }

        private async Task<IReadOnlyCollection<IProjectModelZoneNode>> LoadZonesAsync(
            ProjectLocator project,
            CancellationToken token)
        {
            using (CoreTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var instances = await this.computeClient
                    .ListInstancesAsync(project.Project, token)
                    .ConfigureAwait(false);

                var zoneLocators = instances
                    .EnsureNotNull()
                    .Select(i => ZoneLocator.Parse(i.Zone))
                    .ToHashSet();

                var zones = new List<ZoneNode>();
                foreach (var zoneLocator in zoneLocators.OrderBy(z => z.Name))
                {
                    var instancesInZone = instances
                        .Where(i => ZoneLocator.Parse(i.Zone) == zoneLocator)
                        .Where(i => i.Disks != null && i.Disks.Any())
                        .OrderBy(i => i.Name)
                        .Select(i => new InstanceNode(
                            this,
                            i.Id!.Value,
                            new InstanceLocator(
                                zoneLocator.ProjectId,
                                zoneLocator.Name,
                                i.Name),
                            TraitDetector.DetectTraits(i),
                            i.Status))
                        .ToList();

                    zones.Add(new ZoneNode(
                        zoneLocator,
                        instancesInZone));
                }

                return zones;
            }
        }

        internal async Task ControlInstanceAsync(
            InstanceLocator instance,
            InstanceControlCommand command,
            CancellationToken cancellationToken)
        {
            using (CoreTraceSource.Log.TraceMethod()
                .WithParameters(instance, command))
            {
                await this.computeClient.ControlInstanceAsync(
                    instance,
                    command,
                    cancellationToken)
                .ConfigureAwait(false);

                await this.eventQueue.PublishAsync(
                    new InstanceStateChangedEvent(
                        instance,
                        command == InstanceControlCommand.Start ||
                            command == InstanceControlCommand.Resume ||
                            command == InstanceControlCommand.Reset))
                    .ConfigureAwait(false);
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ProjectWorkspace(
            IComputeEngineClient computeClient,
            IResourceManagerClient resourceManagerAdapter,
            IProjectRepository projectRepository,
            IEventQueue eventQueue)
        {
            this.computeClient = computeClient.ExpectNotNull(nameof(computeClient));
            this.resourceManagerClient = resourceManagerAdapter.ExpectNotNull(nameof(resourceManagerAdapter));
            this.projectRepository = projectRepository.ExpectNotNull(nameof(projectRepository));
            this.eventQueue = eventQueue.ExpectNotNull(nameof(eventQueue));
        }

        //---------------------------------------------------------------------
        // IProjectModelService.
        //---------------------------------------------------------------------

        public async Task AddProjectAsync(ProjectLocator project)
        {
            using (CoreTraceSource.Log.TraceMethod().WithParameters(project))
            {
                this.projectRepository.AddProject(project);

                await this.eventQueue
                    .PublishAsync(new ProjectAddedEvent(project.ProjectId))
                    .ConfigureAwait(false);
            }
        }

        public async Task RemoveProjectAsync(ProjectLocator project)
        {
            using (CoreTraceSource.Log.TraceMethod().WithParameters(project))
            {
                this.projectRepository.RemoveProject(project);

                //
                // Purge from cache.
                //
                using (await this.cacheLock.AcquireAsync(CancellationToken.None)
                    .ConfigureAwait(false))
                {
                    this.cachedZones.Remove(project);
                }

                await this.eventQueue
                    .PublishAsync(new ProjectDeletedEvent(project.ProjectId))
                    .ConfigureAwait(false);
            }
        }

        public async Task<IProjectModelCloudNode> GetRootNodeAsync(
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

                    //
                    // Load zones for projects. This serves two purposes:
                    // - On startup, it ensures that all zones are loaded in 
                    //   parallel, resulting in faster startup experience.
                    // - On force-reload, it ensures that we not only reload
                    //   the list of projects, but also their contents (zones).
                    //
                    var loadProjectTasks = this.cachedRoot
                        .Projects
                        .Select(p => new {
                            p.Project,
                            Zones = LoadZonesAsync(p.Project, token)
                        })

                        //
                        // Force eager evaluation, otherwise we're not triggering
                        // the network call.
                        //
                        .ToList();

                    foreach (var loadProjectTask in loadProjectTasks)
                    {
                        try
                        {
                            this.cachedZones[loadProjectTask.Project] = await loadProjectTask
                                .Zones
                                .ConfigureAwait(false);
                        }
                        catch (ResourceAccessDeniedException)
                        {
                            //
                            // Project inaccessible, ignore. The project will till show up
                            // in the model, but it won't have its zones pre-loaded.
                            //
                        }
                    }
                }
            }

            Debug.Assert(this.cachedRoot != null);

            return this.cachedRoot!;
        }

        public async Task<IReadOnlyCollection<IProjectModelZoneNode>> GetZoneNodesAsync(
            ProjectLocator project,
            bool forceReload,
            CancellationToken token)
        {
            using (CoreTraceSource.Log.TraceMethod().WithParameters(project, forceReload))
            {
                IReadOnlyCollection<IProjectModelZoneNode>? zones = null;
                using (await this.cacheLock.AcquireAsync(token).ConfigureAwait(false))
                {
                    if (!this.cachedZones.TryGetValue(
                        project,
                        out zones) || forceReload)
                    {
                        //
                        // Load from backend and cache.
                        //
                        zones = await LoadZonesAsync(project, token)
                            .ConfigureAwait(false);
                        this.cachedZones[project] = zones;
                    }
                }

                Debug.Assert(zones != null);
                return zones!;
            }
        }

        public async Task<IProjectModelNode?> GetNodeAsync(
            ResourceLocator locator,
            CancellationToken token)
        {
            using (CoreTraceSource.Log.TraceMethod().WithParameters(locator))
            {
                if (locator is ProjectLocator projectLocator)
                {
                    var root = await GetRootNodeAsync(false, token)
                        .ConfigureAwait(false);

                    return root.Projects.FirstOrDefault(p => p.Project == projectLocator);
                }
                else if (locator is ZoneLocator zoneLocator)
                {
                    var project = await GetNodeAsync(
                            new ProjectLocator(zoneLocator.ProjectId), token)
                        .ConfigureAwait(false);
                    if (project == null)
                    {
                        // Don't load a zone if the parent project has not been added.
                        return null;
                    }

                    var zones = await GetZoneNodesAsync(
                            new ProjectLocator(zoneLocator.ProjectId),
                            false,
                            token)
                        .ConfigureAwait(false);
                    return zones.FirstOrDefault(z => z.Zone == zoneLocator);
                }
                else if (locator is InstanceLocator instanceLocator)
                {
                    var project = await GetNodeAsync(
                            new ProjectLocator(instanceLocator.ProjectId), token)
                        .ConfigureAwait(false);
                    if (project == null)
                    {
                        // Don't load a instance if the parent project has not been added.
                        return null;
                    }

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

        public async Task<IProjectModelNode> GetActiveNodeAsync(CancellationToken token)
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
            IProjectModelNode node,
            CancellationToken token)
        {
            if (node is IProjectModelInstanceNode instanceNode)
            {
                return SetActiveNodeAsync(instanceNode.Instance, token);
            }
            else if (node is IProjectModelZoneNode zoneNode)
            {
                return SetActiveNodeAsync(zoneNode.Zone, token);
            }
            else if (node is IProjectModelProjectNode projectNode)
            {
                return SetActiveNodeAsync(projectNode.Project, token);
            }
            else
            {
                return SetActiveNodeAsync((ResourceLocator?)null, token);
            }
        }

        public async Task SetActiveNodeAsync(
            ResourceLocator? locator,
            CancellationToken token)
        {
            using (CoreTraceSource.Log.TraceMethod().WithParameters(locator))
            {
                IProjectModelNode? node;
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

                        await this.eventQueue
                            .PublishAsync(new ActiveProjectChangedEvent(node))
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
                    await this.eventQueue
                        .PublishAsync(new ActiveProjectChangedEvent(this.cachedRoot))
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
