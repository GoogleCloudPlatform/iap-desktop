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
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.ToolWindows.Properties;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Settings;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.ToolWindows.Properties
{
    [SkipCodeCoverage("All logic in view model")]
    public abstract partial class PropertiesInspectorViewBase
        : ProjectExplorerTrackingToolWindow<IPropertiesInspectorViewModel>
    {
        private IPropertiesInspectorViewModel viewModel;

        public PropertiesInspectorViewBase(IServiceProvider serviceProvider)
            : base(serviceProvider, DockState.DockRightAutoHide)
        {
            this.components = new System.ComponentModel.Container();

            InitializeComponent();
        }

        protected void Bind(
            IPropertiesInspectorViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.viewModel = viewModel;
            this.propertyGrid.EnableRichTextDescriptions();

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
            viewModel.InspectedObject.PropertyChanged += (_, __) =>
            {
                SetInspectedObject(viewModel.InspectedObject.Value);
            };
        }

        private void SetInspectedObject(object obj)
        {
            Debug.Assert(this.viewModel != null, "Bind has been called");

            //
            // NB. The PropertyGrid displays a snapshot, if any of the
            // properties of the object changes, the grid does not
            // update automatically. 
            //

            if (this.propertyGrid.SelectedObject is INotifyPropertyChanged oldObj)
            {
                oldObj.PropertyChanged -= RefreshOnPropertyChange;
            }

            if (obj is ISettingsCollection collection)
            {
                // Use a custom type descriptor to interpret each setting
                // as property.
                this.propertyGrid.SelectedObject =
                    new SettingsCollectionTypeDescriptor(collection);
            }
            else
            {
                this.propertyGrid.SelectedObject = obj;
            }

            if (obj is INotifyPropertyChanged newObj)
            {
                newObj.PropertyChanged += RefreshOnPropertyChange;
            }
        }

        private void RefreshOnPropertyChange(
            object sender,
            PropertyChangedEventArgs args)
        {
            this.propertyGrid.Refresh();
        }

        //---------------------------------------------------------------------
        // ProjectExplorerTrackingToolWindow.
        //---------------------------------------------------------------------

        protected override async Task SwitchToNodeAsync(IProjectModelNode node)
        {
            Debug.Assert(!this.InvokeRequired, "running on UI thread");
            Debug.Assert(this.viewModel != null, "Bind has been called");

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

            this.resetToolStripMenuItem.Enabled =
                property != null && property.CanResetValue(this.propertyGrid.SelectedObject);
        }
    }
}
