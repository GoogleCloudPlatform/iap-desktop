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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Services.Windows.Properties
{
    [SkipCodeCoverage("All logic in view model")]
    public partial class PropertiesWindow 
        : ProjectExplorerTrackingToolWindow<IPropertiesViewModel>
    {
        private readonly IPropertiesViewModel viewModel;

        public PropertiesWindow(
            IServiceProvider serviceProvider,
            IPropertiesViewModel viewModel)
            : base(
                  serviceProvider.GetService<IMainForm>().MainPanel,
                  serviceProvider.GetService<IProjectExplorer>(),
                  serviceProvider.GetService<IEventService>(),
                  serviceProvider.GetService<IExceptionDialog>())
        {
            this.components = new System.ComponentModel.Container();
            this.viewModel = viewModel;

            InitializeComponent();

            this.infoLabel.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.InformationText,
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
                title => this.TabText = this.Text = title));
            this.components.Add(this.viewModel.OnPropertyChange(
                m => m.InspectedObject,
                obj =>
                {
                    this.propertyGrid.SelectedObject = obj;
                }));
        }

        protected override DockState DefaultState
            => WeifenLuo.WinFormsUI.Docking.DockState.DockRightAutoHide;

        //---------------------------------------------------------------------
        // ProjectExplorerTrackingToolWindow.
        //---------------------------------------------------------------------

        protected override async Task SwitchToNodeAsync(IProjectExplorerNode node)
        {
            Debug.Assert(!InvokeRequired, "running on UI thread");
            await this.viewModel.SwitchToModelAsync(node)
                .ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Window event handlers.
        //---------------------------------------------------------------------

        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            this.viewModel.SaveChanges();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var property = this.propertyGrid.SelectedGridItem?.PropertyDescriptor;

            if (property != null)
            {
                property.SetValue(this.propertyGrid.SelectedObject, null);

                // The grid does not notice this change, so we need to explicitly
                // save and refresh.

                this.viewModel.SaveChanges();
                this.propertyGrid.Refresh();
            }
        }

        private void contextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var property = this.propertyGrid.SelectedGridItem?.PropertyDescriptor;

            // We only allow resetting strings.
            this.resetToolStripMenuItem.Enabled =
                property != null && property.PropertyType == typeof(string);
        }

        private void PropertiesWindow_SizeChanged(object sender, EventArgs e)
        {
            this.splitContainer.SplitterDistance = this.splitContainer.Panel1MinSize;
        }
    }
}
