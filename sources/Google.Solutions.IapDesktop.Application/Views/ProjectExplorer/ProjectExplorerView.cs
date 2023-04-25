﻿//
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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Properties;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.ProjectPicker;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Application.Views.ProjectExplorer
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

        private readonly Service<IResourceManagerAdapter> resourceManagerAdapter;

        private ProjectExplorerViewModel viewModel;
        private CommandContainer<IProjectModelNode> contextMenuCommands;
        private CommandContainer<IProjectModelNode> toolbarCommands;

        public ICommandContainer<IProjectModelNode> ContextMenuCommands
            => this.contextMenuCommands;

        public ICommandContainer<IProjectModelNode> ToolbarCommands
            => this.toolbarCommands;

        public ProjectExplorerView(IServiceProvider serviceProvider)
            : base(serviceProvider, DockState.DockLeft)
        {
            InitializeComponent();

            this.mainForm = serviceProvider.GetService<IMainWindow>();
            this.jobService = serviceProvider.GetService<IJobService>();
            this.authorization = serviceProvider.GetService<IAuthorization>();
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.projectPickerDialog = serviceProvider.GetService<IProjectPickerDialog>();
            this.resourceManagerAdapter = serviceProvider.GetService<Service<IResourceManagerAdapter>>();

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
            this.viewModel = viewModel;

            //
            // Bind tree view.
            //
            this.treeView.BindChildren(node => node.GetFilteredNodesAsync(false));
            this.treeView.BindImageIndex(node => node.ImageIndex);
            this.treeView.BindSelectedImageIndex(node => node.ImageIndex);
            this.treeView.BindIsExpanded(node => node.IsExpanded);
            this.treeView.BindIsLeaf(node => node.IsLeaf);
            this.treeView.BindText(node => node.Text);
            this.treeView.Bind(this.viewModel.RootNode, bindingContext);
            this.treeView.OnControlPropertyChange(
                c => c.SelectedModelNode,
                node => this.viewModel.SelectedNode = node,
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
            this.viewModel.OnPropertyChange(
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

            this.contextMenuCommands = new CommandContainer<IProjectModelNode>(
                ToolStripItemDisplayStyle.ImageAndText,
                contextSource,
                bindingContext);
            this.toolbarCommands = new CommandContainer<IProjectModelNode>(
                ToolStripItemDisplayStyle.Image,
                contextSource,
                bindingContext);

            //
            // Toolbar.
            // 
            this.linuxInstancesToolStripMenuItem.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsLinuxIncluded,
                bindingContext);
            this.windowsInstancesToolStripMenuItem.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsWindowsIncluded,
                bindingContext);

            //
            // Context menu.
            //

            this.contextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "&Unload projects...",
                    node => node is IProjectModelCloudNode
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => UnloadProjectsAsync())
                {
                    ActivityText = "Unloading projects"
                });
            this.contextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "&Refresh project",
                    _ => this.viewModel.IsRefreshProjectsCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => this.viewModel.RefreshSelectedNodeAsync())
                {
                    Image = Resources.Refresh_16,
                    ActivityText = "Refreshing project"
                });
            this.contextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Refresh &all projects",
                    _ => this.viewModel.IsRefreshAllProjectsCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => this.viewModel.RefreshAsync(false))
                {
                    Image = Resources.Refresh_16,
                    ActivityText = "Refreshing project"
                });
            this.contextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "&Unload project",
                    _ => this.viewModel.IsUnloadProjectCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => this.viewModel.UnloadSelectedProjectAsync())
                {
                    ActivityText = "Unloading project"
                });

            this.contextMenuCommands.AddSeparator();
            this.contextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Open in Cloud Consol&e",
                    _ => this.viewModel.IsCloudConsoleCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => this.viewModel.OpenInCloudConsole())
                {
                    ActivityText = "Opening Cloud Console"
                });
            this.contextMenuCommands.AddCommand(
                new ContextCommand<IProjectModelNode>(
                    "Configure IAP a&ccess",
                    _ => this.viewModel.IsCloudConsoleCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => InvokeAction(
                        () => this.viewModel.ConfigureIapAccess(),
                        "Opening Cloud Console"))
                {
                    ActivityText = "Opening Cloud Console"
                });
            this.contextMenuCommands.AddSeparator();

            //
            // All commands added, apply to menu.
            //
            this.contextMenuCommands.BindTo(
                this.contextMenu.Items,
                bindingContext);
            this.toolbarCommands.BindTo(
                this.toolStrip.Items,
                bindingContext);
        }

        private async Task<bool> AddNewProjectAsync()
        {
            try
            {
                await this.jobService.RunInBackground(
                        new JobDescription("Loading projects..."),
                        _ => this.authorization.Credential.GetAccessTokenForRequestAsync())
                    .ConfigureAwait(true);

                //
                // Show project picker
                //
                if (this.projectPickerDialog
                    .SelectCloudProjects(
                        this,
                        "Add projects",
                        this.resourceManagerAdapter.GetInstance(),
                        out var projects) == DialogResult.OK)
                {
                    await this.viewModel
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
                this.viewModel.Projects,
                out var projects) == DialogResult.OK)
            {
                await this.viewModel
                    .RemoveProjectsAsync(projects.ToArray())
                    .ConfigureAwait(true);
            }
        }

        private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            this.contextMenuCommands.ExecuteDefaultCommand();
        }

        //---------------------------------------------------------------------
        // Tool bar event handlers.
        //---------------------------------------------------------------------

        private async void refreshButton_Click(object sender, EventArgs args)
            => await InvokeActionAsync(
                () => this.viewModel.RefreshSelectedNodeAsync(),
                "Refreshing projects").ConfigureAwait(true);

        private async void addButton_Click(object sender, EventArgs args)
            => await AddNewProjectAsync().ConfigureAwait(true);

        //---------------------------------------------------------------------
        // Other Windows event handlers.
        //---------------------------------------------------------------------

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
                var projects = await this.viewModel.ExpandRootAsync()
                    .ConfigureAwait(true);

                //
                // Force-select the root node to update menus.
                //
                this.viewModel.SelectedNode = this.viewModel.RootNode;

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
                InvokeActionAsync(
                        () => this.viewModel.RefreshSelectedNodeAsync(),
                        "Refreshing projects")
                    .ContinueWith(_ => { });
            }
            else if (e.KeyCode == Keys.F3 || (e.Control && e.KeyCode == Keys.F))
            {
                this.searchTextBox.Focus();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                this.contextMenuCommands.ExecuteDefaultCommand();
            }
            else
            {
                this.contextMenuCommands.ExecuteCommandByKey(e.KeyCode);
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

        public IProjectModelNode SelectedNode => this.viewModel.SelectedNode?.ModelNode;

        internal class NodeTreeView : BindableTreeView<ProjectExplorerViewModel.ViewModelNode>
        { }
    }
}

