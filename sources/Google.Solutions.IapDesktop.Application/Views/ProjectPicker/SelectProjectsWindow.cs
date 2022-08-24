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

using Google.Apis.CloudResourceManager.v1.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.ProjectPicker
{
    /// <summary>
    /// Project picker for selecting an existing project.
    /// </summary>
    public interface ISelectProjectsWindow : IDisposable
    {
        DialogResult ShowDialog(IWin32Window owner);

        IReadOnlyCollection<Project> Projects { get; set; }

        IEnumerable<ProjectLocator> SelectedProjects { get; }
    }

    public class SelectProjectsWindow : ProjectPickerWindow, IAddProjectsWindow
    {
        private readonly StaticProjectPickerModel model;

        protected SelectProjectsWindow(
            StaticProjectPickerModel model,
            IExceptionDialog exceptionDialog)
            : base(model, exceptionDialog)
        {
            this.model = model;

            this.DialogText = "Select projects";
            this.ButtonText = "&Select projects";
        }

        public SelectProjectsWindow(IServiceProvider serviceProvider)
            : this(
                  new StaticProjectPickerModel(),
                    serviceProvider.GetService<IExceptionDialog>())
        {
        }

        public IReadOnlyCollection<Project> Projects
        {
            get => this.model.Projects;
            set => this.model.Projects = value;
        }
    }
}
