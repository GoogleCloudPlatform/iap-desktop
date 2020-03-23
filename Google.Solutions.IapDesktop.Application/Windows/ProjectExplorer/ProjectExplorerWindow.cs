using Google.Apis.Compute.v1.Data;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Windows;
using Google.Solutions.Compute.Auth;
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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.SettingsEditor;

namespace Google.Solutions.IapDesktop.Application.ProjectExplorer
{
    public partial class ProjectExplorerWindow : ToolWindow, IProjectExplorer
    {
        private readonly DockPanel dockPanel;
        private readonly IMainForm mainForm;
        private readonly IEventService eventService;
        private readonly JobService jobService;
        private readonly ProjectInventoryService projectInventoryService;
        private readonly InventorySettingsRepository settingsRepository;
        private readonly ISettingsEditor settingsEditor;
        private readonly IAuthorizationService authService;
        private readonly CloudConsoleService cloudConsoleService;
        private readonly IServiceProvider serviceProvider;

        private readonly CloudNode rootNode = new CloudNode();


        public ProjectExplorerWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
            this.serviceProvider = serviceProvider;

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

            this.mainForm = serviceProvider.GetService<IMainForm>();
            this.eventService = serviceProvider.GetService<IEventService>();
            this.jobService = serviceProvider.GetService<JobService>();
            this.projectInventoryService = serviceProvider.GetService<ProjectInventoryService>();
            this.settingsRepository = serviceProvider.GetService<InventorySettingsRepository>();
            this.settingsEditor = serviceProvider.GetService<ISettingsEditor>();
            this.authService = serviceProvider.GetService<IAuthorizationService>();
            this.cloudConsoleService = serviceProvider.GetService<CloudConsoleService>();

            this.eventService.BindAsyncHandler<ProjectInventoryService.ProjectAddedEvent>(OnProjectAdded);
            this.eventService.BindHandler<ProjectInventoryService.ProjectDeletedEvent>(OnProjectDeleted);
        }

