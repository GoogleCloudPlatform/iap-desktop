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
using Google.Solutions.Apis.Locator;
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

#nullable disable

namespace Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer
{
    public class ProjectExplorerViewModel : ViewModelBase, IDisposable
    {
        private readonly IJobService jobService;
        private readonly IProjectWorkspace workspace;
        private readonly ISessionBroker sessionBroker;
        private readonly ICloudConsoleClient cloudConsoleService;

        private ViewModelNode selectedNode;
        private string instanceFilter;
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
                // Try reloading parent instead.
                await RefreshAsync(node.Parent)
                    .ConfigureAwait(true);
            }
            else
            {
                // Force-reload children and discard result.
                await node.GetFilteredNodesAsync(true)
                    .ConfigureAwait(true);
            }
        }

        internal ProjectExplorerViewModel(
            IProjectExplorerSettings settings,
            IJobService jobService,
            IEventQueue eventService,
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

            eventService.Subscribe<SessionStartedEvent>(
                e => UpdateInstanceAsync(e.Instance, i => i.IsConnected = true));
            eventService.Subscribe<SessionEndedEvent>(
                e => UpdateInstanceAsync(e.Instance, i => i.IsConnected = false));
            eventService.Subscribe<InstanceStateChangedEvent>(
                async e =>
                {
                    //
                    // Refresh the instance node (or rather, its enclosing project)
                    // to ensure that both the UI and the underlying project model
                    // updated.
                    //
                    var node = await TryFindInstanceNodeAsync(e.Instance)
                        .ConfigureAwait(true);
                    if (node != null)
                    {
                        await RefreshAsync(node).ConfigureAwait(true);
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

        private async Task UpdateInstanceAsync(
            InstanceLocator locator,
            Action<InstanceViewModelNode> action)
        {
            if (await TryFindInstanceNodeAsync(locator).ConfigureAwait(true)
                is InstanceViewModelNode instance)
            {
                action(instance);
            }
        }

        private async Task<InstanceViewModelNode> TryFindInstanceNodeAsync(
            InstanceLocator locator)
        {
            var project = (await this.RootNode
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true))
                .FirstOrDefault(p => p.Locator.ProjectId == locator.ProjectId);
            if (project == null)
            {
                return null;
            }

            var zone = (await project
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true))
                .FirstOrDefault(z => z.Locator.Name == locator.Zone);
            if (zone == null)
            {
                return null;
            }

            return (InstanceViewModelNode)(await zone
                .GetFilteredNodesAsync(false)
                .ConfigureAwait(true))
                .FirstOrDefault(i => i.Locator.Name == locator.Name);
        }

        internal IReadOnlyCollection<IProjectModelProjectNode> Projects
        {
            get
            {
                var modelNode = (IProjectModelCloudNode)this.RootNode.ModelNode;
                var projects = modelNode?.Projects ?? Enumerable.Empty<IProjectModelProjectNode>();

                return projects
                    .EnsureNotNull()
                    .ToList();
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

        public string InstanceFilter
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

        public ViewModelNode SelectedNode
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
                        CancellationToken.None)
                    .ContinueWith(_ => { });
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
            var nodes = await this.RootNode.GetFilteredNodesAsync(false)
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
                    .GetFilteredNodesAsync(false)
                    .ConfigureAwait(true);

                await Task
                    .WhenAll(projects
                        .Where(p => p.IsLoaded && p.CanReload)
                        .Select(p => p.GetFilteredNodesAsync(true)))
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

        //---------------------------------------------------------------------
        // Nodes.
        //---------------------------------------------------------------------

        public abstract class ViewModelNode : ViewModelBase
        {
            protected readonly ProjectExplorerViewModel viewModel;

            private bool isExpanded;
            private readonly int defaultImageIndex;
            private RangeObservableCollection<ViewModelNode> nodes;
            private RangeObservableCollection<ViewModelNode> filteredNodes;

            internal ViewModelNode Parent { get; }

            internal bool IsLoaded => this.nodes != null;

            protected virtual void OnExpandedChanged() { }

            //-----------------------------------------------------------------
            // Observable properties.
            //-----------------------------------------------------------------

            public abstract bool CanReload { get; }
            public abstract IProjectModelNode ModelNode { get; }
            public ResourceLocator Locator { get; }
            public string Text { get; }
            public bool IsLeaf { get; }

            public virtual int ImageIndex => this.defaultImageIndex;

            public bool IsExpanded
            {
                get => this.isExpanded;
                set
                {
                    this.isExpanded = value;
                    RaisePropertyChange();
                    OnExpandedChanged();
                }
            }

            //-----------------------------------------------------------------
            // Children.
            //-----------------------------------------------------------------

            protected virtual IEnumerable<ViewModelNode> ApplyFilter(
                RangeObservableCollection<ViewModelNode> allNodes)
            {
                return allNodes;
            }

            public async Task<ObservableCollection<ViewModelNode>> GetFilteredNodesAsync(
                bool forceReload)
            {
                Debug.Assert(!((Control)this.viewModel.View).InvokeRequired);

                if (this.nodes == null)
                {
                    Debug.Assert(this.filteredNodes == null);

                    //
                    // Load lazily. No locking required as we're
                    // operating on the UI thread.
                    //

                    var loadedNodes = await LoadNodesAsync(forceReload)
                        .ConfigureAwait(true);

                    this.nodes = new RangeObservableCollection<ViewModelNode>();
                    this.filteredNodes = new RangeObservableCollection<ViewModelNode>();

                    this.nodes.AddRange(loadedNodes);

                    ReapplyFilter();
                }
                else if (forceReload)
                {
                    Debug.Assert(this.filteredNodes != null);

                    var loadedNodes = await LoadNodesAsync(forceReload)
                        .ConfigureAwait(true);

                    this.nodes.Clear();
                    this.nodes.AddRange(loadedNodes);

                    ReapplyFilter();
                }
                else
                {
                    // Use cached copy.
                }

                Debug.Assert(this.filteredNodes != null);
                Debug.Assert(this.nodes != null);

                return this.filteredNodes;
            }

            protected async Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload)
            {
                using (this.viewModel.EnableLoadingStatus())
                {
                    //
                    // Wrap loading task in a job since it might kick of
                    // I/O (if data has not been cached yet).
                    //
                    return await this.viewModel.jobService.RunAsync(
                            new JobDescription(
                                $"Loading {this.Text}...",
                                JobUserFeedbackType.BackgroundFeedback),
                            token => LoadNodesAsync(forceReload, token))
                        .ConfigureAwait(true);
                }
            }

            protected abstract Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token);

            internal bool DebugIsValidNode(ViewModelNode node)
            {
                if (node == this)
                {
                    return true;
                }
                else if (this.filteredNodes == null)
                {
                    return false;
                }
                else
                {
                    return this.filteredNodes.Any(n => n.DebugIsValidNode(n));
                }
            }

            internal void ReapplyFilter()
            {
                Debug.Assert(this.IsLoaded);

                //
                // Avoid clearing and re-applying filter it's not really
                // necessary. Excessive re-binding can otherwise cause
                // significant CPU load.
                //
                var newFilteredNodes = ApplyFilter(this.nodes);
                if (!this.filteredNodes.SequenceEqual(newFilteredNodes))
                {
                    this.filteredNodes.Clear();
                    this.filteredNodes.AddRange(newFilteredNodes);
                }

                foreach (var n in this.nodes.Where(n => n.IsLoaded))
                {
                    n.ReapplyFilter();
                }
            }

            //-----------------------------------------------------------------
            // Ctor.
            //-----------------------------------------------------------------

            protected ViewModelNode(
                ProjectExplorerViewModel viewModel,
                ViewModelNode parent,
                ResourceLocator locator,
                string text,
                bool isLeaf,
                int defaultImageIndex)
            {
                this.viewModel = viewModel;
                this.Parent = parent;
                this.Locator = locator;
                this.Text = text;
                this.IsLeaf = isLeaf;
                this.defaultImageIndex = defaultImageIndex;
            }
        }

        public class CloudViewModelNode : ViewModelNode
        {
            private const int DefaultIconIndex = 0;
            private IProjectModelCloudNode cloudNode; // Loaded lazily.

            public CloudViewModelNode(
                ProjectExplorerViewModel viewModel)
                : base(
                      viewModel,
                      null,
                      null,
                      "Google Cloud",
                      false,
                      DefaultIconIndex)
            {
            }

            public override IProjectModelNode ModelNode => this.cloudNode;

            public override bool CanReload => true;

            protected override async Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                this.cloudNode = await this.viewModel.workspace
                    .GetRootNodeAsync(forceReload, token)
                    .ConfigureAwait(true);

                var children = new List<ViewModelNode>();
                children.AddRange(this.cloudNode.Projects
                    .Select(m => new ProjectViewModelNode(
                        this.viewModel,
                        this,
                        m))
                    .OrderBy(n => n.Text));

                return children;
            }
        }

        internal class ProjectViewModelNode : ViewModelNode
        {
            private const int DefaultIconIndex = 1;
            public IProjectModelProjectNode ProjectNode { get; }

            private static string CreateDisplayName(IProjectModelProjectNode node)
            {
                if (!node.IsAccesible)
                {
                    return $"inaccessible project ({node.Project.Name})";
                }
                else if (node.Project.Name == node.DisplayName)
                {
                    return node.Project.Name;
                }
                else
                {
                    return $"{node.DisplayName} ({node.Project.Name})";
                }
            }

            public ProjectViewModelNode(
                ProjectExplorerViewModel viewModel,
                CloudViewModelNode parent,
                IProjectModelProjectNode modelNode)
                : base(
                      viewModel,
                      parent,
                      modelNode.Project,
                      CreateDisplayName(modelNode),
                      false,
                      DefaultIconIndex)
            {
                this.ProjectNode = modelNode;
                this.IsExpanded = !viewModel.settings.CollapsedProjects.Contains(modelNode.Project);
            }

            protected override void OnExpandedChanged()
            {
                if (this.IsExpanded)
                {
                    this.viewModel.settings.CollapsedProjects.Remove(this.ProjectNode.Project);
                }
                else
                {
                    this.viewModel.settings.CollapsedProjects.Add(this.ProjectNode.Project);
                }

                base.OnExpandedChanged();
            }

            public override IProjectModelNode ModelNode => this.ProjectNode;

            public override bool CanReload => true;

            protected override async Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                try
                {
                    var zones = await this.viewModel.workspace.GetZoneNodesAsync(
                            this.ProjectNode.Project,
                            forceReload,
                            token)
                        .ConfigureAwait(true);

                    return zones
                        .Select(z => new ZoneViewModelNode(this.viewModel, this, z))
                        .Cast<ViewModelNode>();
                }
                catch (Exception e) when (e.Is<ResourceAccessDeniedException>())
                {
                    //
                    // Letting these exception propagate could cause a flurry
                    // of error messages when multiple projects have become
                    // inaccessible. So it's best to interpret this error as
                    // "cannot list any VMs" and return an empty list.
                    //
                    return Enumerable.Empty<ZoneViewModelNode>();
                }
            }
        }

        internal class ZoneViewModelNode : ViewModelNode
        {
            private const int DefaultIconIndex = 3;

            public IProjectModelZoneNode ZoneNode { get; }

            public ZoneViewModelNode(
                ProjectExplorerViewModel viewModel,
                ProjectViewModelNode parent,
                IProjectModelZoneNode modelNode)
                : base(
                      viewModel,
                      parent,
                      modelNode.Zone,
                      modelNode.DisplayName,
                      false,
                      DefaultIconIndex)
            {
                this.ZoneNode = modelNode;
                this.IsExpanded = true;
            }

            public override IProjectModelNode ModelNode => this.ZoneNode;

            public override bool CanReload => false;

            protected override Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                return Task.FromResult(this.ZoneNode
                    .Instances
                    .Select(i => new InstanceViewModelNode(this.viewModel, this, i))
                    .Cast<ViewModelNode>());
            }

            protected override IEnumerable<ViewModelNode> ApplyFilter(
                RangeObservableCollection<ViewModelNode> allNodes)
            {
                return allNodes
                    .Cast<InstanceViewModelNode>()
                    .Where(i => this.viewModel.InstanceFilter == null ||
                                i.InstanceNode.DisplayName.Contains(this.viewModel.instanceFilter))
                    .Where(i => (i.InstanceNode.OperatingSystem &
                                this.viewModel.OperatingSystemsFilter) != 0);
            }
        }

        internal class InstanceViewModelNode : ViewModelNode
        {
            internal const int WindowsDisconnectedIconIndex = 4;
            internal const int WindowsConnectedIconIndex = 5;
            internal const int StoppedIconIndex = 6;
            internal const int LinuxDisconnectedIconIndex = 7;
            internal const int LinuxConnectedIconIndex = 8;

            private bool isConnected = false;

            public IProjectModelInstanceNode InstanceNode { get; }

            public InstanceViewModelNode(
                ProjectExplorerViewModel viewModel,
                ZoneViewModelNode parent,
                IProjectModelInstanceNode modelNode)
                : base(
                      viewModel,
                      parent,
                      modelNode.Instance,
                      modelNode.DisplayName,
                      true,
                      -1)
            {
                this.InstanceNode = modelNode;
                this.IsConnected = viewModel.sessionBroker.IsConnected(modelNode.Instance);
            }

            public override IProjectModelNode ModelNode => this.InstanceNode;

            public override bool CanReload => false;

            public override int ImageIndex
            {
                get
                {
                    if (this.IsConnected)
                    {
                        return this.InstanceNode.OperatingSystem == OperatingSystems.Windows
                            ? WindowsConnectedIconIndex
                            : LinuxConnectedIconIndex;
                    }
                    else if (!this.InstanceNode.IsRunning)
                    {
                        return StoppedIconIndex;
                    }
                    else
                    {
                        return this.InstanceNode.OperatingSystem == OperatingSystems.Windows
                            ? WindowsDisconnectedIconIndex
                            : LinuxDisconnectedIconIndex;
                    }
                }
            }

            public bool IsConnected
            {
                get => this.isConnected;
                set
                {
                    this.isConnected = value;
                    RaisePropertyChange();
                    RaisePropertyChange((InstanceViewModelNode n) => n.ImageIndex);
                }
            }

            protected override Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                Debug.Fail("Should not be called since this is a leaf node");
                return Task.FromResult(Enumerable.Empty<ViewModelNode>());
            }
        }
    }
}
