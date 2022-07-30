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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Properties;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Surface;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.ProjectPicker;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Application.Views.ProjectExplorer
{
    [ComVisible(false)]
    [SkipCodeCoverage("Logic is in view model")]
    public partial class ProjectExplorerWindow : ToolWindow, IProjectExplorer
    {
        private readonly IMainForm mainForm;
        private readonly IJobService jobService;
        private readonly IAuthorizationSource authService;
        private readonly IServiceProvider serviceProvider;

        private readonly ProjectExplorerViewModel viewModel;
        private readonly ToolStripCommandContainer<IProjectModelNode> contextMenuCommands;
        private readonly ToolStripCommandContainer<IProjectModelNode> toolbarCommands;

        public ICommandContainer<IProjectModelNode> ContextMenuCommands 
            => this.contextMenuCommands;

        public ICommandContainer<IProjectModelNode> ToolbarCommands
            => this.toolbarCommands;

        public ProjectExplorerWindow(IServiceProvider serviceProvider)
            : base(serviceProvider, DockState.DockLeft)
        {
            InitializeComponent();

            this.serviceProvider = serviceProvider;

            this.TabText = this.Text;

            //
            // This window is a singleton, so we never want it to be closed,
            // just hidden.
            //
            this.HideOnClose = true;

            serviceProvider
                .GetService<IThemeService>()
                .ApplyTheme(this.toolStrip);

            this.mainForm = serviceProvider.GetService<IMainForm>();
            this.jobService = serviceProvider.GetService<IJobService>();
            this.authService = serviceProvider.GetService<IAuthorizationSource>();

            this.contextMenuCommands = new ToolStripCommandContainer<IProjectModelNode>(
                ToolStripItemDisplayStyle.ImageAndText);
            this.contextMenuCommands.CommandFailed += Surface_CommandFailed;

            this.toolbarCommands = new ToolStripCommandContainer<IProjectModelNode>(
                ToolStripItemDisplayStyle.Image);
            this.toolbarCommands.CommandFailed += Surface_CommandFailed;

            this.viewModel = new ProjectExplorerViewModel(
                this,
                serviceProvider.GetService<ApplicationSettingsRepository>(),
                this.jobService,
                serviceProvider.GetService<IEventService>(),
                serviceProvider.GetService<IGlobalSessionBroker>(),
                serviceProvider.GetService<IProjectModelService>(),
                serviceProvider.GetService<ICloudConsoleService>());

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
                        this.contextMenuCommands.Context = node.ModelNode;
                        this.toolbarCommands.Context = node.ModelNode;
                    }
                });

            this.Disposed += (sender, args) =>
            {
                this.viewModel.Dispose();
            };

            //
            // Bind tree view.
            //
            this.treeView.BindChildren(node => node.GetFilteredNodesAsync(false));
            this.treeView.BindImageIndex(node => node.ImageIndex);
            this.treeView.BindSelectedImageIndex(node => node.ImageIndex);
            this.treeView.BindIsExpanded(node => node.IsExpanded);
            this.treeView.BindIsLeaf(node => node.IsLeaf);
            this.treeView.BindText(node => node.Text);
            this.treeView.Bind(this.viewModel.RootNode);
            this.treeView.OnControlPropertyChange(
                c => c.SelectedModelNode,
                node => this.viewModel.SelectedNode = node);

            this.treeView.LoadingChildrenFailed += (sender, args) =>
            {
                if (!args.Exception.IsCancellation())
                {
                    this.serviceProvider
                        .GetService<IExceptionDialog>()
                        .Show(this, "Loading project failed", args.Exception);
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
                this.Container);
            this.progressBar.BindProperty(
                c => c.Visible,
                viewModel,
                m => m.IsLoading,
                this.Container);
            this.searchTextBox.BindProperty(
                c => c.Text,
                viewModel,
                m => m.InstanceFilter,
                this.Container);

            //
            // Toolbar.
            // 
            this.linuxInstancesToolStripMenuItem.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsLinuxIncluded,
                this.Container);
            this.windowsInstancesToolStripMenuItem.BindProperty(
                c => c.Checked,
                this.viewModel,
                m => m.IsWindowsIncluded,
                this.Container);

            //
            // Context menu.
            //
            this.contextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "&Refresh project",
                    _ => this.viewModel.IsRefreshProjectsCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => InvokeActionNoawaitAsync(
                        () => this.viewModel.RefreshSelectedNodeAsync(),
                        "Refreshing project"))
                {
                    Image = Resources.Refresh_161
                });
            this.contextMenuCommands.AddCommand(
                new Command<IProjectModelNode>(
                    "Refresh &all projects",
                    _ => this.viewModel.IsRefreshAllProjectsCommandVisible
                        ? CommandState.Enabled
                        : CommandState.Unavailable,
                    _ => InvokeActionNoawaitAsync(
                        () => this.viewModel.RefreshAsync(false),
                        "Refreshing projects"))
                {
                    Image = Resources.Refresh_161
                });
            this.contextMenuCommands.AddCommand(
                "&Unload project",
                _ => this.viewModel.IsUnloadProjectCommandVisible
                    ? CommandState.Enabled
                    : CommandState.Unavailable,
                _ => InvokeActionNoawaitAsync(
                    () => this.viewModel.UnloadSelectedProjectAsync(),
                    "Unloading project"));

            this.contextMenuCommands.AddSeparator();
            this.contextMenuCommands.AddCommand(
                "Open in Cloud Consol&e",
                _ => this.viewModel.IsCloudConsoleCommandVisible
                    ? CommandState.Enabled
                    : CommandState.Unavailable,
                _ => InvokeAction(
                    () => this.viewModel.OpenInCloudConsole(),
                    "Opening Cloud Console"));
            this.contextMenuCommands.AddCommand(
                "Configure IAP a&ccess",
                _ => this.viewModel.IsCloudConsoleCommandVisible
                    ? CommandState.Enabled
                    : CommandState.Unavailable,
                _ => InvokeAction(
                    () => this.viewModel.ConfigureIapAccess(),
                    "Opening Cloud Console"));
            this.contextMenuCommands.AddSeparator();

            //
            // All commands added, apply to menu.
            //
            this.contextMenuCommands.ApplyTo(this.contextMenu);
            this.toolbarCommands.ApplyTo(this.toolStrip);
        }

        private async Task<bool> AddNewProjectAsync()
        {
            try
            {
                await this.jobService.RunInBackground(
                        new JobDescription("Loading projects..."),
                        _ => this.authService.Authorization.Credential.GetAccessTokenForRequestAsync())
                    .ConfigureAwait(true);

                // Show project picker
                var dialog = this.serviceProvider.GetService<IProjectPickerWindow>();
                string projectId = dialog.SelectProject(this);

                if (projectId == null)
                {
                    // Cancelled.
                    return false;
                }

                await this.viewModel
                    .AddProjectAsync(new ProjectLocator(projectId))
                    .ConfigureAwait(true);

                return true;
            }
            catch (Exception e) when (e.IsCancellation())
            {
                // Ignore.
                return false;
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Adding project failed", e);
                return false;
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

        private void Surface_CommandFailed(object sender, ExceptionEventArgs e)
        {
            this.serviceProvider
                .GetService<IExceptionDialog>()
                .Show(this, "Executing command failed", e.Exception);
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
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Loading projects failed", e);

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
            // NB. The project explorer might be hidden and no project
            // might have been loaded yet.
            if (await AddNewProjectAsync().ConfigureAwait(true))
            {
                // Show the window. That might kick of an asynchronous
                // Refresh if the window previously was not visible.
                ShowWindow();
            }
        }

        public IProjectModelNode SelectedNode => this.viewModel.SelectedNode?.ModelNode;

        internal class NodeTreeView : BindableTreeView<ProjectExplorerViewModel.ViewModelNode>
        { }
    }
}

