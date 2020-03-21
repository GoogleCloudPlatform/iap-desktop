using Google.Apis.Compute.v1.Data;
using Google.Solutions.CloudIap.IapDesktop.Windows;
using Google.Solutions.Compute.Auth;
using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.CloudIap.IapDesktop.ProjectExplorer
{
    public partial class ProjectExplorerWindow : ToolWindow, IProjectExplorer
    {
        private readonly DockPanel dockPanel;

        private readonly IMainForm mainForm;
        private readonly IEventService eventService;
        private readonly JobService jobService;
        private readonly ProjectInventoryService projectInventoryService;
        private readonly ComputeEngineAdapter computeEngineAdapter;

        private readonly CloudNode rootNode = new CloudNode();

        public ProjectExplorerWindow()
        {
            InitializeComponent();

            this.TabText = this.Text;

            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;

            this.vsToolStripExtender.SetStyle(
                this.toolStrip,
                VisualStudioToolStripExtender.VsVersion.Vs2015,
                this.vs2015LightTheme);

            this.treeView.Nodes.Add(this.rootNode);

            this.mainForm = Program.Services.GetService<IMainForm>();
            this.eventService = Program.Services.GetService<IEventService>();
            this.jobService = Program.Services.GetService<JobService>();
            this.projectInventoryService = Program.Services.GetService<ProjectInventoryService>();
            this.computeEngineAdapter = Program.Services.GetService<ComputeEngineAdapter>();

            this.eventService.BindAsyncHandler<ProjectInventoryService.ProjectAddedEvent>(OnProjectAdded);
            this.eventService.BindHandler<ProjectInventoryService.ProjectDeletedEvent>(OnProjectDeleted);
        }

        public ProjectExplorerWindow(DockPanel dockPanel) : this()
        {
            this.dockPanel = dockPanel;
            ShowWindow();
        }

        private void RefreshProject(string projectId, IEnumerable<Instance> instances)
        {
            Debug.Assert(!this.InvokeRequired);

            var projectNode = this.rootNode.Nodes
                .Cast<ProjectNode>()
                .FirstOrDefault(n => n.ProjectId == projectId);
            if (projectNode != null)
            {
                projectNode.Populate(instances);
            }
            else
            {
                projectNode = new ProjectNode(projectId);
                projectNode.Populate(instances);
                this.rootNode.Nodes.Add(projectNode);
            }

            this.rootNode.Expand();
        }

        private async void ProjectExplorerWindow_Shown(object sender, EventArgs _)
        {
            try
            {
                await RefreshAllProjects();
            }
            catch (TaskCanceledException)
            {
                // Most likely, the user rejected to reauthorize. Quit the app.
                this.mainForm.Close();
                
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(this, "Loading projects failed", e);
                this.mainForm.Close();
            }
        }

        //---------------------------------------------------------------------
        // Context menu event handlers.
        //---------------------------------------------------------------------

        private void treeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.treeView.SelectedNode = e.Node;
            }
        }

        private void contextMenu_Opening(object sender, CancelEventArgs e)
        {
            var selectedNode = this.treeView.SelectedNode;
            
            e.Cancel = (selectedNode is ZoneNode);

            this.refreshToolStripMenuItem.Visible = 
                this.unloadProjectToolStripMenuItem.Visible = (selectedNode is ProjectNode);
            this.refreshAllProjectsToolStripMenuItem.Visible = (selectedNode is CloudNode);
        }

        private async void refreshAllProjectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await RefreshAllProjects();
        }

        private async void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView.SelectedNode is ProjectNode projectNode)
            {
                await RefreshProject(projectNode.ProjectId);
            }
        }

        private async void unloadProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView.SelectedNode is ProjectNode projectNode)
            {
                await this.projectInventoryService.DeleteProjectAsync(projectNode.ProjectId);
            }
        }

        //---------------------------------------------------------------------
        // Tool bar event handlers.
        //---------------------------------------------------------------------

        private async void refreshButton_Click(object sender, EventArgs args)
        {
            try
            {
                await RefreshAllProjects();
            }
            catch (TaskCanceledException)
            {
                // Ignore.
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(this, "Refreshing projects failed", e);
            }
        }

        private async void addButton_Click(object sender, EventArgs args)
        {
            try
            {
                var authorization = Program.Services.GetService<IAuthorization>();
                // Force authentication refresh.
                await this.jobService.RunInBackground(
                    new JobDescription("Loading projects..."),
                    _ => authorization.Credential.GetAccessTokenForRequestAsync());

                // Show project picker
                string projectId = projectId = ProjectPickerDialog.SelectProjectId(this);

                if (projectId == null)
                {
                    // Cancelled.
                    return;
                }

                await this.projectInventoryService.AddProjectAsync(projectId);
            }
            catch (TaskCanceledException)
            {
                // Ignore.
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(this, "Adding project failed", e);
            }
        }

        private async void treeView_AfterSelect(object sender, TreeViewEventArgs args)
        {
            try
            {
                await this.eventService.FireAsync(
                    ((ProjectExplorerNode)args.Node).CreateSelectedEvent());
            }
            catch (TaskCanceledException)
            {
                // Ignore.
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(this, "An error occured", e);
            }
        }

        //---------------------------------------------------------------------
        // Service event handlers.
        //---------------------------------------------------------------------

        private async Task OnProjectAdded(ProjectInventoryService.ProjectAddedEvent e)
        {
            Debug.Assert(!this.InvokeRequired);

            await RefreshProject(e.ProjectId);
        }

        private void OnProjectDeleted(ProjectInventoryService.ProjectDeletedEvent e)
        {
            Debug.Assert(!this.InvokeRequired);
            var node = this.rootNode.Nodes
                .Cast<ProjectNode>()
                .Where(p => p.ProjectId == e.ProjectId)
                .FirstOrDefault();

            if (node != null)
            {
                // Remove corresponding node from tree.
                this.rootNode.Nodes.Remove(node);
            }
        }

        //---------------------------------------------------------------------
        // IProjectExplorer.
        //---------------------------------------------------------------------

        public void ShowWindow()
        {
            Show(this.dockPanel, DockState.DockLeft);
        }

        public async Task RefreshAllProjects()
        {
            Debug.Assert(!this.InvokeRequired);

            var projectsAndInstances = await this.jobService.RunInBackground(
                new JobDescription("Loading projects..."),
                async token => {
                    var accumulator = new Dictionary<string, IEnumerable<Instance>>();

                    foreach (var project in await this.projectInventoryService.ListProjectsAsync())
                    {
                        accumulator[project.Name] =
                            await this.computeEngineAdapter.QueryInstancesAsync(project.Name);
                    }

                    return accumulator;
                });

            foreach (var entry in projectsAndInstances)
            {
                RefreshProject(entry.Key, entry.Value);
            }
        }

        public async Task RefreshProject(string projectId)
        {
            Debug.Assert(!this.InvokeRequired);

            var instances = await this.jobService.RunInBackground(
                new JobDescription("Loading project inventory..."),
                token => this.computeEngineAdapter.QueryInstancesAsync(projectId));

            RefreshProject(projectId, instances);
        }

        //---------------------------------------------------------------------
        // Nodes.
        //---------------------------------------------------------------------

        private abstract class ProjectExplorerNode : TreeNode
        {
            public ProjectExplorerNode(string name, int iconIndex)
                : base("Google Cloud", iconIndex, iconIndex)
            {
            }

            public abstract ProjectExplorerNodeSelectedEvent CreateSelectedEvent();
        }

        private class CloudNode : ProjectExplorerNode
        {
            private const int IconIndex = 0;

            public CloudNode()
                : base("Google Cloud", IconIndex)
            {
            }

            public override ProjectExplorerNodeSelectedEvent CreateSelectedEvent()
                => new ProjectExplorerCloudNodeSelectedEvent();
        }

        private class ProjectNode : ProjectExplorerNode
        {
            private const int IconIndex = 1;

            public string ProjectId => this.Text;

            public ProjectNode(string projectId)
                : base(projectId, IconIndex)
            {
            }

            private string longZoneToShortZoneId(string zone) => zone.Substring(zone.LastIndexOf("/") + 1);

            public void Populate(IEnumerable<Instance> allInstances)
            {
                this.Nodes.Clear();

                // Narrow the list down to Windows instances - there is no point 
                // of adding Linux instanes to the list of servers.
                var instances = allInstances.Where(i => ComputeEngineAdapter.IsWindowsInstance(i));
                var zoneIds = instances.Select(i => longZoneToShortZoneId(i.Zone)).ToHashSet();

                foreach (var zoneId in zoneIds)
                {
                    var zoneNode = new ZoneNode(zoneId);
                    
                    var instancesInZone = instances
                        .Where(i => longZoneToShortZoneId(i.Zone) == zoneId)
                        .OrderBy(i => i.Name)
                        .Select(i => i.Name);

                    foreach (var instanceName in instancesInZone)
                    {
                        var instanceNode = new VmInstanceNode(instanceName);

                        zoneNode.Nodes.Add(instanceNode);
                    }

                    this.Nodes.Add(zoneNode);
                    zoneNode.Expand();
                }

                Expand();
            }
            public override ProjectExplorerNodeSelectedEvent CreateSelectedEvent()
                => new ProjectExplorerProjectNodeSelectedEvent(this.ProjectId);
        }

        private class ZoneNode : ProjectExplorerNode
        {
            private const int IconIndex = 3;

            public string ProjectId => ((ProjectNode)this.Parent).ProjectId;
            public string ZoneId => this.Text;

            public ZoneNode(string zoneId)
                : base(zoneId, IconIndex)
            {
            }

            public override ProjectExplorerNodeSelectedEvent CreateSelectedEvent()
                => new ProjectExplorerZoneNodeSelectedEvent(this.ProjectId, this.ZoneId);
        }

        private class VmInstanceNode : ProjectExplorerNode
        {
            private const int IconIndex = 4;
            private const int ActiveIconIndex = 4;

            public string ProjectId => ((ZoneNode)this.Parent).ProjectId;
            public string ZoneId => ((ZoneNode)this.Parent).ZoneId;
            public string InstanceName => this.Text;

            public VmInstanceNode(string instanceName)
                : base(instanceName, IconIndex)
            {
            }

            public override ProjectExplorerNodeSelectedEvent CreateSelectedEvent()
                => new ProjectExplorerVmInstanceNodeSelectedEvent(this.ProjectId, this.ZoneId, this.InstanceName);
        }
    }
}

