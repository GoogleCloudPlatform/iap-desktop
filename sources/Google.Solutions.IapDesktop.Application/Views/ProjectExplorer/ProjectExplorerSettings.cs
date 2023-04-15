﻿//
// Copyright 2022 Google LLC
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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Views.ProjectExplorer
{
    internal interface IProjectExplorerSettings : IDisposable
    {
        /// <summary>
        /// Last used filter.
        /// </summary>
        OperatingSystems OperatingSystemsFilter { get; set; }

        /// <summary>
        /// Collapsed projects. 
        /// NB. We store the collapsed projects instead of the expanded
        /// projects so that we're (a) backwards-compatible and (b)
        /// expand by default.
        /// </summary>
        ISet<ProjectLocator> CollapsedProjects { get; }
    }

    internal sealed class ProjectExplorerSettings : IProjectExplorerSettings
    {
        private readonly ApplicationSettingsRepository settingsRepository;
        private readonly bool disposeRepositoryAfterUse;

        public ProjectExplorerSettings(
            ApplicationSettingsRepository settingsRepository,
            bool disposeRepositoryAfterUse)
        {
            this.settingsRepository = settingsRepository;
            this.disposeRepositoryAfterUse = disposeRepositoryAfterUse;

            //
            // Load settings.
            //
            // NB. Do not hold on to the settings object because it might change.
            //
            var settings = this.settingsRepository.GetSettings();

            this.OperatingSystemsFilter = settings.IncludeOperatingSystems.EnumValue;
            this.CollapsedProjects = (settings.CollapsedProjects.StringValue ?? string.Empty)
                .Split(',')
                .Where(projectId => !string.IsNullOrWhiteSpace(projectId))
                .Select(projectId => new ProjectLocator(projectId.Trim()))
                .ToHashSet();
        }

        public OperatingSystems OperatingSystemsFilter { get; set; }

        public ISet<ProjectLocator> CollapsedProjects { get; }

        public void Dispose()
        {
            //
            // Save settings.
            //

            var settings = this.settingsRepository.GetSettings();

            settings.IncludeOperatingSystems.EnumValue = this.OperatingSystemsFilter;
            settings.CollapsedProjects.StringValue = string.Join(
                ",",
                this.CollapsedProjects.Select(locator => locator.ProjectId));

            this.settingsRepository.SetSettings(settings);

            if (this.disposeRepositoryAfterUse)
            {
                this.settingsRepository.Dispose();
            }
        }
    }
}
