//
// Copyright 2021 Google LLC
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

using Google.Apis.CloudResourceManager.v1.Data;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.ProjectPicker
{
    public interface IProjectPickerDialog
    {
        DialogResult SelectCloudProjects(
            IWin32Window owner,
            string caption,
            IResourceManagerClient resourceManager,
            out IReadOnlyCollection<ProjectLocator>? selectedProjects);

        DialogResult SelectLocalProjects(
            IWin32Window owner,
            string caption,
            IReadOnlyCollection<IProjectModelProjectNode> projects,
            out IReadOnlyCollection<ProjectLocator>? selectedProjects);
    }

    public class ProjectPickerDialog : IProjectPickerDialog
    {
        private readonly IExceptionDialog exceptionDialog;
        private readonly ViewFactory<ProjectPickerView, ProjectPickerViewModel> viewFactory;

        public ProjectPickerDialog(IServiceProvider serviceProvider)
        {
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.viewFactory = serviceProvider.GetViewFactory<ProjectPickerView, ProjectPickerViewModel>();
            this.viewFactory.Theme = serviceProvider.GetService<IThemeService>().DialogTheme;
        }

        private DialogResult SelectProjects(
            IWin32Window owner,
            string caption,
            IProjectPickerModel model,
            out IReadOnlyCollection<ProjectLocator>? selectedProjects)
        {
            var viewModel = new ProjectPickerViewModel(model);
            viewModel.DialogText.Value = caption;
            viewModel.ButtonText.Value = $"&{caption}";

            using (var dialog = this.viewFactory.CreateDialog(viewModel))
            {
                var result = dialog.ShowDialog(owner);

                if (result == DialogResult.OK)
                {
                    selectedProjects = viewModel
                        .SelectedProjects
                        .Value
                        .EnsureNotNull()
                        .Select(p => new ProjectLocator(p.ProjectId))
                        .ToList();
                }
                else
                {
                    selectedProjects = null;
                }

                return result;
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public DialogResult SelectCloudProjects(
            IWin32Window owner,
            string caption,
            IResourceManagerClient resourceManager,
            out IReadOnlyCollection<ProjectLocator>? selectedProjects)
        {
            return SelectProjects(
                owner,
                caption,
                new CloudModel(resourceManager),
                out selectedProjects);
        }

        public DialogResult SelectLocalProjects(
            IWin32Window owner,
            string caption,
            IReadOnlyCollection<IProjectModelProjectNode> projects,
            out IReadOnlyCollection<ProjectLocator>? selectedProjects)
        {
            var model = new StaticModel(projects
                .Select(p => new Project()
                {
                    Name = p.DisplayName,
                    ProjectId = p.Project.ProjectId
                })
                .ToList());


            return SelectProjects(
                owner,
                caption,
                model,
                out selectedProjects);
        }

        //---------------------------------------------------------------------
        // Model.
        //---------------------------------------------------------------------

        internal sealed class CloudModel : IProjectPickerModel
        {
            private readonly IResourceManagerClient resourceManager;

            public CloudModel(IResourceManagerClient resourceManager)
            {
                this.resourceManager = resourceManager;
            }

            public async Task<FilteredProjectList> ListProjectsAsync(
                string? prefix,
                int maxResults,
                CancellationToken cancellationToken)
            {
                return await this.resourceManager.ListProjectsAsync(
                        string.IsNullOrEmpty(prefix)
                            ? null // All projects.
                            : ProjectFilter.ByTerm(prefix!),
                        maxResults,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        internal sealed class StaticModel : IProjectPickerModel
        {
            private readonly IReadOnlyCollection<Project> projects;

            public StaticModel(IReadOnlyCollection<Project> projects)
            {
                this.projects = projects;
            }

            public Task<FilteredProjectList> ListProjectsAsync(
                string? prefix,
                int maxResults,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    new FilteredProjectList(
                        this.projects
                            .EnsureNotNull()
                            .Where(p => prefix == null ||
                                        p.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                                        p.ProjectId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            .Take(maxResults),
                        false));
            }
        }
    }
}
