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

using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Diagnostics.ToolWindows
{
    [Service(ServiceLifetime.Singleton)]
    public partial class DebugProjectExplorerTrackingView
        : ProjectExplorerTrackingToolWindow<DebugProjectExplorerTrackingViewModel>,
          IView<DebugProjectExplorerTrackingViewModel>
    {
        private Bound<DebugProjectExplorerTrackingViewModel> viewModel;

        public DebugProjectExplorerTrackingView(IServiceProvider serviceProvider)
            : base(serviceProvider, WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide)
        {
            InitializeComponent();
        }

        public void Bind(
            DebugProjectExplorerTrackingViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.instanceNameLabel.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.InstanceName,
                bindingContext);
            this.viewModel.Value = viewModel;
        }

        //---------------------------------------------------------------------
        // ProjectExplorerTrackingToolWindow.
        //---------------------------------------------------------------------

        protected override Task SwitchToNodeAsync(IProjectModelNode node)
        {
            if (node is IProjectModelInstanceNode vmNode)
            {
                this.viewModel.Value.Node.Value = vmNode;
            }
            else
            {
                // We cannot handle other types or node, so ignore.
            }

            return Task.CompletedTask;
        }
    }
}
