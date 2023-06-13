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
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.ProjectPicker
{
    /// <summary>
    /// Generic project picker.
    /// </summary>
    [SkipCodeCoverage("All logic in view model")]
    public partial class ProjectPickerView : Form, IView<ProjectPickerViewModel>
    {
        private readonly IExceptionDialog exceptionDialog;

        public ProjectPickerView(IExceptionDialog exceptionDialog)
        {
            this.exceptionDialog = exceptionDialog;
            InitializeComponent();

            this.projectList.List.AddCopyCommands();
        }

        public void Bind(
            ProjectPickerViewModel viewModel,
            IBindingContext bindingContext)
        {
            viewModel.LoadingError.OnPropertyChange(
                m => m.Value,
                e =>
                {
                    this.exceptionDialog.Show(this, "Loading projects failed", e);
                },
                bindingContext);

            this.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.DialogText,
                bindingContext);
            this.headlineLabel.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.DialogText,
                bindingContext);
            this.pickProjectButton.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.ButtonText,
                bindingContext);

            //
            // Bind list.
            //
            this.projectList.List.BindCollection(viewModel.FilteredProjects);
            this.projectList.BindProperty(
                c => c.SearchTerm,
                viewModel,
                m => m.Filter,
                bindingContext);
            this.projectList.BindObservableProperty(
                c => c.Loading,
                viewModel,
                m => m.IsLoading,
                bindingContext);
            this.projectList.List.BindObservableProperty(
                c => c.SelectedModelItems,
                viewModel,
                m => m.SelectedProjects,
                bindingContext);

            this.statusLabel.BindReadonlyObservableProperty(
                c => c.Visible,
                viewModel,
                m => m.IsStatusTextVisible,
                bindingContext);
            this.statusLabel.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.StatusText,
                bindingContext);

            //
            // Reset filter to kick off a search.
            //
            viewModel.Filter = null;

            //
            // Bind buttons.
            //
            this.pickProjectButton.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsProjectSelected,
                bindingContext);
        }

        private void addProjectButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }
    }
}
