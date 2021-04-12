using Google.Solutions.Common.Locator;
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

namespace Google.Solutions.IapDesktop.Application.Views.NewProjectExplorer
{
    // TODO: Add tests for NewProjectExplorerViewModel 
    internal class NewProjectExplorerViewModel : ViewModelBase, IDisposable
    {
        private readonly ApplicationSettingsRepository settingsRepository;
        private readonly IJobService jobService;
        private readonly IProjectModelService projectModelService;

        private string instanceFilter;
        private OperatingSystems operatingSystemsFilter = OperatingSystems.All;

        public NewProjectExplorerViewModel(
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


        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsLinuxIncluded
        {
            get => this.operatingSystemsFilter.HasFlag(OperatingSystems.Linux);
            set
            {
                if (value)
                {
                    this.operatingSystemsFilter |= OperatingSystems.Linux;
                }
                else
                {
                    this.operatingSystemsFilter &= ~OperatingSystems.Linux;
                }

                RaisePropertyChange();
                SaveSettings();
            }
        }
        public bool IsWindowsIncluded
        {
            get => this.operatingSystemsFilter.HasFlag(OperatingSystems.Windows);
            set
            {
                if (value)
                {
                    this.operatingSystemsFilter |= OperatingSystems.Windows;
                }
                else
                {
                    this.operatingSystemsFilter &= ~OperatingSystems.Windows;
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

                // Refresh to cause filter to be reapplied.
                RefreshAsync().ContinueWith(t => { });
            }
        }

        public OperatingSystems OperatingSystemsFilter
        {
            get => this.operatingSystemsFilter;
            set
            {
                this.operatingSystemsFilter = value;

                RaisePropertyChange();

                // Refresh to cause filter to be reapplied.
                RefreshAsync().ContinueWith(t => { });
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public Task RefreshAsync() => RefreshAsync(this.RootNode);

        public async Task RefreshAsync(ViewModelNodeBase node)
        {
            if (!node.CanReload)
            {
                // Try reloading parent instead.
                await RefreshAsync(node.Parent)
                    .ConfigureAwait(true);
            }
            else
            {
                // Clear selection.
                await ClearActiveNodeAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // Force-reload children and discard result.
                await node.GetFilteredNodesAsync(true)
                    .ConfigureAwait(true);
            }
        }

        public Task ClearActiveNodeAsync(CancellationToken token)
        {
            return this.projectModelService.SetActiveNodeAsync(
                (ResourceLocator)null, 
                token);
        }

        public Task SelectNodeAsync(
            ViewModelNodeBase node,
            CancellationToken token)
        {
            return this.projectModelService.SetActiveNodeAsync(
                node.Locator,
                token);
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

        internal abstract class ViewModelNodeBase : ViewModelBase
        {
            protected readonly NewProjectExplorerViewModel viewModel;

            private bool isExpanded;
            private RangeObservableCollection<ViewModelNodeBase> nodes;
            private RangeObservableCollection<ViewModelNodeBase> filteredNodes;

            internal ViewModelNodeBase Parent { get; }

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

            protected virtual IEnumerable<ViewModelNodeBase> ApplyFilter(
                RangeObservableCollection<ViewModelNodeBase> allNodes)
            {
                return allNodes;
            }

            public async Task<ObservableCollection<ViewModelNodeBase>> GetFilteredNodesAsync(
                bool forceReload)
            {
                Debug.Assert(((Control)this.View).InvokeRequired);

                if (this.nodes == null)
                {
                    Debug.Assert(this.filteredNodes == null);

                    //
                    // Load lazily. No locking required as we're
                    // operating on the UI thread.
                    //

                    this.nodes = new RangeObservableCollection<ViewModelNodeBase>();
                    this.nodes.AddRange(
                        await LoadNodesAsync(forceReload)
                            .ConfigureAwait(true));

                    this.filteredNodes = new RangeObservableCollection<ViewModelNodeBase>();
                    this.filteredNodes.AddRange(ApplyFilter(this.nodes));
                }
                else if (forceReload)
                {
                    Debug.Assert(this.filteredNodes != null);
                    
                    var newChildren = await LoadNodesAsync(forceReload)
                        .ConfigureAwait(true);

                    this.nodes.Clear();
                    this.nodes.AddRange(newChildren);

                    this.filteredNodes.Clear();
                    this.filteredNodes.AddRange(ApplyFilter(this.nodes));
                }
                else
                {
                    // Use cached copy.
                }

                Debug.Assert(this.filteredNodes != null);
                Debug.Assert(this.nodes != null);

                return this.filteredNodes;
            }

            protected Task<IEnumerable<ViewModelNodeBase>> LoadNodesAsync(
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

            protected abstract Task<IEnumerable<ViewModelNodeBase>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token);

            //-----------------------------------------------------------------
            // Ctor.
            //-----------------------------------------------------------------

            protected ViewModelNodeBase(
                NewProjectExplorerViewModel viewModel,
                ViewModelNodeBase parent,
                ResourceLocator locator,
                string text,
                bool isLeaf,
                int imageIndex,
                int selectedImageIndex)
            {
                this.viewModel = viewModel;
                this.Parent = parent;
                this.Locator = locator;
                this.Text = text;
                this.IsLeaf = isLeaf;
                this.ImageIndex = imageIndex;
                this.SelectedImageIndex = selectedImageIndex;
            }
        }

        internal class CloudViewModelNode : ViewModelNodeBase
        {
            private readonly IProjectModelService projectModelService;

            public CloudViewModelNode(
                NewProjectExplorerViewModel viewModel,
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

            protected override async Task<IEnumerable<ViewModelNodeBase>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                var model = await this.projectModelService
                    .GetRootNodeAsync(forceReload, token)
                    .ConfigureAwait(true);

                var children = new List<ViewModelNodeBase>();
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

        internal class ProjectViewModelNode : ViewModelNodeBase
        {
            private readonly IProjectExplorerProjectNode modelNode;

            public ProjectViewModelNode(
                NewProjectExplorerViewModel viewModel,
                CloudViewModelNode parent,
                IProjectExplorerProjectNode modelNode)
                : base(
                      viewModel,
                      parent,
                      modelNode.Project,
                      modelNode.DisplayName,
                      false,
                      0,
                      0)
            {
                this.modelNode = modelNode;
            }

            public override bool CanReload => true;

            protected override async Task<IEnumerable<ViewModelNodeBase>> LoadNodesAsync(
                bool forceReload, 
                CancellationToken token)
            {
                var zones = await this.viewModel.projectModelService.GetZoneNodesAsync(
                        this.modelNode.Project,
                        forceReload,
                        token)
                    .ConfigureAwait(true);

                return zones
                    .Select(z => new ZoneViewModelNode(this.viewModel, this, z))
                    .Cast<ViewModelNodeBase>();
            }
        }

        internal class InaccessibleProjectViewModelNode : ViewModelNodeBase
        {
            public InaccessibleProjectViewModelNode(
                NewProjectExplorerViewModel viewModel,
                CloudViewModelNode parent,
                ProjectLocator projectLocator)
                : base(
                      viewModel,
                      parent,
                      projectLocator,
                      $"{projectLocator} (inaccessible)",
                      true,
                      0,
                      0)
            {
            }

            public override bool CanReload => false;

            protected override Task<IEnumerable<ViewModelNodeBase>> LoadNodesAsync(
                bool forceReload, 
                CancellationToken token)
            {
                Debug.Fail("Should not be called since this is a leaf node");
                return Task.FromResult(Enumerable.Empty<ViewModelNodeBase>());
            }
        }

        internal class ZoneViewModelNode : ViewModelNodeBase
        {
            private readonly IProjectExplorerZoneNode modelNode;

            public ZoneViewModelNode(
                NewProjectExplorerViewModel viewModel,
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
            }

            public override bool CanReload => false;

            protected override Task<IEnumerable<ViewModelNodeBase>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                return Task.FromResult(this.modelNode
                    .Instances
                    .Select(i => new InstanceViewModelNode(this.viewModel, this, i))
                    .Cast<ViewModelNodeBase>());
            }

            protected override IEnumerable<ViewModelNodeBase> ApplyFilter(
                RangeObservableCollection<ViewModelNodeBase> allNodes)
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

        internal class InstanceViewModelNode : ViewModelNodeBase
        {
            public IProjectExplorerInstanceNode ModelNode { get; }

            public InstanceViewModelNode(
                NewProjectExplorerViewModel viewModel,
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

            protected override Task<IEnumerable<ViewModelNodeBase>> LoadNodesAsync(
                bool forceReload,
                CancellationToken token)
            {
                Debug.Fail("Should not be called since this is a leaf node");
                return Task.FromResult(Enumerable.Empty<ViewModelNodeBase>());
            }
        }
    }
}
