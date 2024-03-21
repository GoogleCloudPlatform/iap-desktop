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

using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Extensions.Diagnostics.ToolWindows
{
    [Service]
    public partial class DebugDockingView : ToolWindowViewBase, IView<DebugDockingViewModel>
    {
        private Bound<DebugDockingViewModel> viewModel;

        public DebugDockingView(
            IMainWindow mainWindow,
            ToolWindowStateRepository stateRepository)
            : base(mainWindow, stateRepository, DockState.DockRightAutoHide)
        {
            InitializeComponent();
            this.TabText = this.Text;
        }

        public void Bind(
            DebugDockingViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.output.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                v => v.LogOutput,
                bindingContext);

            this.grid.BindReadonlyObservableProperty<PropertyGrid, object, DebugDockingViewModel>(
                c => c.SelectedObject,
                viewModel,
                v => v.SelectedObject,
                bindingContext);

            this.viewModel.Value = viewModel;
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        protected override void OnUserVisibilityChanged(bool visible)
        {
            var viewModel = this.viewModel.Value;

            viewModel.WriteOutput($"{this.IsActivated}\r\n");
            viewModel.WriteOutput($"{this.IsHidden}\r\n");
            viewModel.WriteOutput($"{this.IsFloat}\r\n");
            viewModel.WriteOutput($"{this.VisibleState}\r\n");
            viewModel.WriteOutput($"{this.DockState}\r\n");
            viewModel.WriteOutput($"{this.Pane.ActiveContent == this}\r\n");
            viewModel.WriteOutput($"\r\n");

            if (this.IsActivated != viewModel.Snapshot.Value.IsActivated)
            {
                viewModel.WriteOutput($"IsActivated: {viewModel.Snapshot.Value.IsActivated} -> {this.IsActivated}\r\n");
            }

            if (this.IsHidden != viewModel.Snapshot.Value.IsHidden)
            {
                viewModel.WriteOutput($"IsHidden: {viewModel.Snapshot.Value.IsHidden} -> {this.IsHidden}\r\n");
            }

            if (this.IsFloat != viewModel.Snapshot.Value.IsFloat)
            {
                viewModel.WriteOutput($"IsFloat: {viewModel.Snapshot.Value.IsFloat} -> {this.IsFloat}\r\n");
            }

            if (this.VisibleState != viewModel.Snapshot.Value.VisibleState)
            {
                viewModel.WriteOutput($"VisibleState: {viewModel.Snapshot.Value.VisibleState} -> {this.VisibleState}\r\n");
            }

            if (this.DockState != viewModel.Snapshot.Value.DockState)
            {
                viewModel.WriteOutput($"DockState: {viewModel.Snapshot.Value.DockState} -> {this.DockState}\r\n");
            }

            if (this.IsUserVisible != viewModel.Snapshot.Value.IsUserVisible)
            {
                viewModel.WriteOutput($"IsUserVisible: {viewModel.Snapshot.Value.IsUserVisible} -> {this.IsUserVisible}\r\n");
            }

            viewModel.Snapshot.Value = new DebugDockingViewModel.StateSnapshot()
            {
                IsActivated = this.IsActivated,
                IsHidden = this.IsHidden,
                IsFloat = this.IsFloat,
                VisibleState = this.VisibleState,
                DockState = this.DockState,
                IsActiveContent = this.Pane.ActiveContent == this,
                IsUserVisible = this.IsUserVisible,
            };

            base.OnUserVisibilityChanged(visible);
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.viewModel.Value.ClearOutput();
        }
    }
}
