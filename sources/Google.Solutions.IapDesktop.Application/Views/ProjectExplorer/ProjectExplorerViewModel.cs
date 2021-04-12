using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.ProjectExplorer
{
    internal class ProjectExplorerViewModel : ViewModelBase, IDisposable
    {
        private readonly ApplicationSettingsRepository settingsRepository;
        private readonly IJobService jobService;
        private readonly IProjectModelService projectModelService;

        private ViewModelNode selectedNode;
        private string instanceFilter;
        private OperatingSystems operatingSystemsFilter = OperatingSystems.All;

        public ProjectExplorerViewModel(
            IWin32Window view,
            ApplicationSettingsRepository settingsRepository,
            IJobService jobService,
            IProjectModelService projectModelService)
        {
            this.View = view;
            this.settingsRepository = settingsRepository;
            this.jobService = jobService;
            this.projectModelService = projectModelService;
            this.RootNode = new CloudViewModelNode(
                this,
                projectModelService);

            //
            // Read current settings.
            //
            // NB. Do not hold on to the settings object because it might change.
            //

            this.operatingSystemsFilter = settingsRepository
                .GetSettings()
                .IncludeOperatingSystems
                .EnumValue;

            // TODO: Listen for IsConnected changes
        }

        private async Task<IReadOnlyCollection<ViewModelNode>> ListAllNodesAsync()
        {
            async Task VisitNodeAsync(
                ViewModelNode node,
                List<ViewModelNode> accumulator)
            {
                accumulator.AddRange(
                    await node.GetFilteredNodesAsync(false)
                        .ConfigureAwait(true));
            }

            var list = new List<ViewModelNode>();
            await VisitNodeAsync(this.RootNode, list).ConfigureAwait(true);
            return list;
        }

        private async Task RefreshAsync(ViewModelNode node)
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

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

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
                SaveSettings();
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
                SaveSettings();
            }
        }

        public CloudViewModelNode RootNode { get; }

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

                this.selectedNode = value;
                RaisePropertyChange();

                // 
                // Update active node in model.
                //
                this.projectModelService.SetActiveNodeAsync(
                        value?.Locator,
                        CancellationToken.None)
                    .ContinueWith(_ => { });
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

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

        public async Task AddProjectAsync(ProjectLocator project)
        {
            await this.projectModelService
                .AddProjectAsync(project)
                .ConfigureAwait(true);

            // Make sure the new project is reflected.
            await RefreshAsync(true).ConfigureAwait(true);
        }

        public async Task RemoveProjectAsync(ProjectLocator project)
        {
            // Reset selection to a safe place.
            this.SelectedNode = this.RootNode;

            await this.projectModelService
                .RemoveProjectAsync(project)
                .ConfigureAwait(true);

            // Make sure the new project is reflected.
            await RefreshAsync(true).ConfigureAwait(true);
        }

        public async Task RefreshAsync(
            bool reloadProjects)
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

        public async Task RefreshSelectedNodeAsync(CancellationToken token)
        {
            await RefreshAsync(this.SelectedNode ?? this.RootNode)
                .ConfigureAwait(true);
        }

        public void SaveSettings()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IncludeOperatingSystems.EnumValue = this.operatingSystemsFilter;
            this.settingsRepository.SetSettings(settings);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.settingsRepository.Dispose();
        }

        //---------------------------------------------------------------------
        // Nodes.
        //---------------------------------------------------------------------

        internal abstract class ViewModelNode : ViewModelBase
        {
            protected readonly ProjectExplorerViewModel viewModel;

            private bool isExpanded;
            private RangeObservableCollection<ViewModelNode> nodes;
            private RangeObservableCollection<ViewModelNode> filteredNodes;

            internal ViewModelNode Parent { get; }

            internal bool IsLoaded => this.nodes != null;

            //-----------------------------------------------------------------
            // Observable properties.
            //-----------------------------------------------------------------

            public abstract bool CanReload { get; }
            public ResourceLocator Locator { get; }
            public string Text { get; }
            public bool IsLeaf { get; }
            public int ImageIndex { get; }
            public int SelectedImageIndex { get; }

            public bool IsExpanded
            {
                get => this.isExpanded;
                set
                {
                    this.isExpanded = value;
                    RaisePropertyChange();
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
                Debug.Assert(!((Control)this.View).InvokeRequired);

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

            protected Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload)
            {
                //
                // Wrap loading task in a job since it might kick of
                // I/O (if data has not been cached yet).
                //
                return this.viewModel.jobService.RunInBackground(
                    new JobDescription(
                        $"Loading {this.Text}...",
                        JobUserFeedbackType.BackgroundFeedback),
                    token => LoadNodesAsync(forceReload, token));
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

                this.filteredNodes.Clear();
                this.filteredNodes.AddRange(ApplyFilter(this.nodes));

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
                int imageIndex,
                int selectedImageIndex)
            {
                this.viewModel = viewModel;
                this.View = viewModel.View;
                this.Parent = parent;
                this.Locator = locator;
                this.Text = text;
                this.IsLeaf = isLeaf;
                this.ImageIndex = imageIndex;
                this.SelectedImageIndex = selectedImageIndex;
            }
        }

        internal class CloudViewModelNode : ViewModelNode
        {
            private readonly IProjectModelService projectModelService;

            public CloudViewModelNode(
                ProjectExplorerViewModel viewModel,
                IProjectModelService projectModelService)
                : base(
                      viewModel,
                      null,
                      null,
                      "Google Cloud",
                      false,
                      0,
                      0)
            {
                this.projectModelService = projectModelService;
            }

            public override bool CanReload => true;

            protected override async Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                var model = await this.projectModelService
                    .GetRootNodeAsync(forceReload, token)
                    .ConfigureAwait(true);

                var children = new List<ViewModelNode>();
                children.AddRange(model.Projects
                    .Select(m => new ProjectViewModelNode(
                        this.viewModel,
                        this,
                        m)));
                children.AddRange(model.InaccessibleProjects
                    .Select(m => new InaccessibleProjectViewModelNode(
                        this.viewModel,
                        this,
                        m)));

                return children;
            }
        }

        internal class ProjectViewModelNode : ViewModelNode
        {
            public IProjectExplorerProjectNode ModelNode { get; }

            public ProjectViewModelNode(
                ProjectExplorerViewModel viewModel,
                CloudViewModelNode parent,
                IProjectExplorerProjectNode modelNode)
                : base(
                      viewModel,
                      parent,
                      modelNode.Project,
                      modelNode.Project.Name == modelNode.DisplayName
                        ? modelNode.Project.Name
                        : $"{modelNode.DisplayName} ({modelNode.Project.Name})",
                      false,
                      0,
                      0)
            {
                this.ModelNode = modelNode;
                this.IsExpanded = true;
            }

            public override bool CanReload => true;

            protected override async Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                var zones = await this.viewModel.projectModelService.GetZoneNodesAsync(
                        this.ModelNode.Project,
                        forceReload,
                        token)
                    .ConfigureAwait(true);

                return zones
                    .Select(z => new ZoneViewModelNode(this.viewModel, this, z))
                    .Cast<ViewModelNode>();
            }
        }

        internal class InaccessibleProjectViewModelNode : ViewModelNode
        {
            public ProjectLocator Project { get; }

            public InaccessibleProjectViewModelNode(
                ProjectExplorerViewModel viewModel,
                CloudViewModelNode parent,
                ProjectLocator projectLocator)
                : base(
                      viewModel,
                      parent,
                      projectLocator,
                      $"{projectLocator.Name} (inaccessible)",
                      true,
                      0,
                      0)
            {
                this.Project = projectLocator;
            }

            public override bool CanReload => false;

            protected override Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                Debug.Fail("Should not be called since this is a leaf node");
                return Task.FromResult(Enumerable.Empty<ViewModelNode>());
            }
        }

        internal class ZoneViewModelNode : ViewModelNode
        {
            private readonly IProjectExplorerZoneNode modelNode;

            public ZoneViewModelNode(
                ProjectExplorerViewModel viewModel,
                ProjectViewModelNode parent,
                IProjectExplorerZoneNode modelNode)
                : base(
                      viewModel,
                      parent,
                      modelNode.Zone,
                      modelNode.DisplayName,
                      false,
                      0,
                      0)
            {
                this.modelNode = modelNode;
                this.IsExpanded = true;
            }

            public override bool CanReload => false;

            protected override Task<IEnumerable<ViewModelNode>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                return Task.FromResult(this.modelNode
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
                                i.ModelNode.DisplayName.Contains(this.viewModel.instanceFilter))
                    // TODO: Remove cast
                    .Where(i => (((InstanceNode)i.ModelNode).OperatingSystem &
                                this.viewModel.OperatingSystemsFilter) != 0);
            }
        }

        internal class InstanceViewModelNode : ViewModelNode
        {
            public IProjectExplorerInstanceNode ModelNode { get; }

            public InstanceViewModelNode(
                ProjectExplorerViewModel viewModel,
                ZoneViewModelNode parent,
                IProjectExplorerInstanceNode modelNode)
                : base(
                      viewModel,
                      parent,
                      modelNode.Instance,
                      modelNode.DisplayName,
                      true,
                      0,
                      0)
            {
                this.ModelNode = modelNode;

                // TODO: Set icon based on OS, state
                // TODO: Set icon based on IsConnected, and make observable
            }

            public override bool CanReload => false;

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
