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
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.SshKeys
{
    [Service(ServiceLifetime.Singleton)]
    [SkipCodeCoverage("All logic in view model")]
    public partial class AuthorizedPublicKeysView
        : ProjectExplorerTrackingToolWindow<AuthorizedPublicKeysViewModel>, IView<AuthorizedPublicKeysViewModel>
    {
        private Bound<AuthorizedPublicKeysViewModel> viewModel;

        public AuthorizedPublicKeysView(
            IServiceProvider serviceProvider)
            : base(serviceProvider, WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide)
        {
            this.components = new System.ComponentModel.Container();

            InitializeComponent();

            this.keysList.List.AddCopyCommands();
        }

        public void Bind(
            AuthorizedPublicKeysViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.viewModel.Value = viewModel;

            this.panel.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.InformationText,
                bindingContext);
            this.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.WindowTitle,
                bindingContext);
            this.BindReadonlyObservableProperty(
                c => c.TabText,
                viewModel,
                m => m.WindowTitle,
                bindingContext);
            viewModel.ResetWindowTitleAndInformationBar();  // Fire event to set initial window title.

            //
            // Bind tool strip.
            //
            this.toolStrip.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsListEnabled,
                bindingContext);
            this.deleteToolStripButton.BindReadonlyProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsDeleteButtonEnabled,
                bindingContext);

            //
            // Bind list.
            //
            this.keysList.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsListEnabled,
                bindingContext);

            this.keysList.List.BindCollection(viewModel.FilteredKeys);
            this.keysList.List.BindProperty(
                l => l.SelectedModelItem,
                viewModel,
                m => viewModel.SelectedItem,
                bindingContext);

            this.keysList.BindProperty(
                c => c.SearchTerm,
                viewModel,
                m => m.Filter,
                bindingContext);
            this.keysList.BindReadonlyObservableProperty(
                c => c.Loading,
                viewModel,
                m => m.IsLoading,
                bindingContext);
            this.keysList.SearchOnKeyDown = true;
            this.keysList.List.MultiSelect = false;

            //
            // Bind commands.
            //
            this.refreshToolStripButton.BindObservableCommand(
                viewModel,
                m => m.RefreshCommand,
                bindingContext);
            this.deleteToolStripButton.BindObservableCommand(
                viewModel,
                m => m.DeleteSelectedItemCommand,
                bindingContext);
        }

        //---------------------------------------------------------------------
        // ProjectExplorerTrackingToolWindow.
        //---------------------------------------------------------------------

        protected override async Task SwitchToNodeAsync(IProjectModelNode node)
        {
            Debug.Assert(!this.InvokeRequired, "running on UI thread");
            await this.viewModel.Value
                .SwitchToModelAsync(node)
                .ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //
            // NB. Hook KeyDown instead of KeyUp event to not interfere with 
            // child dialogs. With KeyUp, we'd get an event if a child dialog
            // is dismissed by pressing Enter.
            //
            if ((e.Control && e.KeyCode == Keys.F) || e.KeyCode == Keys.F3)
            {
                this.keysList.SetFocusOnSearchBox();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                this.deleteToolStripButton.PerformClick();
            }
        }
    }
}
