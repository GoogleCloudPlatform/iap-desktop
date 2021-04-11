using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
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
    internal class NewProjectExplorerViewModel : ViewModelBase, IDisposable
    {
        private readonly IJobService jobService;
        private readonly IProjectModelService projectModelService;

        public NewProjectExplorerViewModel(
            IWin32Window view,
            IJobService jobService,
            IProjectModelService projectModelService)
        {
            this.View = view;
            this.jobService = jobService;
            this.projectModelService = projectModelService;
            this.RootNode = new CloudViewModelNode(
                this, 
                projectModelService);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public CloudViewModelNode RootNode { get; }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public Task RefreshAsync()
            => RefreshAsync(this.RootNode);

        public async Task RefreshAsync(ViewModelNodeBase node)
        {
            if (!node.CanReload)
            {
                // Try reloading parent instead.
                await RefreshAsync(node.Parent).ConfigureAwait(true);
            }
            else
            {
                // Force-reload children and discard result.
                await node.GetChildren(true).ConfigureAwait(true);
            }
        }

        public Task SetActiveNodeAsync(
            ViewModelNodeBase node,
            CancellationToken token)
        {
            return this.projectModelService.SetActiveNodeAsync(
                node.Locator, 
                token);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        internal abstract class ViewModelNodeBase : ViewModelBase
        {
            protected readonly NewProjectExplorerViewModel viewModel;

            private bool isExpanded;
            private RangeObservableCollection<ViewModelNodeBase> children;

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

            public async Task<ObservableCollection<ViewModelNodeBase>> GetChildren(
                bool forceReload)
            {
                Debug.Assert(((Control)this.View).InvokeRequired);

                if (this.children == null)
                {
                    //
                    // Load lazily. No locking required as we're
                    // operating on the UI thread.
                    //

                    this.children = new RangeObservableCollection<ViewModelNodeBase>();
                    this.children.AddRange(
                        await LoadChildrenInJob(forceReload)
                            .ConfigureAwait(true));
                }
                else if (forceReload)
                {
                    var newChildren = await LoadChildrenInJob(forceReload)
                        .ConfigureAwait(true);

                    this.children.Clear();
                    this.children.AddRange(newChildren);
                }

                Debug.Assert(this.children != null);

                return this.children;
            }

            protected Task<IEnumerable<ViewModelNodeBase>> LoadChildrenInJob(
                bool forceReload)
            {
                return this.viewModel.jobService.RunInBackground(
                    new JobDescription($"Loading {this.Text}..."),
                    token => LoadChildren(forceReload, token));
            }

            protected abstract Task<IEnumerable<ViewModelNodeBase>> LoadChildren(
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

            protected override async Task<IEnumerable<ViewModelNodeBase>> LoadChildren(
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

            protected override async Task<IEnumerable<ViewModelNodeBase>> LoadChildren(
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

            protected override Task<IEnumerable<ViewModelNodeBase>> LoadChildren(
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

            protected override Task<IEnumerable<ViewModelNodeBase>> LoadChildren(
                bool forceReload,
                CancellationToken token)
            {
                Debug.Assert(!forceReload);
                return Task.FromResult(this.modelNode
                    .Instances
                    .Select(i => new InstanceViewModelNode(this.viewModel, this, i))
                    .Cast<ViewModelNodeBase>());
            }
        }

        internal class InstanceViewModelNode : ViewModelNodeBase
        {
            private readonly IProjectExplorerInstanceNode modelNode;

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
                this.modelNode = modelNode;
            }

            public override bool CanReload => false;

            protected override Task<IEnumerable<ViewModelNodeBase>> LoadChildren(
                bool forceReload,
                CancellationToken token)
            {
                Debug.Fail("Should not be called since this is a leaf node");
                return Task.FromResult(Enumerable.Empty<ViewModelNodeBase>());
            }
        }
    }
}
