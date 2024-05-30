//
// Copyright 2020 Google LLC
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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Properties;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Application.Windows.ProjectPicker;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Application.Windows.ProjectExplorer
{
    [ComVisible(false)]
    [SkipCodeCoverage("Logic is in view model")]
    public partial class ProjectExplorerView : ToolWindowViewBase, IProjectExplorer, IView<ProjectExplorerViewModel>
    {
        private readonly IMainWindow mainForm;
        private readonly IJobService jobService;
        private readonly IAuthorization authorization;
        private readonly IExceptionDialog exceptionDialog;
        private readonly IProjectPickerDialog projectPickerDialog;

        private readonly Service<IResourceManagerClient> resourceManagerAdapter;

        private Bound<ProjectExplorerViewModel> viewModel;
        private Bound<CommandContainer<IProjectModelNode>> contextMenuCommands;
        private Bound<CommandContainer<IProjectModelNode>> toolbarCommands;

        public ICommandContainer<IProjectModelNode> ContextMenuCommands
            => this.contextMenuCommands.Value;

        public ICommandContainer<IProjectModelNode> ToolbarCommands
            => this.toolbarCommands.Value;

        public ProjectExplorerView(IServiceProvider serviceProvider)
            : base(
                  serviceProvider.GetService<IMainWindow>(),
                  serviceProvider.GetService<ToolWindowStateRepository>(),
                  DockState.DockLeft)
        {
            InitializeComponent();

            this.mainForm = serviceProvider.GetService<IMainWindow>();
            this.jobService = serviceProvider.GetService<IJobService>();
            this.authorization = serviceProvider.GetService<IAuthorization>();
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.projectPickerDialog = serviceProvider.GetService<IProjectPickerDialog>();
            this.resourceManagerAdapter = serviceProvider.GetService<Service<IResourceManagerClient>>();

            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            Debug.Assert(
                ((ServiceRegistry)serviceProvider).Registrations[typeof(IProjectExplorer)] == ServiceLifetime.Singleton,
                "Service must be registered as singleton for HideOnClose to work");
            this.HideOnClose = true;
        }

        public void Bind(
            ProjectExplorerViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.viewModel.Value = viewModel;

            //
            // Bind tree view.
            //
            this.treeView.BindChildren(node => node.GetFilteredNodesAsync(false));
            this.treeView.BindImageIndex(node => node.ImageIndex);
            this.treeView.BindSelectedImageIndex(node => node.ImageIndex);
            this.treeView.BindIsExpanded(node => node.IsExpanded);
            this.treeView.BindIsLeaf(node => node.IsLeaf);
            this.treeView.BindText(node => node.Text);
            this.treeView.Bind(viewModel.RootNode, bindingContext);
            this.treeView.OnControlPropertyChange(
                c => c.SelectedModelNode,
                node => viewModel.SelectedNode = node,
                bindingContext);

            this.treeView.LoadingChildrenFailed += (sender, args) =>
            {
                if (!args.Exception.IsCancellation())
                {
                    this.exceptionDialog.Show(
                        this,
                        "Loading project failed", args.Exception);
                }
            };

            //
            // Bind search box and progress bar.
            //
            var searchButton = this.searchTextBox.AddOverlayButton(Resources.Search_16);
            this.progressBar.BindProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsLoading,
                bindingContext);
            this.progressBar.BindProperty(
                c => c.Visible,
                viewModel,
                m => m.IsLoading,
                bindingContext);
            this.searchTextBox.BindProperty(
                c => c.Text,
                viewModel,
                m => m.InstanceFilter,
                bindingContext);

            //
            // Menus.
            //
            var contextSource = new ContextSource<IProjectModelNode>();
            viewModel.OnPropertyChange(
                m => m.SelectedNode,
                node =>
                {
                    //
                    // NB. Due to lazily loading and inaccessible projects,
                    // ModelNode can be null.
                    //
                    if (node?.ModelNode != null)
                    {
                        contextSource.Context = node.ModelNode;
                    }
                },
                bindingContext);

            this.contextMenuCommands.Value = new CommandContainer<IProjectModelNode>(
                ToolStripItemDisplayStyle.ImageAndText,
                contextSource,
                bindingContext);
            this.toolbarCommands.Value = new CommandContainer<IProjectModelNode>(
                ToolStripItemDisplayStyle.Image,
                contextSource,
                bindingContext);

            //
            // Toolbar.
            // 
            this.linuxInstancesToolStripMenuItem.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsLinuxIncluded,
                bindingContext);
            this.windowsInstancesToolStripMenuItem.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsWindowsIncluded,
                bindingContext);
            this.refreshButton.BindObservableCommand(
                viewModel,
                m => m.RefreshSelectedNodeCommand,
                bindingContext);

            //
            // Context menu.
            //

            this.contextMenuCommands.Value.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "&Unload projects...",
                    node => node is IProjectModelCloudNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => UnloadProjectsAsync())
                {
                    Id = "UnloadProject",
                    ActivityText = "Unloading projects"
                });
            this.contextMenuCommands.Value.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "&Refresh project",
                    _ => viewModel.IsRefreshProjectsCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => viewModel.RefreshSelectedNodeAsync())
                {
                    Id = "RefreshProject",
                    Image = Resources.Refresh_16,
                    ActivityText = "Refreshing project"
                });
            this.contextMenuCommands.Value.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Refresh &all projects",
                    _ => viewModel.IsRefreshAllProjectsCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => viewModel.RefreshAsync(false))
                {
                    Id = "RefeshAllProjects",
                    Image = Resources.Refresh_16,
                    ActivityText = "Refreshing project"
                });
            this.contextMenuCommands.Value.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "&Unload project",
                    _ => viewModel.IsUnloadProjectCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => viewModel.UnloadSelectedProjectAsync())
                {
                    Id = "UnloadProject",
                    ActivityText = "Unloading project"
                });

            this.contextMenuCommands.Value.AddSeparator();
            this.contextMenuCommands.Value.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Open in Cloud Consol&e",
                    _ => viewModel.IsCloudConsoleCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => viewModel.OpenInCloudConsole())
                {
                    Id = "OpenCloudConsole",
                    ActivityText = "Opening Cloud Console"
                });
            this.contextMenuCommands.Value.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Configure IAP a&ccess",
                    _ => viewModel.IsCloudConsoleCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => viewModel.ConfigureIapAccess())
                {
                    Id = "ConfigureIapAccess",
                    ActivityText = "Opening Cloud Console"
                });
            this.contextMenuCommands.Value.AddSeparator();

            //
            // All commands added, apply to menu.
            //
            this.contextMenuCommands.Value.BindTo(
                this.contextMenu.Items,
                bindingContext);
            this.toolbarCommands.Value.BindTo(
                this.toolStrip.Items,
                bindingContext);
        }

        private async Task<bool> AddNewProjectAsync()
        {
            try
            {
                await this.jobService.RunAsync(
                        new JobDescription("Loading projects..."),
                        _ => this.authorization
                            .Session
                            .ApiCredential
                            .GetAccessTokenForRequestAsync())
                    .ConfigureAwait(true);

                //
                // Show project picker
                //
                if (this.projectPickerDialog
                    .SelectCloudProjects(
                        this,
                        "Add projects",
                        this.resourceManagerAdapter.Activate(),
                        out var projects) == DialogResult.OK)
                {
                    await this.viewModel
                        .Value
                        .AddProjectsAsync(projects.ToArray())
                        .ConfigureAwait(true);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e) when (e.IsCancellation())
            {
                // Ignore.
                return false;
            }
            catch (Exception e)
            {
                this.exceptionDialog.Show(
                    this,
                    "Adding project failed", e);
                return false;
            }
        }

        private async Task UnloadProjectsAsync()
        {
            if (this.projectPickerDialog.SelectLocalProjects(
                this,
                "Unload projects",
                this.viewModel.Value.Projects,
                out var projects) == DialogResult.OK)
            {
                await this.viewModel
                    .Value
                    .RemoveProjectsAsync(projects.ToArray())
                    .ConfigureAwait(true);
            }
        }

        private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            this.contextMenuCommands.Value.ExecuteDefaultCommand();
        }

        //---------------------------------------------------------------------
        // Tool bar event handlers.
        //---------------------------------------------------------------------

        private async void addButton_Click(object sender, EventArgs args)
            => await AddNewProjectAsync().ConfigureAwait(true);

        //---------------------------------------------------------------------
        // Other Windows event handlers.
        //---------------------------------------------------------------------

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.searchTextBox != null)
            {
                //
                // DPI scaling can produce a gap between controls.
                // Rearrange controls to remove this gap.
                //
                this.searchTextBox.Top = this.toolStrip.Height;
                this.progressBar.Top = this.searchTextBox.Bottom;

                var gap = this.treeView.Top - this.progressBar.Bottom;
                this.treeView.Top -= gap;
                this.treeView.Height += gap;
            }
        }

        private async void ProjectExplorerWindow_Shown(object sender, EventArgs _)
        {
            try
            {
                //
                // Expand projects.
                //
                // NB. It's not safe to do this in the constructor
                // because some of the dependencies might not be ready yet.
                //
                var projects = await this.viewModel.Value.ExpandRootAsync()
                    .ConfigureAwait(true);

                //
                // Force-select the root node to update menus.
                //
                this.viewModel.Value.SelectedNode = this.viewModel.Value.RootNode;

                if (!projects.Any())
                {
                    // No projects in inventory yet - pop open the 'Add Project'
                    // dialog to get the user started.
                    await AddNewProjectAsync().ConfigureAwait(true);
                }
            }
            catch (Exception e) when (e.IsCancellation())
            {
                // Most likely, the user rejected to reauthorize. Quit the app.
                this.mainForm.Close();

            }
            catch (Exception e)
            {
                this.exceptionDialog.Show(
                    this.mainForm,
                    "Loading projects failed", e);

                // Do not close the application, otherwise the user has no 
                // chance to remediate the situation by unloading the offending
                // project.
            }
        }

        private void ProjectExplorerWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // NB. Hook KeyDown instead of KeyUp event to not interfere with 
            // child dialogs. With KeyUp, we'd get an event if a child dialog
            // is dismissed by pressing Enter.

            if (e.KeyCode == Keys.F5)
            {
                this.refreshButton.PerformClick();
            }
            else if (e.KeyCode == Keys.F3 || (e.Control && e.KeyCode == Keys.F))
            {
                this.searchTextBox.Focus();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                this.contextMenuCommands.Value.ExecuteDefaultCommand();
            }
            else
            {
                this.contextMenuCommands.Value.ExecuteCommandByKey(e.KeyCode);
            }
        }

        //---------------------------------------------------------------------
        // IProjectExplorer.
        //---------------------------------------------------------------------

        public async Task ShowAddProjectDialogAsync()
        {
            //
            // NB. The project explorer might be hidden and no project
            // might have been loaded yet.
            //
            if (await AddNewProjectAsync().ConfigureAwait(true))
            {
                //
                // Show the window. That might kick off an asynchronous
                // Refresh if the window previously was not visible.
                //
                ShowWindow();
            }
        }

        public IProjectModelNode? SelectedNode
        {
            get => this.viewModel.Value.SelectedNode?.ModelNode;
        }

        internal class NodeTreeView : BindableTreeView<ProjectExplorerViewModel.ViewModelNode>
        { }
    }
}

