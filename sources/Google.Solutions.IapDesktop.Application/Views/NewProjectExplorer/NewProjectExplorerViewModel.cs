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
        private readonly IProjectModelService projectModelService;

        public NewProjectExplorerViewModel(
            IWin32Window view,
            IProjectModelService projectModelService)
        {
            this.View = view;
            this.projectModelService = projectModelService;
            this.RootNode = new CloudViewModelNode(
                view, 
                projectModelService);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public CloudViewModelNode RootNode { get; }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public Task RefreshAsync(IJobService jobService)
            => RefreshAsync(jobService, this.RootNode);

        public async Task RefreshAsync(
            IJobService jobService,
            ViewModelNodeBase node)
        {
            if (!node.CanReload)
            {
                // Try reloading parent instead.
                await RefreshAsync(jobService, node.Parent)
                    .ConfigureAwait(true);
            }
            else
            {
                // Force-reload children and discard result.
                await node
                    .GetChildren(jobService, true)
                    .ConfigureAwait(true);
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
                IJobService jobService,
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
                        await LoadChildrenInJob(jobService, forceReload)
                            .ConfigureAwait(true));
                }
                else if (forceReload)
                {
                    var newChildren = 
                        await LoadChildrenInJob(jobService, forceReload)
                            .ConfigureAwait(true);

                    this.children.Clear();
                    this.children.AddRange(newChildren);
                }

                Debug.Assert(this.children != null);

                return this.children;
            }

            protected Task<IEnumerable<ViewModelNodeBase>> LoadChildrenInJob(
                IJobService jobService,
                bool forceReload)
            {
                return jobService.RunInBackground(
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
                ViewModelNodeBase parent,
                IWin32Window view,
                ResourceLocator locator,
                string text,
                bool isLeaf,
                int imageIndex,
                int selectedImageIndex)
            {
                this.Parent = parent;
                this.View = view;
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
                IWin32Window view,
                IProjectModelService projectModelService)
                : base(
                      null,
                      view, 
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
                    .Select(m => new ProjectViewModelNode(this, m, this.projectModelService)));
                children.AddRange(model.InaccessibleProjects
                    .Select(m => new InaccessibleProjectViewModelNode(this, m)));

                return children;
            }
        }

        internal class ProjectViewModelNode : ViewModelNodeBase
        {
            private readonly IProjectExplorerProjectNode modelNode;
            private readonly IProjectModelService projectModelService;

            public ProjectViewModelNode(
                CloudViewModelNode parent,
                IProjectExplorerProjectNode modelNode,
                IProjectModelService projectModelService)
                : base(
                      parent,
                      parent.View,
                      modelNode.Project,
                      modelNode.DisplayName,
                      false,
                      0,
                      0)
            {
                this.modelNode = modelNode;
                this.projectModelService = projectModelService;
            }

            public override bool CanReload => true;

            protected override async Task<IEnumerable<ViewModelNodeBase>> LoadChildren(
                bool forceReload, 
                CancellationToken token)
            {
                var zones = await this.projectModelService.GetZoneNodesAsync(
                        this.modelNode.Project,
                        forceReload,
                        token)
                    .ConfigureAwait(true);

                return zones
                    .Select(z => new ZoneViewModelNode(this, z))
                    .Cast<ViewModelNodeBase>();
            }
        }

        internal class InaccessibleProjectViewModelNode : ViewModelNodeBase
        {
            public InaccessibleProjectViewModelNode(
                CloudViewModelNode parent,
                ProjectLocator projectLocator)
                : base(
                      parent,
                      parent.View,
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
                ProjectViewModelNode parent,
                IProjectExplorerZoneNode modelNode)
                : base(
                      parent,
                      parent.View,
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
                    .Select(i => new InstanceViewModelNode(this, i))
                    .Cast<ViewModelNodeBase>());
            }
        }

        internal class InstanceViewModelNode : ViewModelNodeBase
        {
            private readonly IProjectExplorerInstanceNode modelNode;

            public InstanceViewModelNode(
                ZoneViewModelNode parent,
                IProjectExplorerInstanceNode modelNode)
                : base(
                      parent,
                      parent.View,
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
