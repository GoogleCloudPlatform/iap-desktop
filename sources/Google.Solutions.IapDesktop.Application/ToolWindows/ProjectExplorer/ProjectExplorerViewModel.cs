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

using Google.Apis.Logging.v2.Data;
using Google.Solutions.Apis;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Settings.Collection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer
{
    public partial class ProjectExplorerViewModel : ViewModelBase, IDisposable
    {
        private readonly IJobService jobService;
        private readonly IProjectWorkspace workspace;
        private readonly ISessionBroker sessionBroker;
        private readonly ICloudConsoleClient cloudConsoleService;

        private ViewModelNode? selectedNode;
        private string? instanceFilter;
        private OperatingSystems operatingSystemsFilter = OperatingSystems.All;
        private readonly IProjectExplorerSettings settings;

        private bool isUnloadProjectCommandVisible;
        private bool isRefreshProjectsCommandVisible;
        private bool isRefreshAllProjectsCommandVisible;
        private bool isCloudConsoleCommandVisible;
        private bool isLoading = false;

        private IDisposable EnableLoadingStatus()
        {
            this.IsLoading = true;
            return Disposable.For(() => this.IsLoading = false);
        }

        private static async Task RefreshAsync(ViewModelNode node)
        {
            if (!node.CanReload)
            {
                Debug.Assert(node.Parent != null);
                if (node.Parent != null)
                {
                    //
                    // Try reloading parent instead.
                    //
                    await RefreshAsync(node.Parent)
                        .ConfigureAwait(true);
                }
                else
                {
                    //
                    // Ignore.
                    //
                }
            }
            else
            {
                // Force-reload children and discard result.
                await node.GetFilteredChildrenAsync(true)
                    .ConfigureAwait(true);
            }
        }

        internal ProjectExplorerViewModel(
            IProjectExplorerSettings settings,
            IJobService jobService,
            IEventQueue eventQueue,
            ISessionBroker sessionBroker,
            IProjectWorkspace workspace,
            ICloudConsoleClient cloudConsoleService)
        {
            this.settings = settings;
            this.jobService = jobService;
            this.sessionBroker = sessionBroker;
            this.workspace = workspace;
            this.cloudConsoleService = cloudConsoleService;

            this.RefreshSelectedNodeCommand = ObservableCommand.Build(
                "Refresh",
                RefreshSelectedNodeAsync);

            this.RootNode = new CloudViewModelNode(this);

            //
            // NB. Only consider instances that have already bee loaded.
            //
            eventQueue.Subscribe<SessionStartedEvent>(
                e => 
                {
                    if (FindLoadedInstance(e.Instance) is InstanceViewModelNode instance)
                    {
                        instance.IsConnected = true;
                    }
                });
            eventQueue.Subscribe<SessionEndedEvent>(
                e =>
                {
                    if (FindLoadedInstance(e.Instance) is InstanceViewModelNode instance)
                    {
                        instance.IsConnected = false;
                    }
                });
            eventQueue.Subscribe<InstanceStateChangedEvent>(
                async e =>
                {
                    //
                    // Refresh the instance node (or rather, its enclosing project)
                    // to ensure that both the UI and the underlying project model
                    // updated.
                    //
                    if (FindLoadedInstance(e.Instance) is InstanceViewModelNode instance)
                    {
                        await RefreshAsync(instance).ConfigureAwait(true);
                    }
                });
        }

        public ProjectExplorerViewModel(
            IRepository<IApplicationSettings> settingsRepository,
            IJobService jobService,
            IEventQueue eventService,
            ISessionBroker sessionBroker,
            IProjectWorkspace workspace,
            ICloudConsoleClient cloudConsoleService)
            : this(
                new ProjectExplorerSettings(
                    settingsRepository,
                    true),
                jobService,
                eventService,
                sessionBroker,
                workspace,
                cloudConsoleService)
        {
        }

        /// <summary>
        /// Look for instance among the loaded nodes.
        /// </summary>
        private InstanceViewModelNode? FindLoadedInstance(
            InstanceLocator locator)
        {
            
            return this.RootNode.LoadedDescendents
                .OfType<InstanceViewModelNode>()
                .FirstOrDefault(i => Equals(i.Locator, locator));
        }

        internal IReadOnlyCollection<IProjectModelProjectNode> Projects
        {
            get
            {
                if (!this.RootNode.IsLoaded)
                {
                    return Array.Empty<IProjectModelProjectNode>();
                }
                else
                {
                    var modelNode = (IProjectModelCloudNode)this.RootNode.ModelNode;
                    return modelNode.Projects.ToList();
                }
            }
        }

        //---------------------------------------------------------------------
        // Commands.
        //---------------------------------------------------------------------

        public ObservableCommand RefreshSelectedNodeCommand { get; }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsLoading
        {
            get => this.isLoading;
            set
            {
                this.isLoading = value;
                RaisePropertyChange();
            }
        }

        public bool IsLinuxIncluded
        {
            get => this.OperatingSystemsFilter.HasFlag(OperatingSystems.Linux);
            set
            {
                if (value)
                {
                    this.OperatingSystemsFilter |= OperatingSystems.Linux;
                }
                else
                {
                    this.OperatingSystemsFilter &= ~OperatingSystems.Linux;
                }

                RaisePropertyChange();
            }
        }

        public bool IsWindowsIncluded
        {
            get => this.OperatingSystemsFilter.HasFlag(OperatingSystems.Windows);
            set
            {
                if (value)
                {
                    this.OperatingSystemsFilter |= OperatingSystems.Windows;
                }
                else
                {
                    this.OperatingSystemsFilter &= ~OperatingSystems.Windows;
                }

                RaisePropertyChange();
            }
        }

        public CloudViewModelNode RootNode { get; }

        public bool IsUnloadProjectCommandVisible
        {
            get => this.isUnloadProjectCommandVisible;
            set
            {
                this.isUnloadProjectCommandVisible = value;
                RaisePropertyChange();
            }
        }

        public bool IsRefreshProjectsCommandVisible
        {
            get => this.isRefreshProjectsCommandVisible;
            set
            {
                this.isRefreshProjectsCommandVisible = value;
                RaisePropertyChange();
            }
        }

        public bool IsRefreshAllProjectsCommandVisible
        {
            get => this.isRefreshAllProjectsCommandVisible;
            set
            {
                this.isRefreshAllProjectsCommandVisible = value;
                RaisePropertyChange();
            }
        }

        public bool IsCloudConsoleCommandVisible
        {
            get => this.isCloudConsoleCommandVisible;
            set
            {
                this.isCloudConsoleCommandVisible = value;
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // "Input" properties.
        //---------------------------------------------------------------------

        public string? InstanceFilter
        {
            get => this.instanceFilter?.Trim();
            set
            {
                this.instanceFilter = value;
                RaisePropertyChange();

                if (this.RootNode.IsLoaded)
                {
                    this.RootNode.ReapplyFilter();
                }
            }
        }

        public OperatingSystems OperatingSystemsFilter
        {
            get => this.operatingSystemsFilter;
            set
            {
                this.operatingSystemsFilter = value;

                RaisePropertyChange();

                if (this.RootNode.IsLoaded)
                {
                    this.RootNode.ReapplyFilter();
                }
            }
        }

        public ViewModelNode? SelectedNode
        {
            get
            {
                Debug.Assert(
                    this.selectedNode == null ||
                        this.RootNode.DebugIsValidNode(this.selectedNode),
                    "Node detached");

                return this.selectedNode;
            }
            set
            {
                Debug.Assert(
                    this.selectedNode == null ||
                        this.RootNode.DebugIsValidNode(this.selectedNode),
                    "Node detached");

                this.IsUnloadProjectCommandVisible = value is ProjectViewModelNode;
                this.IsRefreshAllProjectsCommandVisible = value is CloudViewModelNode;
                this.IsRefreshProjectsCommandVisible =
                    value is ProjectViewModelNode ||
                    value is ZoneViewModelNode ||
                    value is InstanceViewModelNode;
                this.IsCloudConsoleCommandVisible =
                    value is ProjectViewModelNode ||
                    value is ZoneViewModelNode ||
                    value is InstanceViewModelNode;

                this.selectedNode = value;
                RaisePropertyChange();

                // 
                // Update active node in model.
                //
                _ = this.workspace.SetActiveNodeAsync(
                    value?.Locator,
                    CancellationToken.None);
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public async Task AddProjectsAsync(params ProjectLocator[] projects)
        {
            foreach (var project in projects)
            {
                await this.workspace
                    .AddProjectAsync(project)
                    .ConfigureAwait(true);
            }

            //
            // Refresh to ensure the new project is reflected.
            //
            await RefreshAsync(true).ConfigureAwait(true);
        }

        public async Task RemoveProjectsAsync(params ProjectLocator[] projects)
        {
            //
            // Reset selection to a safe place.
            //
            this.SelectedNode = this.RootNode;

            foreach (var project in projects)
            {
                await this.workspace
                    .RemoveProjectAsync(project)
                    .ConfigureAwait(true);

                //
                // Remove from collapsed list so that we don't
                // accumulate junk.
                //
                this.settings.CollapsedProjects.Remove(project);
            }

            //
            // Refresh to ensure the removal is reflected.
            //
            await RefreshAsync(true).ConfigureAwait(true);
        }

        public async Task<IEnumerable<ViewModelNode>> ExpandRootAsync()
        {
            // Explicitly load nodes.
            var nodes = await this.RootNode.GetFilteredChildrenAsync(false)
                .ConfigureAwait(true);

            // NB. If we did not load the nodes explicitly before, 
            // IsExpanded would asynchronously trigger a load without
            // awaiting the result. To prevent this behavior.
            this.RootNode.IsExpanded = true;

            return nodes;
        }

        public async Task RefreshAsync(bool reloadProjects)
        {
            // Reset selection to a safe place.
            this.SelectedNode = this.RootNode;

            if (reloadProjects)
            {
                // Refresh everything. 
                await RefreshAsync(this.RootNode).ConfigureAwait(true);
            }
            else
            {
                // Retain project nodes, but refresh their descendents.
                var projects = await this.RootNode
                    .GetFilteredChildrenAsync(false)
                    .ConfigureAwait(true);

                await Task
                    .WhenAll(projects
                        .Where(p => p.IsLoaded && p.CanReload)
                        .Select(p => p.GetFilteredChildrenAsync(true)))
                    .ConfigureAwait(true);
            }
        }

        public async Task RefreshSelectedNodeAsync()
        {
            await RefreshAsync(this.SelectedNode ?? this.RootNode)
                .ConfigureAwait(true);
        }

        public async Task UnloadSelectedProjectAsync()
        {
            if (this.SelectedNode is ProjectViewModelNode projectNode)
            {
                await RemoveProjectsAsync(projectNode.ProjectNode.Project)
                    .ConfigureAwait(true);
            }
        }

        public void OpenInCloudConsole()
        {
            if (this.SelectedNode is InstanceViewModelNode vmInstanceNode)
            {
                this.cloudConsoleService.OpenInstanceDetails(
                    vmInstanceNode.InstanceNode.Instance);
            }
            else if (this.SelectedNode is ZoneViewModelNode zoneNode)
            {
                this.cloudConsoleService.OpenInstanceList(
                    zoneNode.ZoneNode.Zone);
            }
            else if (this.SelectedNode is ProjectViewModelNode projectNode)
            {
                this.cloudConsoleService.OpenInstanceList(
                    projectNode.ProjectNode.Project);
            }
        }

        public void ConfigureIapAccess()
        {
            if (this.SelectedNode is InstanceViewModelNode vmInstanceNode)
            {
                this.cloudConsoleService.OpenIapSecurity(
                    vmInstanceNode.InstanceNode.Instance.ProjectId);
            }
            else if (this.SelectedNode is ZoneViewModelNode zoneNode)
            {
                this.cloudConsoleService.OpenIapSecurity(
                    zoneNode.ZoneNode.Zone.ProjectId);
            }
            else if (this.SelectedNode is ProjectViewModelNode projectNode)
            {
                this.cloudConsoleService.OpenIapSecurity(
                    projectNode.ProjectNode.Project.Name);
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.settings.Dispose();
            this.workspace.Dispose();
        }
    }
}
