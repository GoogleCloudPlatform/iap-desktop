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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
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
        private readonly IAuthorizationAdapter authService;
        private readonly IServiceProvider serviceProvider;

        private readonly ProjectExplorerViewModel viewModel;

        public CommandContainer<IProjectExplorerNode> ContextMenuCommands { get; }
        public CommandContainer<IProjectExplorerNode> ToolbarCommands { get; }

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

            this.vsToolStripExtender.SetStyle(
                this.toolStrip,
                VisualStudioToolStripExtender.VsVersion.Vs2015,
                this.vs2015LightTheme);

            this.mainForm = serviceProvider.GetService<IMainForm>();
            this.jobService = serviceProvider.GetService<IJobService>();
            this.authService = serviceProvider.GetService<IAuthorizationAdapter>();

            this.ContextMenuCommands = new CommandContainer<IProjectExplorerNode>(
                this,
                this.contextMenu.Items,
                ToolStripItemDisplayStyle.ImageAndText,
                this.serviceProvider);
            this.ToolbarCommands = new CommandContainer<IProjectExplorerNode>(
                this,
                this.toolStrip.Items,
                ToolStripItemDisplayStyle.Image,
                this.serviceProvider);

            this.viewModel = new ProjectExplorerViewModel(
                this,
                serviceProvider.GetService<ApplicationSettingsRepository>(),
                this.jobService,
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
                        this.ContextMenuCommands.Context = node.ModelNode;
                        this.ToolbarCommands.Context = node.ModelNode;
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
            this.treeView.BindSelectedImageIndex(node => node.SelectedImageIndex);
            this.treeView.BindIsExpanded(node => node.IsExpanded);
            this.treeView.BindIsLeaf(node => node.IsLeaf);
            this.treeView.BindText(node => node.Text);
            this.treeView.Bind(this.viewModel.RootNode);
            this.treeView.OnControlPropertyChange(
                c => c.SelectedModelNode,
                node => this.viewModel.SelectedNode = node);

            this.treeView.LoadingChildrenFailed += (sender, args) =>
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "Loading project failed", args.Exception);
            };

            //
            // Bind toolbar controls.
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
            // Bind menu items.
            //
            this.unloadProjectToolStripMenuItem.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsUnloadProjectCommandVisible,
                this.Container);
            this.refreshToolStripMenuItem.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsRefreshProjectsCommandVisible,
                this.Container);
            this.refreshAllProjectsToolStripMenuItem.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsRefreshAllProjectsCommandVisible,
                this.Container);
            this.openInCloudConsoleToolStripMenuItem.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsCloudConsoleCommandVisible,
                this.Container);
            this.iapSeparatorToolStripMenuItem.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsCloudConsoleCommandVisible,
                this.Container);
            this.cloudConsoleSeparatorToolStripMenuItem.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsCloudConsoleCommandVisible,
                this.Container);
            this.configureIapAccessToolStripMenuItem.BindProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsCloudConsoleCommandVisible,
                this.Container);
        }

        private async Task CallViewModelAsync(
            Func<ProjectExplorerViewModel, Task> action,
            string actionName)
        {
            try
            {
                await action(this.viewModel).ConfigureAwait(true);
            }
            catch (Exception e) when (e.IsCancellation())
            {
                // Ignore.
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, $"{actionName} failed", e);
            }
        }

        private void CallViewModel(
            Action<ProjectExplorerViewModel> action,
            string actionName)
        {
            try
            {
                action(this.viewModel);
            }
            catch (Exception e) when (e.IsCancellation())
            {
                // Ignore.
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, $"{actionName} failed", e);
            }
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

        //---------------------------------------------------------------------
        // Context menu event handlers.
        //---------------------------------------------------------------------

        private async void refreshAllProjectsToolStripMenuItem_Click(object sender, EventArgs _)
            => await CallViewModelAsync(
                vm => vm.RefreshAsync(false),
                "Refreshing projects").ConfigureAwait(true);

        private async void refreshToolStripMenuItem_Click(object sender, EventArgs _)
            => await CallViewModelAsync(
                vm => vm.RefreshSelectedNodeAsync(),
                "Refreshing projects").ConfigureAwait(true);

        private async void unloadProjectToolStripMenuItem_Click(object sender, EventArgs _)
            => await CallViewModelAsync(
                vm => vm.UnloadSelectedProjectAsync(),
                "Unloading projects").ConfigureAwait(true);

        private void openInCloudConsoleToolStripMenuItem_Click(object sender, EventArgs _)
            => CallViewModel(
                vm => vm.OpenInCloudConsole(),
                "Opening Cloud Console");

        private void configureIapAccessToolStripMenuItem_Click(object sender, EventArgs _)
            => CallViewModel(
                vm => vm.ConfigureIapAccess(),
                "Opening Cloud Console");

        private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            this.ContextMenuCommands.ExecuteDefaultCommand();
        }

        //---------------------------------------------------------------------
        // Tool bar event handlers.
        //---------------------------------------------------------------------

        private async void refreshButton_Click(object sender, EventArgs args)
            => await CallViewModelAsync(
                vm => vm.RefreshSelectedNodeAsync(),
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
                CallViewModelAsync(
                        vm => vm.RefreshSelectedNodeAsync(),
                        "Refreshing projects")
                    .ContinueWith(_ => { });
            }
            else if (e.KeyCode == Keys.Enter)
            {
                this.ContextMenuCommands.ExecuteDefaultCommand();
            }
            else
            {
                this.ContextMenuCommands.ExecuteCommandByKey(e.KeyCode);
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

        public IProjectExplorerNode SelectedNode => this.viewModel.SelectedNode?.ModelNode;
    }
}

