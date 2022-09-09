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
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.ProjectPicker
{
    /// <summary>
    /// Generic project picker.
    /// </summary>
    [SkipCodeCoverage("All logic in view model")]
    public partial class ProjectPickerWindow : Form
    {
        private readonly ProjectPickerViewModel viewModel;

        public ProjectPickerWindow(
            IProjectPickerModel model,
            IExceptionDialog exceptionDialog)
        {
            InitializeComponent();

            this.viewModel = new ProjectPickerViewModel(model);
            this.viewModel.LoadingError.OnPropertyChange(
                m => m.Value,
                e =>
                {
                    exceptionDialog.Show(this, "Loading projects failed", e);
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
            this.pickProjectButton.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsProjectSelected,
                this.components);

            this.headlineLabel.ForeColor = ThemeColors.HighlightBlue;
        }

        private void addProjectButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        public IReadOnlyCollection<ProjectLocator> SelectedProjects
            => this.viewModel
                .SelectedProjects
                .Value
                .EnsureNotNull()
                .Select(p => new ProjectLocator(p.ProjectId))
                .ToList();

        public string DialogText
        {
            get => this.Text;
            set
            {
                this.Text = value;
                this.headlineLabel.Text = value;
            }
        }

        public string ButtonText
        {
            get => this.pickProjectButton.Text;
            set => this.pickProjectButton.Text = value;
        }
    }
}
