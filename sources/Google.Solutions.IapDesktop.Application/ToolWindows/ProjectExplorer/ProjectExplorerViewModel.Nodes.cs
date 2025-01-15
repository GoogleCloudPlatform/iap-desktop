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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
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
    public partial class ProjectExplorerViewModel
    {
        public abstract class ViewModelNode : ViewModelBase
        {
            private bool isExpanded;
            private readonly int defaultImageIndex;

            //
            // Child nodes, loaded lazily.
            //

            private RangeObservableCollection<ViewModelNode>? children;
            private RangeObservableCollection<ViewModelNode>? filteredChildren;

            //-----------------------------------------------------------------
            // Properties.
            //-----------------------------------------------------------------

            internal ViewModelNode? Parent { get; }
            internal abstract bool CanReload { get; }
            protected abstract ProjectExplorerViewModel ViewModel { get; }
            public abstract IProjectModelNode ModelNode { get; }
            public ComputeEngineLocator? Locator { get; }
            public string Text { get; }
            public bool IsLeaf { get; }

            public virtual int ImageIndex
            {
                get => this.defaultImageIndex;
            }

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

            internal virtual bool IsLoaded
            {
                get => this.children != null;
            }

            /// <summary>
            /// List all children that have been loaded, ignoring any filter.
            /// </summary>
            internal IEnumerable<ViewModelNode> LoadedChildren
            {
                get => this.children ?? Enumerable.Empty<ViewModelNode>();
            }

            /// <summary>
            /// List all descendents that have been loaded, ignoring any filter.
            /// </summary>
            internal IEnumerable<ViewModelNode> LoadedDescendents
            {
                get
                {
                    var accumulator = new List<ViewModelNode>();
                    foreach (var child in this.LoadedChildren)
                    {
                        accumulator.Add(child);
                        accumulator.AddRange(child.LoadedDescendents);
                    }

                    return accumulator;
                }
            }

            //-----------------------------------------------------------------
            // Children.
            //-----------------------------------------------------------------

            protected virtual void OnExpandedChanged() { }

            protected virtual IEnumerable<ViewModelNode> ApplyFilter(
                RangeObservableCollection<ViewModelNode> allNodes)
            {
                return allNodes;
            }

            /// <summary>
            /// List all children that match the current filter. Causes
            /// children to be loaded if necessary.
            /// </summary>
            public async Task<ObservableCollection<ViewModelNode>> GetFilteredChildrenAsync(
                bool forceReload)
            {
                Debug.Assert(!((Control)this.ViewModel.View!).InvokeRequired);
                Debug.Assert(!this.IsLeaf);

                if (this.children == null)
                {
                    Debug.Assert(this.filteredChildren == null);

                    //
                    // Load lazily. No locking required as we're
                    // operating on the UI thread.
                    //

                    var loadedNodes = await LoadChildrenAsync(forceReload)
                        .ConfigureAwait(true);

                    this.children = new RangeObservableCollection<ViewModelNode>();
                    this.filteredChildren = new RangeObservableCollection<ViewModelNode>();

                    this.children.AddRange(loadedNodes);

                    ReapplyFilter();
                }
                else if (forceReload)
                {
                    Debug.Assert(this.filteredChildren != null);

                    var loadedNodes = await LoadChildrenAsync(forceReload)
                        .ConfigureAwait(true);

                    this.children.Clear();
                    this.children.AddRange(loadedNodes);

                    ReapplyFilter();
                }
                else
                {
                    // Use cached copy.
                }

                Debug.Assert(this.filteredChildren != null);
                Debug.Assert(this.children != null);

                return this.filteredChildren!;
            }

            /// <summary>
            /// Load children in a job.
            /// </summary>
            protected async Task<IEnumerable<ViewModelNode>> LoadChildrenAsync(
                bool forceReload)
            {
                using (this.ViewModel.EnableLoadingStatus())
                {
                    //
                    // Wrap loading task in a job since it might kick of
                    // I/O (if data has not been cached yet).
                    //
                    return await this.ViewModel.jobService.RunAsync(
                            new JobDescription(
                                $"Loading {this.Text}...",
                                JobUserFeedbackType.BackgroundFeedback),
                            token => LoadChildrenAsync(forceReload, token))
                        .ConfigureAwait(true);
                }
            }

            /// <summary>
            /// Load children.
            /// </summary>
            protected abstract Task<IEnumerable<ViewModelNode>> LoadChildrenAsync(
                bool forceReload,
                CancellationToken token);

            internal bool DebugIsValidNode(ViewModelNode node)
            {
                if (node == this)
                {
                    return true;
                }
                else if (this.filteredChildren == null)
                {
                    return false;
                }
                else
                {
                    return this.filteredChildren.Any(n => n.DebugIsValidNode(n));
                }
            }

            internal void ReapplyFilter()
            {
                Debug.Assert(this.IsLoaded);
                if (this.IsLeaf)
                {
                    return;
                }

                Debug.Assert(this.children != null);
                Debug.Assert(this.filteredChildren != null);

                //
                // Avoid clearing and re-applying filter it's not really
                // necessary. Excessive re-binding can otherwise cause
                // significant CPU load.
                //
                var newFilteredNodes = ApplyFilter(this.children!);
                if (!this.filteredChildren!.SequenceEqual(newFilteredNodes))
                {
                    this.filteredChildren!.Clear();
                    this.filteredChildren.AddRange(newFilteredNodes);
                }

                foreach (var n in this.children.Where(n => n.IsLoaded))
                {
                    n.ReapplyFilter();
                }
            }

            //-----------------------------------------------------------------
            // Ctor.
            //-----------------------------------------------------------------

            protected ViewModelNode(
                ViewModelNode? parent,
                ComputeEngineLocator? locator,
                string text,
                bool isLeaf,
                int defaultImageIndex)
            {
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
            private IProjectModelCloudNode? cloudNode; // Loaded lazily.
            protected override ProjectExplorerViewModel ViewModel { get; }

            public CloudViewModelNode(
                ProjectExplorerViewModel viewModel)
                : base(
                      null,
                      null,
                      "Google Cloud",
                      false,
                      DefaultIconIndex)
            {
                this.ViewModel = viewModel;
            }

            public override IProjectModelNode ModelNode
            {
                get
                {
                    Invariant.ExpectNotNull(this.cloudNode, "Loading completed");
                    return this.cloudNode!;
                }
            }

            internal override bool CanReload
            {
                get => true;
            }

            protected override async Task<IEnumerable<ViewModelNode>> LoadChildrenAsync(
                bool forceReload,
                CancellationToken token)
            {
                this.cloudNode = await this.ViewModel.workspace
                    .GetRootNodeAsync(forceReload, token)
                    .ConfigureAwait(true);

                var children = new List<ViewModelNode>();
                children.AddRange(this.cloudNode.Projects
                    .Select(m => new ProjectViewModelNode(
                        this.ViewModel,
                        this,
                        m))
                    .OrderBy(n => n.Text));

                return children;
            }
        }

        internal class ProjectViewModelNode : ViewModelNode
        {
            private const int DefaultIconIndex = 1;
            internal IProjectModelProjectNode ProjectNode { get; }
            protected override ProjectExplorerViewModel ViewModel { get; }

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
                      parent,
                      modelNode.Project,
                      CreateDisplayName(modelNode),
                      false,
                      DefaultIconIndex)
            {
                this.ProjectNode = modelNode;
                this.ViewModel = viewModel;
                this.IsExpanded = !viewModel.settings.CollapsedProjects.Contains(modelNode.Project);
            }

            protected override void OnExpandedChanged()
            {
                if (this.IsExpanded)
                {
                    this.ViewModel.settings.CollapsedProjects.Remove(this.ProjectNode.Project);
                }
                else
                {
                    this.ViewModel.settings.CollapsedProjects.Add(this.ProjectNode.Project);
                }

                base.OnExpandedChanged();
            }

            public override IProjectModelNode ModelNode
            {
                get => this.ProjectNode;
            }

            internal override bool CanReload
            {
                get => true;
            }

            protected override async Task<IEnumerable<ViewModelNode>> LoadChildrenAsync(
                bool forceReload,
                CancellationToken token)
            {
                try
                {
                    var zones = await this.ViewModel.workspace.GetZoneNodesAsync(
                            this.ProjectNode.Project,
                            forceReload,
                            token)
                        .ConfigureAwait(true);

                    return zones
                        .Select(z => new ZoneViewModelNode(this.ViewModel, this, z))
                        .Cast<ViewModelNode>();
                }
                catch (Exception e) when (
                    e.Is<ResourceAccessDeniedException>() ||
                    e.Is<ResourceNotFoundException>())
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

            internal IProjectModelZoneNode ZoneNode { get; }
            protected override ProjectExplorerViewModel ViewModel { get; }

            public ZoneViewModelNode(
                ProjectExplorerViewModel viewModel,
                ProjectViewModelNode parent,
                IProjectModelZoneNode modelNode)
                : base(
                      parent,
                      modelNode.Zone,
                      modelNode.DisplayName,
                      false,
                      DefaultIconIndex)
            {
                this.ZoneNode = modelNode;
                this.ViewModel = viewModel;
                this.IsExpanded = true;
            }

            public override IProjectModelNode ModelNode
            {
                get => this.ZoneNode;
            }

            internal override bool CanReload
            {
                get => false;
            }

            protected override Task<IEnumerable<ViewModelNode>> LoadChildrenAsync(
                bool forceReload,
                CancellationToken token)
            {
                return Task.FromResult(this.ZoneNode
                    .Instances
                    .Select(i => new InstanceViewModelNode(this.ViewModel, this, i))
                    .Cast<ViewModelNode>());
            }

            protected override IEnumerable<ViewModelNode> ApplyFilter(
                RangeObservableCollection<ViewModelNode> allNodes)
            {
                return allNodes
                    .Cast<InstanceViewModelNode>()
                    .Where(i => this.ViewModel.InstanceFilter == null ||
                                i.InstanceNode.DisplayName.Contains(this.ViewModel.instanceFilter))
                    .Where(i => (i.InstanceNode.OperatingSystem &
                                this.ViewModel.OperatingSystemsFilter) != 0);
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

            internal IProjectModelInstanceNode InstanceNode { get; }
            protected override ProjectExplorerViewModel ViewModel { get; }

            public InstanceViewModelNode(
                ProjectExplorerViewModel viewModel,
                ZoneViewModelNode parent,
                IProjectModelInstanceNode modelNode)
                : base(
                      parent,
                      modelNode.Instance,
                      modelNode.DisplayName,
                      true,
                      -1)
            {
                this.InstanceNode = modelNode;
                this.ViewModel = viewModel;
                this.IsConnected = viewModel.sessionBroker.IsConnected(modelNode.Instance);
            }

            public override IProjectModelNode ModelNode
            {
                get => this.InstanceNode;
            }

            internal override bool CanReload
            {
                get => false;
            }

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

            internal override bool IsLoaded
            {
                get => true;
            }

            protected override Task<IEnumerable<ViewModelNode>> LoadChildrenAsync(
                bool forceReload,
                CancellationToken token)
            {
                Debug.Fail("Should not be called since this is a leaf node");
                return Task.FromResult(Enumerable.Empty<ViewModelNode>());
            }
        }
    }
}
