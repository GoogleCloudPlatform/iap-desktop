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

using Google.Solutions.Apis;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Threading;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.EntityModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CrmProject = Google.Apis.CloudResourceManager.v1.Data.Project;

namespace Google.Solutions.IapDesktop.Core.ResourceModel
{
    /// <summary>
    /// Contains a user-selected set of projects, aggregated
    /// by the organization they belong to.
    /// </summary>
    public class ProjectWorkspace 
    {
        private readonly IProjectWorkspaceSettings settings;
        private readonly IAncestryCache ancestryCache;
        private readonly IResourceManagerClient resourceManager;

        private readonly AsyncLock cacheLock = new AsyncLock();
        private State? cache = null;
        private volatile bool cacheIsDirty = false;

        public ProjectWorkspace(
            IProjectWorkspaceSettings settings,
            IAncestryCache ancestryCache,
            IResourceManagerClient resourceManager)
        {
            this.settings = settings;
            this.ancestryCache = ancestryCache;
            this.resourceManager = resourceManager;

            this.settings.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(settings.Projects))
                {
                    //
                    // Force cache invalidation next time it's accessed.
                    //
                    this.cacheIsDirty = true;
                }
            };
        }

        //----------------------------------------------------------------------
        // State loading.
        //----------------------------------------------------------------------

        /// <summary>
        /// Tracks the (cached) state.
        /// </summary>
        private class State
        {
            /// <summary>
            /// Organizations used in this workspace.
            /// </summary>
            internal IDictionary<OrganizationLocator, Organization> Organizations { get; }

            /// <summary>
            /// Projects used in this workspace.
            /// </summary>
            internal IDictionary<ProjectLocator, Project> Projects { get; }

            internal State(
                IDictionary<OrganizationLocator, Organization> organizations,
                IDictionary<ProjectLocator, Project> projects)
            {
                this.Organizations = organizations;
                this.Projects = projects;
            }
        }

        private class ProjectWithAncestry
        {
            internal ProjectLocator Locator;
            internal CrmProject? CrmProject;
            internal bool IsAccessible => this.CrmProject != null;
            internal OrganizationLocator? OrganizationLocator;

            public ProjectWithAncestry(ProjectLocator locator)
            {
                this.Locator = locator;
                this.CrmProject = null;
                this.OrganizationLocator = null;
            }
        }

        /// <summary>
        /// Load list of projects from API.
        /// </summary>
        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks")]
        private static async Task<State> LoadStateAsync(
            IProjectWorkspaceSettings context,
            IAncestryCache ancestryCache,
            IResourceManagerClient resourceManager,
            CancellationToken cancellationToken)
        {
            //
            // NB. The Compute Engine project.get resource does not include the
            // project name, so we have to use the Resource Manager API instead.
            //
            var getProjectTasks = context
                .Projects
                .EnsureNotNull()
                .Select(p => new {
                    Locator = p,
                    Task = resourceManager.GetProjectAsync(p, cancellationToken)
                })
                .ToList();

            var projects = new List<ProjectWithAncestry>();

            foreach (var task in getProjectTasks)
            {
                //
                // NB. Some projects might not be accessible anymore,
                // either because they have been deleted or the user
                // lost access.
                //
                try
                {
                    var project = new ProjectWithAncestry(task.Locator)
                    {
                        CrmProject = await task.Task.ConfigureAwait(false)
                    };
                    projects.Add(project);

                    //
                    // Add cached ancestry, if available.
                    //
                    ancestryCache.TryGetAncestry(project.Locator, out project.OrganizationLocator);

                    CoreTraceSource.Log.TraceVerbose(
                        "Successfully loaded project {0}", project.Locator);
                }
                catch (Exception e) when (e.IsReauthError())
                {
                    //
                    // Propagate reauth errors so that the reauth logic kicks in.
                    //
                    throw e.Unwrap();
                }
                catch (Exception e)
                {
                    // 
                    // Add as inaccessible project and continue.
                    //
                    projects.Add(new ProjectWithAncestry(task.Locator));

                    CoreTraceSource.Log.TraceError(
                        "Failed to load project {0}: {1}",
                        task.Locator,
                        e);
                }
            }

            Debug.Assert(projects.Count == getProjectTasks.Count);

            //
            // At this point, we have all projects, but we might not know the
            // org ID for all projects yet.
            //

            var findOrgIdsTasks = projects
                .Where(p => p.IsAccessible && p.OrganizationLocator == null)
                .Select(p => new {
                    Project = p,
                    Task = resourceManager.FindOrganizationAsync(
                        p.Locator,
                        cancellationToken)
                })
                .ToList();

            foreach (var task in findOrgIdsTasks)
            {
                //
                // Amend ancestry (if available).
                //
                task.Project.OrganizationLocator = await task.Task.ConfigureAwait(false);

                if (task.Project.OrganizationLocator != null)
                {
                    //
                    // Cache ancestry information to speed up future lookups.
                    //
                    ancestryCache.SetAncestry(task.Project.Locator, task.Project.OrganizationLocator);
                }
            }

            //
            // Finally, resolve all org IDs.
            //
            var findOrgTasks = projects
                .Where(p => p.OrganizationLocator != null)
                .Select(p => p.OrganizationLocator)
                .Distinct()
                .Select(loc => new {
                    Organization = loc!,
                    Task = resourceManager.GetOrganizationAsync(loc!, cancellationToken)
                })
                .ToList();

            var organizations = new Dictionary<OrganizationLocator, Organization>();
            foreach (var task in findOrgTasks)
            {
                try
                {
                    var org = await task.Task.ConfigureAwait(false);
                    organizations[task.Organization] = new Organization(
                        task.Organization,
                        org.DisplayName);
                }
                catch (Exception e) when (e.IsReauthError())
                {
                    //
                    // Propagate reauth errors so that the reauth logic kicks in.
                    //
                    throw e.Unwrap();
                }
                catch
                {
                    //
                    // Organization inaccessible (even though we do have its ID),
                    // use default.
                    //
                    organizations[task.Organization] = Organization.Default;
                }
            }

            if (projects.Any(p => p.OrganizationLocator == null))
            {
                //
                // Include default org.
                //
                organizations[Organization.Default.Locator] = Organization.Default;
            }

            return new State(
                organizations,
                projects.ToDictionary(
                    p => p.Locator,
                    p => new Project(
                        p.OrganizationLocator ?? Organization.Default.Locator,
                        p.Locator,
                        p.CrmProject != null
                            ? p.CrmProject.Name // Actual name.
                            : p.Locator.Name,   // Project inaccessible, use ID.
                        p.CrmProject != null)));
        }

        /// <summary>
        /// Preload cache.
        /// </summary>
        public Task PreloadCacheAsync()
        {
            return LoadStateAsync(
                this.settings,
                this.ancestryCache,
                this.resourceManager,
                CancellationToken.None);
        }
    }

    //----------------------------------------------------------------------
    // Context.
    //----------------------------------------------------------------------

    /// <summary>
    /// Pesistent settings for a workspace.
    /// </summary>
    public interface IProjectWorkspaceSettings : INotifyPropertyChanged
    {
        /// <summary>
        /// List of projects in this workspace.
        /// </summary>
        /// <remarks>
        /// Raises a PropertyChanged events when changed.
        /// </remarks>
        IEnumerable<ProjectLocator> Projects { get; }
    }

    /// <summary>
    /// Cache for project ancestry information.
    /// </summary>
    public interface IAncestryCache
    {
        /// <summary>
        /// Get cached project's ancestry, in top-to-bottom order.
        /// 
        /// The ancestry path might be incomplete or empty if the current 
        /// doesn't have sufficient access to resolve the full ancestry.
        /// </summary>
        /// <returns>false if ancestry hasn't been set before</returns>
        bool TryGetAncestry(ProjectLocator project, out OrganizationLocator? ancestry);

        /// <summary>
        /// Cache project ancestry path, in top-to-bottom order.
        /// 
        /// The ancestry path might be incomplete or empty if the current 
        /// doesn't have sufficient access to resolve the full ancestry.
        /// </summary>
        void SetAncestry(ProjectLocator project, OrganizationLocator ancestry);
    }
}
