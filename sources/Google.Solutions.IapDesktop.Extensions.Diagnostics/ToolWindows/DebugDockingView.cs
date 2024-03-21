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
        private DebugDockingViewModel viewModel;

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

            this.viewModel = viewModel;
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        protected override void OnUserVisibilityChanged(bool visible)
        {

            this.viewModel.WriteOutput($"{this.IsActivated}\r\n");
            this.viewModel.WriteOutput($"{this.IsHidden}\r\n");
            this.viewModel.WriteOutput($"{this.IsFloat}\r\n");
            this.viewModel.WriteOutput($"{this.VisibleState}\r\n");
            this.viewModel.WriteOutput($"{this.DockState}\r\n");
            this.viewModel.WriteOutput($"{this.Pane.ActiveContent == this}\r\n");
            this.viewModel.WriteOutput($"\r\n");

            if (this.IsActivated != this.viewModel.Snapshot.Value.IsActivated)
            {
                this.viewModel.WriteOutput($"IsActivated: {this.viewModel.Snapshot.Value.IsActivated} -> {this.IsActivated}\r\n");
            }

            if (this.IsHidden != this.viewModel.Snapshot.Value.IsHidden)
            {
                this.viewModel.WriteOutput($"IsHidden: {this.viewModel.Snapshot.Value.IsHidden} -> {this.IsHidden}\r\n");
            }

            if (this.IsFloat != this.viewModel.Snapshot.Value.IsFloat)
            {
                this.viewModel.WriteOutput($"IsFloat: {this.viewModel.Snapshot.Value.IsFloat} -> {this.IsFloat}\r\n");
            }

            if (this.VisibleState != this.viewModel.Snapshot.Value.VisibleState)
            {
                this.viewModel.WriteOutput($"VisibleState: {this.viewModel.Snapshot.Value.VisibleState} -> {this.VisibleState}\r\n");
            }

            if (this.DockState != this.viewModel.Snapshot.Value.DockState)
            {
                this.viewModel.WriteOutput($"DockState: {this.viewModel.Snapshot.Value.DockState} -> {this.DockState}\r\n");
            }

            if (this.IsUserVisible != this.viewModel.Snapshot.Value.IsUserVisible)
            {
                this.viewModel.WriteOutput($"IsUserVisible: {this.viewModel.Snapshot.Value.IsUserVisible} -> {this.IsUserVisible}\r\n");
            }

            this.viewModel.Snapshot.Value = new DebugDockingViewModel.StateSnapshot()
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
            this.viewModel.ClearOutput();
        }
    }
}
