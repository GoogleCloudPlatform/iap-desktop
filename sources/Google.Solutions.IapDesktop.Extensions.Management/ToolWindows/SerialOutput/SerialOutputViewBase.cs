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
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.SerialOutput
{
    [SkipCodeCoverage("All logic in view model")]
    [Service(ServiceLifetime.Singleton)]
    public partial class SerialOutputViewBase
        : ProjectExplorerTrackingToolWindow<SerialOutputViewModel>, IView<SerialOutputViewModel>
    {
        private Bound<SerialOutputViewModel> viewModel;
        private readonly ushort portNumber;

        public SerialOutputViewBase(
            IServiceProvider serviceProvider, ushort portNumber)
            : base(serviceProvider, WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide)
        {
            this.portNumber = portNumber;
            this.components = new System.ComponentModel.Container();

            InitializeComponent();
        }

        public void Bind(
            SerialOutputViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.viewModel.Value = viewModel;
            viewModel.SerialPortNumber = this.portNumber;

            viewModel.OnPropertyChange(
                m => m.WindowTitle,
                title =>
                {
                    //
                    // NB. Update properties separately instead of using multi-assignment,
                    // otherwise the title does not update properly.
                    //
                    this.TabText = title;
                    this.Text = title;
                },
                bindingContext);
            viewModel.OnPropertyChange(
                m => m.Output,
                text =>
                {
                    this.output.Clear();
                    if (!string.IsNullOrEmpty(text))
                    {
                        //
                        // Use AppendText so that the control auto-scrolls to
                        // the bottom.
                        //
                        this.output.AppendText(text);
                    }
                },
                bindingContext);

            this.enableTailButton.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsTailEnabled,
                bindingContext);
            this.enableTailButton.BindProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsEnableTailingButtonEnabled,
                bindingContext);

            viewModel.NewOutputAvailable += (sender, output) =>
            {
                BeginInvoke((Action)(() => this.output.AppendText(output)));
            };
        }

        //---------------------------------------------------------------------
        // ProjectExplorerTrackingToolWindow.
        //---------------------------------------------------------------------

        protected override void OnUserVisibilityChanged(bool visible)
        {
            base.OnUserVisibilityChanged(visible);

            //
            // Start/stop tailing depending on whether the window is actually
            // visible to the user. That avoids keeping the tail thread running
            // when nobody is watching.
            //

            this.viewModel.Value.IsTailBlocked = !visible;
        }

        protected override async Task SwitchToNodeAsync(IProjectModelNode node)
        {
            Debug.Assert(!this.InvokeRequired, "running on UI thread");
            await this.viewModel.Value.SwitchToModelAsync(node)
                .ConfigureAwait(true);
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void SerialOutputWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            //
            // Disable background activity to avoid having it touch any controls
            // while the window is being destroyed.
            //
            this.viewModel.Value.IsTailBlocked = true;
        }
    }
}
