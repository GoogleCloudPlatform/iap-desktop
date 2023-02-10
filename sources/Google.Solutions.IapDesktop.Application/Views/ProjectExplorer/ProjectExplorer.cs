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

using Google.Apis.Util;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.Mvvm.Commands;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Views.ProjectExplorer
{
    public interface IProjectExplorer
    {
        Task ShowAddProjectDialogAsync();

        IProjectModelNode SelectedNode { get; }

        ICommandContainer<IProjectModelNode> ContextMenuCommands { get; }
        ICommandContainer<IProjectModelNode> ToolbarCommands { get; }
    }

    public class ProjectExplorer : IProjectExplorer
    {
        private readonly ProjectExplorerView view;

        public ProjectExplorer(IServiceProvider serviceProvider)
        {
            serviceProvider.ThrowIfNull(nameof(serviceProvider));

            var window = ToolWindow
                .GetWindow<ProjectExplorerView, ProjectExplorerViewModel>(serviceProvider);
            window.Bind();
            this.view = window.view;
        }

        public IProjectModelNode SelectedNode 
            => this.view.SelectedNode;

        public ICommandContainer<IProjectModelNode> ContextMenuCommands
            => this.view.ContextMenuCommands;

        public ICommandContainer<IProjectModelNode> ToolbarCommands
            => this.view.ToolbarCommands;

        public Task ShowAddProjectDialogAsync()
            => this.view.ShowAddProjectDialogAsync();
    }
}
