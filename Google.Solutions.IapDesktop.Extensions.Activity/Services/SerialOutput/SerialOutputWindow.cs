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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using System.Linq;
using Google.Solutions.Common.Diagnostics;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Integration;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.SerialOutput
{
    [Service(ServiceLifetime.Singleton)]
    [SkipCodeCoverage("All logic in view model")]
    internal partial class SerialOutputWindow
        : ProjectExplorerTrackingToolWindow<SerialOutputViewModel>
    {
        private readonly SerialOutputViewModel viewModel;

        public SerialOutputWindow(IServiceProvider serviceProvider)
            : base(
                  serviceProvider.GetService<IMainForm>().MainPanel,
                  serviceProvider.GetService<IProjectExplorer>(),
                  serviceProvider.GetService<IEventService>(),
                  serviceProvider.GetService<IExceptionDialog>())
        {
            InitializeComponent();
            this.theme.ApplyTo(this.toolStrip);

            this.viewModel = new SerialOutputViewModel();

            this.portComboBox.Items.AddRange(SerialOutputViewModel.AvailablePorts.ToArray());

            // Bind toolbar buttons.
            this.portComboBox.BindProperty(
                c => c.SelectedIndex,
                this.viewModel,
                m => m.SelectedPortIndex,
                this.components);
            this.portComboBox.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsPortComboBoxEnabled,
                this.components);
        }


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
        // Window events.
        //---------------------------------------------------------------------

    }
}
