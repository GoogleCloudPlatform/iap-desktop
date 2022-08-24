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
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.ProjectPicker
{
    public interface IProjectPickerWindow : IDisposable
    {
        DialogResult ShowDialog(IWin32Window owner);
        IEnumerable<ProjectLocator> Projects { get; }
    }

    [SkipCodeCoverage("All logic in view model")]
    public partial class ProjectPickerWindow : Form, IProjectPickerWindow
    {
        private readonly ProjectPickerViewModel viewModel;

        public ProjectPickerWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            this.viewModel = new ProjectPickerViewModel(
                serviceProvider.GetService<IResourceManagerAdapter>());

            this.viewModel.LoadingError.OnPropertyChange(
                m => m.Value,
                e =>
                {
                    serviceProvider.GetService<IExceptionDialog>()
                        .Show(this, "Loading projects failed", e);
                });

            // Bind list.
            this.projectList.List.BindCollection(this.viewModel.FilteredProjects);
            this.projectList.List.AddCopyCommands();
            this.projectList.BindProperty(
                c => c.SearchTerm,
                this.viewModel,
                m => m.Filter,
                this.components);
            this.projectList.BindProperty(
                c => c.Loading,
                this.viewModel,
                m => m.IsLoading,
                this.components);
            this.projectList.List.BindProperty(
                c => c.SelectedModelItems,
                this.viewModel,
                m => m.SelectedProjects,
                this.components);

            this.statusLabel.BindReadonlyProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsStatusTextVisible,
                this.components);
            this.statusLabel.BindReadonlyProperty(
                c => c.Text,
                this.viewModel,
                m => m.StatusText,
                this.components);

            // Reset filter to kick off a search.
            this.viewModel.Filter = null;

            // Bind buttons.
            this.addProjectButton.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProjectSelected,
                this.components);

            this.headlineLabel.ForeColor = ThemeColors.HighlightBlue;

            this.Disposed += (sender, args) =>
            {
                this.viewModel.Dispose();
            };
        }

        //---------------------------------------------------------------------
        // IProjectPickerWindow.
        //---------------------------------------------------------------------

        public IEnumerable<ProjectLocator> Projects
            => this.viewModel
                .SelectedProjects
                .Value
                .EnsureNotNull()
                .Select(p => new ProjectLocator(p.ProjectId));

        private void addProjectButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }
    }
}
