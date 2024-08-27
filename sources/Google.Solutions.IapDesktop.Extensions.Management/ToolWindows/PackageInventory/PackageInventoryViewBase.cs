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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Platform.Net;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.PackageInventory
{
    [SkipCodeCoverage("All logic in view model")]
    [Service]
    public partial class PackageInventoryViewBase
        : ProjectExplorerTrackingToolWindow<PackageInventoryViewModel>, IView<PackageInventoryViewModel>
    {
        private readonly PackageInventoryType inventoryType;
        private Bound<PackageInventoryViewModel> viewModel;

        public PackageInventoryViewBase(
            PackageInventoryType inventoryType,
            IServiceProvider serviceProvider)
            : base(serviceProvider, WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide)
        {
            this.inventoryType = inventoryType;
            this.components = new System.ComponentModel.Container();

            InitializeComponent();

            this.packageList.List.AddCopyCommands();
        }

        public void Bind(
            PackageInventoryViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.viewModel.Value = viewModel;
            viewModel.InventoryType = this.inventoryType;

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
            viewModel.ResetWindowTitle();  // Fire event to set initial window title.

            //
            // Bind list.
            //
            this.packageList.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsPackageListEnabled,
                bindingContext);

            this.packageList.List.BindCollection(viewModel.FilteredPackages);
            this.packageList.BindProperty(
                c => c.SearchTerm,
                viewModel,
                m => m.Filter,
                bindingContext);
            this.packageList.BindReadonlyObservableProperty(
                c => c.Loading,
                viewModel,
                m => m.IsLoading,
                bindingContext);
            this.packageList.SearchOnKeyDown = true;

            var openUrl = new ToolStripMenuItem(
                "&Additional information...",
                null,
                (sender, args) =>
                {
                    var address = this.packageList.List
                        .SelectedModelItem?
                        .Package?
                        .Weblink?
                        .ToString();

                    if (address != null)
                    {
                        Browser.Default.Navigate(address);
                    }
                });
            this.packageList.List.ContextMenuStrip.Items.Add(openUrl);
            this.packageList.List.ContextMenuStrip.Opening += (sender, args) =>
            {
                openUrl.Enabled = this.packageList.List.SelectedModelItem?.Package?.Weblink != null;
            };
        }

        //---------------------------------------------------------------------
        // ProjectExplorerTrackingToolWindow.
        //---------------------------------------------------------------------

        protected override async Task SwitchToNodeAsync(IProjectModelNode node)
        {
            Debug.Assert(!this.InvokeRequired, "running on UI thread");
            await this.viewModel.Value.SwitchToModelAsync(node)
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
                this.packageList.SetFocusOnSearchBox();
            }
        }
    }
}
