//
// Copyright 2022 Google LLC
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
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshKeys
{
    [Service(ServiceLifetime.Singleton)]
    [SkipCodeCoverage("All logic in view model")]
    public partial class AuthorizedPublicKeysWindow
        : ProjectExplorerTrackingToolWindow<AuthorizedPublicKeysViewModel>
    {
        private readonly AuthorizedPublicKeysViewModel viewModel;

        public AuthorizedPublicKeysWindow(
            IServiceProvider serviceProvider)
            : base(serviceProvider, WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide)
        {
            this.components = new System.ComponentModel.Container();

            InitializeComponent();

            serviceProvider
                .GetService<IThemeService>()
                .ApplyTheme(this.toolStrip);

            this.viewModel = new AuthorizedPublicKeysViewModel(serviceProvider)
            {
                View = this
            };

            this.infoLabel.BindReadonlyProperty(
                c => c.Text,
                this.viewModel,
                m => m.InformationBarContent,
                this.components);
            this.components.Add(this.viewModel.OnPropertyChange(
                m => m.IsInformationBarVisible,
                visible =>
                {
                    this.splitContainer.Panel1Collapsed = !visible;
                    this.splitContainer.SplitterDistance = this.splitContainer.Panel1MinSize;
                }));

            this.components.Add(this.viewModel.OnPropertyChange(
                m => m.WindowTitle,
                title =>
                {
                    // NB. Update properties separately instead of using multi-assignment,
                    // otherwise the title does not update properly.
                    this.TabText = title;
                    this.Text = title;
                }));
            this.viewModel.ResetWindowTitleAndInformationBar();  // Fire event to set initial window title.


            // Bind tool strip.
            this.toolStrip.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsListEnabled,
                this.components);
            this.deleteToolStripButton.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsDeleteButtonEnabled,
                this.components);

            // Bind list.
            this.keysList.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsListEnabled,
                this.components);

            this.keysList.List.BindCollection(this.viewModel.FilteredKeys);
            this.keysList.List.AddCopyCommands();
            this.keysList.List.BindProperty(
                l => l.SelectedModelItem,
                this.viewModel,
                m => this.viewModel.SelectedItem,
                this.components);

            this.keysList.BindProperty(
                c => c.SearchTerm,
                this.viewModel,
                m => m.Filter,
                this.components);
            this.keysList.BindProperty(
                c => c.Loading,
                this.viewModel,
                m => m.IsLoading,
                this.components);
            this.keysList.SearchOnKeyDown = true;
            this.keysList.List.MultiSelect = false;
        }

        //---------------------------------------------------------------------
        // ProjectExplorerTrackingToolWindow.
        //---------------------------------------------------------------------

        protected override async Task SwitchToNodeAsync(IProjectModelNode node)
        {
            Debug.Assert(!InvokeRequired, "running on UI thread");
            await this.viewModel.SwitchToModelAsync(node)
                .ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void PackageInventoryWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // NB. Hook KeyDown instead of KeyUp event to not interfere with 
            // child dialogs. With KeyUp, we'd get an event if a child dialog
            // is dismissed by pressing Enter.

            if ((e.Control && e.KeyCode == Keys.F) ||
                 e.KeyCode == Keys.F3)
            {
                this.keysList.SetFocusOnSearchBox();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                InvokeActionAsync(
                        () => this.viewModel.DeleteSelectedItemAsync(CancellationToken.None),
                        "Deleting key")
                    .ConfigureAwait(true);
            }
        }

        private async void refreshToolStripButton_Click(object sender, EventArgs _)
            => await InvokeActionAsync(
                () => this.viewModel.RefreshAsync(),
                "Refreshing keys")
            .ConfigureAwait(true);

        private async void deleteToolStripButton_Click(object sender, EventArgs _)
            => await InvokeActionAsync(
                () => this.viewModel.DeleteSelectedItemAsync(CancellationToken.None),
                "Deleting key")
            .ConfigureAwait(true);
    }
}
