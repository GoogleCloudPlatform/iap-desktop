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
using Google.Apis.Util;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.ProjectPicker
{
    public interface IProjectPickerDialog
    {
        DialogResult SelectCloudProjects(
            IWin32Window owner,
            string caption,
            IResourceManagerAdapter resourceManager,
            out IReadOnlyCollection<ProjectLocator> selectedProjects);

        DialogResult SelectLocalProjects(
            IWin32Window owner,
            string caption,
            IReadOnlyCollection<IProjectModelProjectNode> projects,
            out IReadOnlyCollection<ProjectLocator> selectedProjects);
    }

    public class ProjectPickerDialog : IProjectPickerDialog
    {
        private readonly IExceptionDialog exceptionDialog;
        private readonly ITheme theme;

        public ProjectPickerDialog(
            IExceptionDialog exceptionDialog,
            ITheme theme)
        {
            this.exceptionDialog = exceptionDialog.ThrowIfNull(nameof(exceptionDialog));
            this.theme = theme.ThrowIfNull(nameof(theme));
        }

        private DialogResult SelectProjects(
            IWin32Window owner,
            ProjectPickerWindow window,
            string caption,
            out IReadOnlyCollection<ProjectLocator> selectedProjects)
        {
            window.DialogText = caption;
            window.ButtonText = $"&{caption}";

            var result = window.ShowDialog(owner);

            selectedProjects = (result == DialogResult.OK)
                ? window.SelectedProjects
                : null;

            return result;
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public DialogResult SelectCloudProjects(
            IWin32Window owner,
            string caption,
            IResourceManagerAdapter resourceManager,
            out IReadOnlyCollection<ProjectLocator> selectedProjects)
        {
            var model = new CloudModel(resourceManager);

            using (var window = new ProjectPickerWindow(model, this.exceptionDialog)
            {
                Theme = this.theme
            })
            {
                return SelectProjects(owner, window, caption, out selectedProjects);
            }
        }

        public DialogResult SelectLocalProjects(
            IWin32Window owner,
            string caption,
            IReadOnlyCollection<IProjectModelProjectNode> projects,
            out IReadOnlyCollection<ProjectLocator> selectedProjects)
        {
            var model = new StaticModel(projects
                .Select(p => new Project()
                {
                    Name = p.DisplayName,
                    ProjectId = p.Project.ProjectId
                })
                .ToList());

            using (var window = new ProjectPickerWindow(model, this.exceptionDialog)
            {
                Theme = this.theme
            })
            {
                return SelectProjects(owner, window, caption, out selectedProjects);
            }
        }

        //---------------------------------------------------------------------
        // Model.
        //---------------------------------------------------------------------

        internal sealed class CloudModel : IProjectPickerModel
        {
            private readonly IResourceManagerAdapter resourceManager;

            public CloudModel(IResourceManagerAdapter resourceManager)
            {
                this.resourceManager = resourceManager;
            }

            public async Task<FilteredProjectList> ListProjectsAsync(
                string prefix,
                int maxResults,
                CancellationToken cancellationToken)
            {
                return await this.resourceManager.ListProjectsAsync(
                        string.IsNullOrEmpty(prefix)
                            ? null // All projects.
                            : ProjectFilter.ByPrefix(prefix),
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
                string prefix,
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