        private void PopulateProjectNode(string projectId, IEnumerable<Instance> instances)
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
                projectNode = new ProjectNode(this.settingsRepository, projectId);
                projectNode.Populate(instances);
                this.rootNode.Nodes.Add(projectNode);
            }

            this.rootNode.Expand();
        }

        private async Task AddProjectAsync()
        {
            await this.jobService.RunInBackground(
                new JobDescription("Loading projects..."),
                _ => this.authService.Authorization.Credential.GetAccessTokenForRequestAsync());

            // Show project picker
            var dialog = this.serviceProvider.GetService<ProjectPickerDialog>();
            string projectId = projectId = dialog.SelectProjectId(this);

            if (projectId == null)
            {
                // Cancelled.
                return;
            }

            await this.projectInventoryService.AddProjectAsync(projectId);
            
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
            
            this.refreshToolStripMenuItem.Visible = 
                this.unloadProjectToolStripMenuItem.Visible = (selectedNode is ProjectNode);
            this.refreshAllProjectsToolStripMenuItem.Visible = (selectedNode is CloudNode);
            this.propertiesToolStripMenuItem.Visible = (selectedNode is InventoryNode);

            this.cloudConsoleSeparatorToolStripMenuItem.Visible =
                this.openInCloudConsoleToolStripMenuItem.Visible =
                this.openlogsToolStripMenuItem.Visible = (selectedNode is VmInstanceNode);

            this.iapSeparatorToolStripMenuItem.Visible =
                this.configureIapAccessToolStripMenuItem.Visible =
                     (selectedNode is VmInstanceNode || selectedNode is ProjectNode);
        }

        private async void refreshAllProjectsToolStripMenuItem_Click(object sender, EventArgs _)
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
                ExceptionDialog.Show(this, "Refreshing project failed", e);
            }
        }

        private async void refreshToolStripMenuItem_Click(object sender, EventArgs _)
        {
            try
            { 
                if (this.treeView.SelectedNode is ProjectNode projectNode)
                {
                    await RefreshProject(projectNode.ProjectId);
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore.
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(this, "Refreshing project failed", e);
            }
        }

        private async void unloadProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView.SelectedNode is ProjectNode projectNode)
            {
                await this.projectInventoryService.DeleteProjectAsync(projectNode.ProjectId);
            }
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
            => openSettingsButton_Click(sender, e);

        private void openInCloudConsoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView.SelectedNode is VmInstanceNode vmInstanceNode)
            {
                this.cloudConsoleService.OpenVmInstance(vmInstanceNode.Reference);
            }
        }

        private void openlogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView.SelectedNode is VmInstanceNode vmInstanceNode)
            {
                this.cloudConsoleService.OpenVmInstanceLogs(vmInstanceNode.Reference, vmInstanceNode.InstanceId);
            }
        }

        private void configureIapAccessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView.SelectedNode is ProjectNode projectNode)
            {
                this.cloudConsoleService.ConfigureIapAccess(projectNode.ProjectId);
            }
            else if (this.treeView.SelectedNode is VmInstanceNode vmInstanceNode)
            {
                this.cloudConsoleService.ConfigureIapAccess(vmInstanceNode.ProjectId);
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
                await AddProjectAsync();
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

        private void openSettingsButton_Click(object sender, EventArgs _)
        {
            if (this.treeView.SelectedNode is InventoryNode inventoryNode)
            {
                this.settingsEditor.ShowWindow(inventoryNode);
            }
        }

        //---------------------------------------------------------------------
        // Other Windows event handlers.
        //---------------------------------------------------------------------

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
                
                // Do not close the application, otherwise the user has no 
                // chance to remediate the situation by unloading the offending
                // project.
            }
        }

        private async void treeView_AfterSelect(object sender, TreeViewEventArgs args)
        {
            try
            {
                this.openSettingsButton.Enabled = args.Node is InventoryNode;
                await this.eventService.FireAsync(
                    new ProjectExplorerNodeSelectedEvent(
                        ((IProjectExplorerNode)args.Node)));
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
        private void ProjectExplorerWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F4)
            {
                openSettingsButton_Click(sender, EventArgs.Empty);
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

            // Move selection to a "safe" spot.
            this.treeView.SelectedNode = this.rootNode;

            var computeEngineAdapter = this.serviceProvider.GetService<ComputeEngineAdapter>();

            var failedProjects = new Dictionary<string, Exception>();

            var projectsAndInstances = await this.jobService.RunInBackground(
                new JobDescription("Loading projects..."),
                async token => {
                    var accumulator = new Dictionary<string, IEnumerable<Instance>>();

                    foreach (var project in await this.projectInventoryService.ListProjectsAsync())
                    {
                        try
                        {
                            accumulator[project.Name] =
                                await computeEngineAdapter.QueryInstancesAsync(project.Name);
                        }
                        catch (Exception e)
                        {
                            // If one project fails to load, we should stil load the other onces.
                            failedProjects[project.Name] = e;
                        }
                    }

                    return accumulator;
                });

            foreach (var entry in projectsAndInstances)
            {
                PopulateProjectNode(entry.Key, entry.Value);
            }

            if (failedProjects.Any())
            {
                // Add an (empty) project node so that the user can at least unload the project.
                foreach (string projectId in failedProjects.Keys)
                {
                    PopulateProjectNode(projectId, Enumerable.Empty<Instance>());
                }

                throw new AggregateException(
                    $"The following projects failed to refresh: {string.Join(", ", failedProjects.Keys)}", 
                    failedProjects.Values.Cast<Exception>());
            }
        }

        public async Task RefreshProject(string projectId)
        {
            Debug.Assert(!this.InvokeRequired);

            var computeEngineAdapter = this.serviceProvider.GetService<ComputeEngineAdapter>();
            var instances = await this.jobService.RunInBackground(
                new JobDescription("Loading project inventory..."),
                token => computeEngineAdapter.QueryInstancesAsync(projectId));

            PopulateProjectNode(projectId, instances);
        }

        public async Task ShowAddProjectDialogAsync()
        {
            ShowWindow();

            await AddProjectAsync();
        }
    }
}

